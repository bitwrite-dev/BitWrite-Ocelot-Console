namespace BitWrite.OcelotConsole.Application.Permissions;

public enum PermissionLookupState
{
    Active = 1,
    Retired = 2,
    Unknown = 3
}

public sealed record PermissionLookupResult(PermissionLookupState State, PermissionDefinition? Definition)
{
    public static PermissionLookupResult Unknown(string _) => new(PermissionLookupState.Unknown, null);

    public static PermissionLookupResult Known(PermissionDefinition definition) =>
        new(definition.Status == PermissionStatus.Active ? PermissionLookupState.Active : PermissionLookupState.Retired,
            definition);

    public bool IsKnown => State != PermissionLookupState.Unknown;
    public bool IsActive => State == PermissionLookupState.Active;
    public bool IsRetired => State == PermissionLookupState.Retired;
    public bool IsUnknown => State == PermissionLookupState.Unknown;
    public bool IsAssignable => IsActive && Definition is { Assignable: true };
    public bool CanGrant => IsActive;
}
