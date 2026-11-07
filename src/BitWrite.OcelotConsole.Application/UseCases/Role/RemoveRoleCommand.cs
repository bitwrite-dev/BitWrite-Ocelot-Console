using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

/// <summary>
/// Removes a role assignment from a user (§6.2).
/// </summary>
public record RemoveRoleCommand(
    UserId UserId,
    RoleId RoleId,
    string InitiatedBy,
    string CorrelationId = "");
