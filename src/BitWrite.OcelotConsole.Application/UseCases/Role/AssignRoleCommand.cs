using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

/// <summary>
/// Assigns a role to a user (§6.2).
/// </summary>
public record AssignRoleCommand(
    UserId UserId,
    RoleId RoleId,
    string InitiatedBy,
    string CorrelationId = "");

/// <summary>
/// Response for Role assignment.
/// </summary>
public record RoleAssignmentResponse(
    Guid UserId,
    string RoleId,
    string RoleName,
    string AssignedBy,
    DateTimeOffset AssignedAt);
