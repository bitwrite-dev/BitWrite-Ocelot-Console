using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Disable a user (§5.4).
/// </summary>
public record DisableUserCommand(
    UserId Id,
    string InitiatedBy,
    string CorrelationId = ""
);
