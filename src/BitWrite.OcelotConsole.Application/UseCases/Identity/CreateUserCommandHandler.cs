using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class CreateUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILocalCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        ILocalCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<UserResponse> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Guard: username must be unique
        var existing = await _userRepository.GetByUsernameAsync(command.Username, cancellationToken);
        if (existing is not null)
            throw new DomainException($"Username '{command.Username}' is already taken", "USERNAME_ALREADY_TAKEN");

        var userId = UserId.New();

        var user = User.CreateLocal(
            userId,
            command.Username,
            command.DisplayName,
            command.Email,
            command.InitiatedBy,
            command.CorrelationId);

        await _userRepository.AddAsync(user, cancellationToken);

        // Create local credential with hashed password if one was provided
        if (!string.IsNullOrWhiteSpace(command.Password))
        {
            var passwordHash = await _passwordHasher.HashPasswordAsync(command.Password, cancellationToken);
            var credential = LocalCredential.Create(
                userId,
                command.Password,
                PasswordHash.From(passwordHash, "BCrypt", 1),
                command.CorrelationId);
            await _credentialRepository.AddAsync(credential, cancellationToken);
        }

        // Dispatch domain events
        foreach (var domainEvent in user.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        user.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "CreateUser",
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
