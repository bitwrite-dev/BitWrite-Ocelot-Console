namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Configuration for JIT provisioning of external identities.
/// </summary>
/// <remarks>
/// DefaultRoleId must reference an existing, active restricted role. No hardcoded identity.
/// </remarks>
public sealed record JitProvisionOptions
{
    public const string SectionName = "Identity:JitProvisioning";

    /// <summary>
    /// Configured default role identifier (canonical RoleId value) assigned to JIT-provisioned users.
    /// </summary>
    public string? DefaultRoleId { get; init; }
}
