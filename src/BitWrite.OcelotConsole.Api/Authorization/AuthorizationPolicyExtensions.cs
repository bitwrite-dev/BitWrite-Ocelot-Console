using BitWrite.OcelotConsole.Application.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace BitWrite.OcelotConsole.Api.Authorization;

public static class AuthorizationPolicyExtensions
{
    public const string PermissionPolicyPrefix = "perm:";

    public static string PolicyName(string permission) => $"{PermissionPolicyPrefix}{PermissionCode.Normalize(permission)}";

    public static void AddPermissionPolicy(this AuthorizationOptions options, string permission)
    {
        var normalized = PermissionCode.Normalize(permission);
        options.AddPolicy(PolicyName(normalized), policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(normalized));
        });
    }

    public static void AddPermissionPolicies(this AuthorizationOptions options, IEnumerable<string> permissions)
    {
        foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
            options.AddPermissionPolicy(permission);
    }
}
