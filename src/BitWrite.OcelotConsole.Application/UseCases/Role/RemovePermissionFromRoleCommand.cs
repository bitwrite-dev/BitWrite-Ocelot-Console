using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public record RemovePermissionFromRoleCommand(
    RoleId RoleId,
    string PermissionCode,
    string InitiatedBy,
    string CorrelationId = "");

public record RemovePermissionFromRoleResponse(
    string RoleId,
    string PermissionCode,
    string InitiatedBy,
    DateTimeOffset OccurredAt);
