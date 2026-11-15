namespace BitWrite.OcelotConsole.Application.Permissions;

public sealed record PermissionDefinition(
    string Code,
    string Name,
    string? Description,
    string? Category,
    PermissionStatus Status,
    bool Assignable);
