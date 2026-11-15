using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public record AssignPermissionToRoleCommand(
    RoleId RoleId,
    string PermissionCode,
    string InitiatedBy,
    string CorrelationId = "");

public record AssignPermissionToRoleResponse(
    string RoleId,
    string PermissionCode,
    bool Assigned,
    bool AlreadyAssigned,
    string InitiatedBy,
    DateTimeOffset OccurredAt);
