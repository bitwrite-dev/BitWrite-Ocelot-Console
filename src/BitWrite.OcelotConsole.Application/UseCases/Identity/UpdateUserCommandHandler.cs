using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class UpdateUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<UserResponse> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(command.Id, cancellationToken);
        if (user == null)
            throw new DomainException($"User {command.Id} not found", "USER_NOT_FOUND");

        user.UpdateProfile(command.DisplayName, command.Email, command.InitiatedBy, command.CorrelationId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in user.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        user.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "UpdateUser",
            "User",
            user.Id.Value.ToString(),
            "Success");
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return MapToResponse(user);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse(
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.Email,
            user.Status.Value,
            user.UserType,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastAuthenticatedAt,
            user.RoleIds);
    }
}
