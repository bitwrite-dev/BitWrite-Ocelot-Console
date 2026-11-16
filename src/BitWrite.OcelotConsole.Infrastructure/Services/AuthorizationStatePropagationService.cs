using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.Extensions.Logging;

namespace BitWrite.OcelotConsole.Infrastructure.Services;

/// <summary>
/// Propagates security-relevant state changes to revocation (#647 mechanics via ISecurityService)
/// and emits authorization invalidation/version signals (IAM-12/IAM-13).
/// Fail-closed and idempotent with respect to repeated events: revocation is safe to repeat,
/// and version signals are monotonic.
/// </summary>
public sealed class AuthorizationStatePropagationService : IAuthorizationStatePropagationService
{
    private readonly ISecurityService _securityService;
    private readonly IUserRepository _userRepository;
    private readonly IAtomicOperationService _atomicOperationService;
    private readonly ILogger<AuthorizationStatePropagationService> _logger;

    public AuthorizationStatePropagationService(
        ISecurityService securityService,
        IUserRepository userRepository,
        IAtomicOperationService atomicOperationService,
        ILogger<AuthorizationStatePropagationService> logger)
    {
        _securityService = securityService;
        _userRepository = userRepository;
        _atomicOperationService = atomicOperationService;
        _logger = logger;
    }

    public async Task OnUserDisabledAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await RevokeUserSessionsAsync(userId, "UserDisabled", cancellationToken);
        await InvalidateUserAsync(userId, cancellationToken);
    }

    public async Task OnRoleChangedAsync(RoleId roleId, string reason, CancellationToken cancellationToken = default)
    {
        await InvalidateRoleAsync(roleId, cancellationToken);
        await InvalidateUsersAssignedToRoleAsync(roleId, reason, cancellationToken);
    }

    public async Task OnUserRoleAssignmentChangedAsync(UserId userId, RoleId roleId, string reason, CancellationToken cancellationToken = default)
    {
        // Assignment/removal affects this user's effective permissions; signal both user and role.
        await InvalidateRoleAsync(roleId, cancellationToken);
        await InvalidateUserAsync(userId, cancellationToken);
    }

    public async Task OnCredentialOrSecurityStateChangedAsync(UserId userId, string reason, CancellationToken cancellationToken = default)
    {
        await RevokeUserSessionsAsync(userId, reason, cancellationToken);
        await InvalidateUserAsync(userId, cancellationToken);
    }

    private async Task RevokeUserSessionsAsync(UserId userId, string reason, CancellationToken cancellationToken)
    {
        // Uses existing #647 session/refresh mechanics; revoking already-revoked tokens is a no-op.
        await _securityService.RevokeAllUserSessionsAsync(userId, "system", reason, cancellationToken);
        _logger.LogInformation("Revoked sessions/refresh tokens for user {UserId} due to {Reason}", userId.Value, reason);
    }

    private async Task InvalidateUsersAssignedToRoleAsync(RoleId roleId, string reason, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var affected = 0;
        foreach (var user in users)
        {
            if (user.RoleIds.Contains(roleId.Value, StringComparer.Ordinal))
            {
                await InvalidateUserAsync(user.Id, cancellationToken);
                affected++;
            }
        }

        _logger.LogInformation("Invalidated {Count} users for role {RoleId} due to {Reason}", affected, roleId.Value, reason);
    }

    private Task InvalidateUserAsync(UserId userId, CancellationToken cancellationToken)
    {
        // Monotonic version bump as invalidation signal for freshness enforcement (IAM-13).
        return _atomicOperationService.IncrementAsync(UserAuthzVersionKey(userId), 1, cancellationToken);
    }

    private Task InvalidateRoleAsync(RoleId roleId, CancellationToken cancellationToken)
    {
        return _atomicOperationService.IncrementAsync(RoleAuthzVersionKey(roleId), 1, cancellationToken);
    }

    private static string UserAuthzVersionKey(UserId userId) => $"ocelot:iam:v1:authz:ver:user:{userId.Value}";
    private static string RoleAuthzVersionKey(RoleId roleId) => $"ocelot:iam:v1:authz:ver:role:{roleId.Value}";
}
