using System.Security.Claims;
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

    public CurrentStatePermissionAuthorizationHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEffectivePermissionResolver effectivePermissionResolver)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _effectivePermissionResolver = effectivePermissionResolver;
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

            var effective = await _effectivePermissionResolver.ResolveAsync(userId);
            if (effective.Contains(requested))
                context.Succeed(requirement);
        }
        catch
        {
            // Fail closed on any incomplete/exceptional validation.
        }
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
                return false; // stale, renamed, ambiguous/unresolvable

            if (!user.RoleIds.Contains(roleId.Value, StringComparer.Ordinal))
                return false; // not currently assigned

            var role = await _roleRepository.GetByIdAsync(roleId);
            if (role is null || !role.IsActive)
                return false;

            // Ensure current role name still matches the claimed normalized name.
            if (!string.Equals(PermissionCode.Normalize(role.Name), claimed, StringComparison.Ordinal))
                return false;

            resolvedClaimRoleIds.Add(roleId.Value);
        }

        // Fail closed if token omits currently assigned role names that are claim-eligible in issuance terms?
        // Current-state authz must not trust claims alone; resolver enforces effective permissions from current state.
        // Claims must be valid/current (no stale/invalid claim accepted). Presence of extra valid claims is denied above;
        // absence of some currently assigned roles is acceptable as long as effective permissions are sufficient.
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
