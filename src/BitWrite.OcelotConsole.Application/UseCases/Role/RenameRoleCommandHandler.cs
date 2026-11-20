using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class RenameRoleCommandHandler
{
    private readonly IRoleRepository _roleRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RenameRoleCommandHandler(IRoleRepository roleRepository, IDomainEventDispatcher eventDispatcher)
    {
        _roleRepository = roleRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RoleResponse> HandleAsync(RenameRoleCommand command, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
            ?? throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

        role.Rename(command.NewName, command.InitiatedBy, command.CorrelationId);
        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _eventDispatcher.DispatchAsync(role.DomainEvents, cancellationToken);
        role.ClearDomainEvents();
        await _eventDispatcher.DispatchAsync(new AuditRecorded(command.InitiatedBy, "RenameRole", "Role", role.Id.Value, "Success").WithCorrelationId(command.CorrelationId), cancellationToken);
        return RoleMapping.ToResponse(role);
    }
}
