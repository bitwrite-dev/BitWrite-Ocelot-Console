using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Create a new local user (§5.2).
/// </summary>
public record CreateUserCommand(
    string Username,
    string? DisplayName,
    string? Email,
    string InitiatedBy,
    string CorrelationId = ""
);
