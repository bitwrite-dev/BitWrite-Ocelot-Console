using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class RemoveRoleCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RemoveRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RoleAssignmentResponse> HandleAsync(RemoveRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(command.UserId, cancellationToken);
        if (user == null)
            throw new DomainException($"User {command.UserId} not found", "USER_NOT_FOUND");

        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken);
        if (role == null)
            throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

        user.RemoveRole(role.Name, command.CorrelationId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return new RoleAssignmentResponse(
            user.Id.Value,
            role.Id.Value,
            role.Name,
            command.InitiatedBy,
            DateTimeOffset.UtcNow);
    }
}
