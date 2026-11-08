using System.Text.Json;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public static class RedisKeyHelper
{
    public const string GlobalConfig = "ocelot:global";
    /// <summary>The one system settings row, holding the Ocelot version.</summary>
    public const string SystemSettings = "ocelot:settings";
    public static string RuntimeCurrent(EnvironmentName environment) => $"ocelot:runtime:current:{environment.Value.ToLowerInvariant()}";
    /// <summary>
    /// One entry per configuration a gateway tried to apply, appended by the runtime.
    /// </summary>
    /// <remarks>
    /// Unbounded, and nothing trimmed it until something read it. It is the only
    /// record of what gateways actually experienced when applying a configuration.
    /// </remarks>
    public const string GatewayActivationResults = "ocelot:gateway:activation:results";
    public const string LockConfiguration = "ocelot:lock:configuration";

    public static string Gateway(GatewayId id) => $"ocelot:gateway:{id.Value}";
    public static string Service(ServiceId id, EnvironmentName environment) => $"ocelot:service:{environment.Value}:{id.Value}";
    public static string Route(RouteId id, EnvironmentName environment) => $"ocelot:route:{environment.Value}:{id.Value}";
    public static string Snapshot(SnapshotVersion version, EnvironmentName environment) => $"ocelot:snapshot:{environment.Value}:{version.Value}";
    public static string Publication(PublicationId id) => $"ocelot:publication:{id.Value}";
    public static string Plugin(PluginId id) => $"ocelot:plugin:{id.Value}";
    public static string RuntimeInstance(GatewayId id) => $"ocelot:runtime:gateway:{id.Value}";
    /// <summary>
    /// What the runtime last reported, on its own key.
    /// </summary>
    /// <remarks>
    /// Separate from the instance record because the two are written by different
    /// parties and answer different questions. The runtime cannot report a status —
    /// only the control plane knows what it asked for — so when both wrote one key,
    /// every heartbeat replaced the status with nothing and the gateway went back to
    /// looking disconnected.
    /// </remarks>
    public static string RuntimeGatewayHeartbeat(GatewayId id) => $"{RuntimeInstance(id)}:heartbeat";
    public static string License(LicenseId id) => $"ocelot:license:{id.Value}";
    public static string AuditLog(string id) => $"ocelot:audit:{id}";

    public const string IndexLicenses = "ocelot:index:licenses";
    public const string IndexAuditLogs = "ocelot:index:audit-logs";

    // The indexes of the environment-scoped collections carry the environment too.
    //
    // Leaving them shared would leave the isolation half-applied and would break in
    // the way a missing row does not: every environment's id lands in one set, so
    // deleting a route in development would remove a production route from the index
    // and leave its row unreachable rather than deleted — and a list would read the
    // shared set, drop every id absent from its own environment, and answer with a
    // short list that looks like the whole truth.
    public static string IndexServices(EnvironmentName environment) => $"ocelot:index:services:{environment.Value}";
    public static string IndexRoutes(EnvironmentName environment) => $"ocelot:index:routes:{environment.Value}";
    public static string IndexSnapshots(EnvironmentName environment) => $"ocelot:index:snapshots:{environment.Value}";
    public static string IndexServiceRoutes(ServiceId serviceId, EnvironmentName environment) =>
        $"ocelot:index:service:{environment.Value}:{serviceId.Value}:routes";
    public static string IndexRouteSignature(string signature, EnvironmentName environment) =>
        $"ocelot:index:route-signature:{environment.Value}:{signature}";
    public static string IndexLicenseProductCodes = "ocelot:index:license-product-codes";

    /// <summary>Authentication throttling keys.</summary>
    public const string AuthAttempts = "ocelot:auth:attempts:{id}";
    public static string AuthAttemptsKey(string id) => $"ocelot:auth:attempts:{id}";
    public static string AuthLockoutKey(string id) => $"ocelot:auth:lockout:{id}";

    /// <summary>Identity & Access Management (IAM) namespace prefix (§10.1).</summary>
    public const string IamNamespace = "ocelot:iam:v1";

    /// <summary>IAM User key: <c>ocelot:iam:v1:user:{userId}</c>.</summary>
    /// <remarks>Legacy/simple user key helper kept for compatibility with existing code paths.</remarks>
    public static string IamUser(UserId id) => $"{IamNamespace}:user:{id.Value}";

    /// <summary>IAM User data key: <c>ocelot:iam:v1:user-data:{userId}</c>.</summary>
    public static string IamUserData(UserId id) => $"{IamNamespace}:user-data:{id.Value}";

    /// <summary>Username lookup key: <c>ocelot:iam:v1:user:by-username:{username}</c>.</summary>
    public static string IamUserByUsername(string username) => $"{IamNamespace}:user:by-username:{username.ToLowerInvariant()}";

    /// <summary>All users set key: <c>ocelot:iam:v1:users</c>.</summary>
    public static string IamUsers => $"{IamNamespace}:users";

    /// <summary>ExternalIdentity lookup key: <c>ocelot:iam:v1:external:{issuer}:{subject}</c> (§7.4).</summary>
    public static string IamExternalIdentity(string issuer, string subject) => $"{IamNamespace}:external:{issuer}:{subject}";

    /// <summary>Session key: <c>ocelot:iam:v1:session:{sessionId}</c> (§8).</summary>
    public static string IamSession(SessionId id) => $"{IamNamespace}:session:{id.Value}";

    /// <summary>Per-user session index: <c>ocelot:iam:v1:user:{userId}:sessions</c> (§8).</summary>
    public static string IamUserSessions(UserId id) => $"{IamNamespace}:user:{id.Value}:sessions";

    /// <summary>RefreshToken key: <c>ocelot:iam:v1:refreshtoken:{tokenId}</c> (§8).</summary>
    public static string IamRefreshToken(RefreshTokenId id) => $"{IamNamespace}:refreshtoken:{id.Value}";

    /// <summary>Refresh token hash lookup: <c>ocelot:iam:v1:refreshtoken:hash:{tokenHash}</c> (§8).</summary>
    public static string IamRefreshTokenByHash(string tokenHash) => $"{IamNamespace}:refreshtoken:hash:{tokenHash}";

    /// <summary>Per-user refresh token index: <c>ocelot:iam:v1:user:{userId}:refreshtokens</c> (§8).</summary>
    public static string IamUserRefreshTokens(UserId id) => $"{IamNamespace}:user:{id.Value}:refreshtokens";

    /// <summary>Role key: <c>ocelot:iam:v1:role:{roleId}</c> (§6.2).</summary>
    public static string IamRole(RoleId id) => $"{IamNamespace}:role:{id.Value}";

    /// <summary>Roles index key: <c>ocelot:iam:v1:roles</c>.</summary>
    public static string IndexRoles => $"{IamNamespace}:roles";

    /// <summary>Token blacklist key: <c>ocelot:iam:v1:blacklist:{tokenHash}</c> (§8).</summary>
    public static string IamTokenBlacklist(string tokenHash) => $"{IamNamespace}:blacklist:{tokenHash}";
}
