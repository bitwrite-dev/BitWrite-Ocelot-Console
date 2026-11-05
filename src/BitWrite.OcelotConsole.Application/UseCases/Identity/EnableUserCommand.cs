using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Enable a disabled user (§5.5).
/// </summary>
public record EnableUserCommand(
    UserId Id,
    string InitiatedBy,
    string CorrelationId = ""
);
