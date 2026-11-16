using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Central propagation for security-relevant state changes (IAM-12).
/// Consumes committed domain events to revoke sessions/refresh tokens via existing
/// security/session mechanics and emit authorization/version invalidation signals
/// for freshness enforcement (IAM-13). Implementations must be fail-closed and idempotent.
/// </summary>
public interface IAuthorizationStatePropagationService
{
    Task OnUserDisabledAsync(UserId userId, CancellationToken cancellationToken = default);
    Task OnRoleChangedAsync(RoleId roleId, string reason, CancellationToken cancellationToken = default);
    Task OnUserRoleAssignmentChangedAsync(UserId userId, RoleId roleId, string reason, CancellationToken cancellationToken = default);
    Task OnCredentialOrSecurityStateChangedAsync(UserId userId, string reason, CancellationToken cancellationToken = default);
}
