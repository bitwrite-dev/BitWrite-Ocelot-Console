using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class ActivateRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;

    public ActivateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponse> HandleAsync(ActivateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
            ?? throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

        role.Activate(command.InitiatedBy, command.CorrelationId);
        await _roleRepository.UpdateAsync(role, cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
