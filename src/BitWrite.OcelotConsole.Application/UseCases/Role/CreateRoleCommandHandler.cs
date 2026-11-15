using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class CreateRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponse> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = RoleEntity.Create(RoleId.New(), command.Name, command.Description, command.InitiatedBy, command.CorrelationId);
        await _roleRepository.AddAsync(role, cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
