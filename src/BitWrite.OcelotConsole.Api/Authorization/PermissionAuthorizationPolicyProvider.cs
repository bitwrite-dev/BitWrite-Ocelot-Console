using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BitWrite.OcelotConsole.Api.Authorization;

public sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(AuthorizationPolicyExtensions.PermissionPolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[AuthorizationPolicyExtensions.PermissionPolicyPrefix.Length..];
            // Permission codes carry the "perm." prefix (e.g. perm.settings.read). Policy names
            // may omit it (e.g. "perm:settings.read"); normalise so both forms resolve.
            if (!permission.StartsWith("perm.", StringComparison.OrdinalIgnoreCase))
                permission = "perm." + permission;
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
