namespace BitWrite.OcelotConsole.Application.Permissions;

public static class PermissionCode
{
    public static string Normalize(string? code) => (code ?? string.Empty).Trim().ToLowerInvariant();
}
