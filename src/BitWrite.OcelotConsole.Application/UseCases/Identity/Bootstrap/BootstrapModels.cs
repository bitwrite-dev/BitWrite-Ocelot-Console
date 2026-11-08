namespace BitWrite.OcelotConsole.Application.UseCases.Identity.Bootstrap;

/// <summary>
/// Deployment-supplied bootstrap input. Values must come from secure configuration/secrets, never defaults.
/// </summary>
public record BootstrapInitialAdminOptions
{
    public bool Enabled { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string RoleName { get; init; } = "admin";
}

public record BootstrapInitialAdminCommand(string InitiatedBy, string CorrelationId = "");

public record BootstrapInitialAdminResponse(
    bool Completed,
    bool Created,
    string Message,
    Guid? UserId = null,
    string? Username = null);
