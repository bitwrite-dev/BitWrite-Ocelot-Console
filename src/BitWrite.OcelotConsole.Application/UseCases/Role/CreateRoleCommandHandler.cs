using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

using BitWrite.OcelotConsole.Domain.Events;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class CreateRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IDomainEventDispatcher eventDispatcher)
    {
        _roleRepository = roleRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RoleResponse> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = RoleEntity.Create(RoleId.New(), command.Name, command.Description, command.InitiatedBy, command.CorrelationId);
        await _roleRepository.AddAsync(role, cancellationToken);
        await _eventDispatcher.DispatchAsync(role.DomainEvents, cancellationToken);
        role.ClearDomainEvents();
        await _eventDispatcher.DispatchAsync(new AuditRecorded(command.InitiatedBy, "CreateRole", "Role", role.Id.Value, "Success").WithCorrelationId(command.CorrelationId), cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
