using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Update user profile (§5.3).
/// </summary>
public record UpdateUserCommand(
    UserId Id,
    string? DisplayName,
    string? Email,
    string InitiatedBy,
    string CorrelationId = ""
);
