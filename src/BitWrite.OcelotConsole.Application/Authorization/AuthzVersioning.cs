using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Authorization;

/// <summary>
/// Authorization version keys/signals (IAM-12 propagation, IAM-13 freshness enforcement).
/// Monotonic counters; an increase invalidates previously issued access-token baselines.
/// </summary>
public static class AuthzVersioning
{
    public const string AccessTokenVersionClaimType = "av";

    public static string UserVersionKey(UserId userId) => $"ocelot:iam:v1:authz:ver:user:{userId.Value}";
    public static string RoleVersionKey(RoleId roleId) => $"ocelot:iam:v1:authz:ver:role:{roleId.Value}";
}
