using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class RenameRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;

    public RenameRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponse> HandleAsync(RenameRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
            ?? throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

        role.Rename(command.NewName, command.InitiatedBy, command.CorrelationId);
        await _roleRepository.UpdateAsync(role, cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
