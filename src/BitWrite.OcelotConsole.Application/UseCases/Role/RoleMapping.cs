using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

internal static class RoleMapping
{
    public static RoleResponse ToResponse(RoleEntity role) => new(
        role.Id.Value,
        role.Name,
        role.Description,
        role.IsSystemRole,
        role.Status.Value,
        role.Permissions.ToList(),
        role.CreatedAt,
        role.UpdatedAt,
        role.UpdatedBy);
}
