using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public record RoleResponse(
    string Id,
    string Name,
    string Description,
    bool IsSystemRole,
    string Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public record CreateRoleCommand(
    string Name,
    string? Description,
    string InitiatedBy,
    string CorrelationId = "");

public record GetRoleQuery(RoleId RoleId);

public record GetRolesQuery;

public record UpdateRoleDescriptionCommand(
    RoleId RoleId,
    string? Description,
    string InitiatedBy,
    string CorrelationId = "");

public record RenameRoleCommand(
    RoleId RoleId,
    string NewName,
    string InitiatedBy,
    string CorrelationId = "");

public record ActivateRoleCommand(
    RoleId RoleId,
    string InitiatedBy,
    string CorrelationId = "");

public record DeactivateRoleCommand(
    RoleId RoleId,
    string InitiatedBy,
    string CorrelationId = "");
