namespace BitWrite.OcelotConsole.Application.Permissions;

/// <summary>
/// Product-owned, source-code Permission Catalog definitions (SoT).
/// </summary>
/// <remarks>
/// Official production-approved permission codes are gated by P-GATE-1 and are not invented here.
/// This registry provides the in-code source-of-truth mechanism and remains read-only at runtime.
/// </remarks>
public static class BitWritePermissionCatalogDefinitions
{
    public static IReadOnlyCollection<PermissionDefinition> All { get; } = Array.Empty<PermissionDefinition>();
}
