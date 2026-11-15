using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class DeactivateRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;

    public DeactivateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponse> HandleAsync(DeactivateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
            ?? throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

        role.Deactivate(command.InitiatedBy, command.CorrelationId);
        await _roleRepository.UpdateAsync(role, cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
