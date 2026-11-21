using System.Security.Claims;
using BitWrite.OcelotConsole.Application.Authorization;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace BitWrite.OcelotConsole.Api.Authorization;

public sealed class CurrentStatePermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEffectivePermissionResolver _effectivePermissionResolver;
    private readonly IAtomicOperationService _atomicOperationService;

    public CurrentStatePermissionAuthorizationHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEffectivePermissionResolver effectivePermissionResolver,
        IAtomicOperationService atomicOperationService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _effectivePermissionResolver = effectivePermissionResolver;
        _atomicOperationService = atomicOperationService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        try
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return;

            var requested = PermissionCode.Normalize(requirement.Permission);
            if (string.IsNullOrWhiteSpace(requested))
                return;

            var userId = TryGetUserId(context.User);
            if (userId is null)
                return;

            var user = await _userRepository.GetAsync(userId);
            if (user is null || !user.Status.IsActive)
                return;

            var claimedRoleNames = GetClaimedRoleNames(context.User);
            if (!await AreClaimsCurrentAndValidAsync(user, claimedRoleNames))
                return;

            if (!await IsTokenFreshAsync(context.User, userId, claimedRoleNames))
                return;

            var effective = await _effectivePermissionResolver.ResolveAsync(userId);
            if (effective.Contains(requested))
                context.Succeed(requirement);
        }
        catch
        {
            // Fail closed on any incomplete/exceptional validation.
        }
    }

    private async Task<bool> IsTokenFreshAsync(
        ClaimsPrincipal principal,
        UserId userId,
        IReadOnlyCollection<string> claimedRoleNames)
    {
        // IAM-13: consume IAM-12 invalidation/version signals; fail closed on stale/missing baseline.
        var avRaw = principal.FindFirstValue(AuthzVersioning.AccessTokenVersionClaimType);
        if (!long.TryParse(avRaw, out var tokenVersion))
            return false;

        var currentUserVersion = await GetVersionAsync(AuthzVersioning.UserVersionKey(userId));
        if (tokenVersion != currentUserVersion)
            return false;

        var normalizedClaims = claimedRoleNames
            .Select(PermissionCode.Normalize)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var claimed in normalizedClaims)
        {
            var roleId = await _roleRepository.GetIdByNameAsync(claimed);
            if (roleId is null)
                return false;

            var currentRoleVersion = await GetVersionAsync(AuthzVersioning.RoleVersionKey(roleId));
            // Role version is a monotonic invalidation signal; token baseline must not predate bumps.
            if (tokenVersion < 0 || currentRoleVersion < 0)
                return false;
        }

        return true;
    }

    private async Task<long> GetVersionAsync(string key)
    {
        var raw = await _atomicOperationService.GetAsync(key);
        return long.TryParse(raw, out var v) ? v : 0L;
    }

    private async Task<bool> AreClaimsCurrentAndValidAsync(
        BitWrite.OcelotConsole.Domain.Aggregates.User.User user,
        IReadOnlyCollection<string> claimedRoleNames)
    {
        var normalizedClaims = claimedRoleNames
            .Select(PermissionCode.Normalize)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // No role claims => cannot satisfy a permission requirement via stale/empty token.
        if (normalizedClaims.Count == 0)
            return false;

        var resolvedClaimRoleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claimed in normalizedClaims)
        {
            var roleId = await _roleRepository.GetIdByNameAsync(claimed);
            if (roleId is null)
                return false;

            if (!user.RoleIds.Contains(roleId.Value, StringComparer.Ordinal))
                return false;

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role is null || !role.IsActive)
                return false;

            if (!string.Equals(PermissionCode.Normalize(role.Name), claimed, StringComparison.Ordinal))
                return false;

            resolvedClaimRoleIds.Add(roleId.Value);
        }

        return resolvedClaimRoleIds.Count == normalizedClaims.Count;
    }

    private static IReadOnlyCollection<string> GetClaimedRoleNames(ClaimsPrincipal user)
    {
        var claims = user.FindAll(SecurityService.RoleClaimType).Select(c => c.Value).ToList();
        if (claims.Count == 0)
            claims = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (claims.Count == 0)
            claims = user.FindAll("roles").Select(c => c.Value).ToList();
        return claims;
    }

    private static UserId? TryGetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub))
            return null;

        if (Guid.TryParse(sub, out var guid))
            return UserId.From(guid);

        try { return UserId.From(sub); }
        catch { return null; }
    }
}
