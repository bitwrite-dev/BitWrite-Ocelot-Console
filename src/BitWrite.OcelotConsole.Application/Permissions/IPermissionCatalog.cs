namespace BitWrite.OcelotConsole.Application.Permissions;

public interface IPermissionCatalog
{
    PermissionLookupResult Lookup(string? code);
    PermissionDefinition? GetDefinition(string? code);
    bool IsKnown(string? code);
    bool IsActive(string? code);
    bool IsAssignable(string? code);
    IReadOnlyCollection<PermissionDefinition> GetAll();
}
