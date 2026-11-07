using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class CreateSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateSessionCommandHandler(
        ISessionRepository sessionRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _sessionRepository = sessionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<CreateSessionResponse> HandleAsync(CreateSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = Session.Create(command.UserId, command.ExpiresAt);

        await _sessionRepository.AddAsync(session, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in session.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        session.ClearDomainEvents();

        return new CreateSessionResponse(
            session.Id,
            session.UserId,
            session.CreatedAt,
            session.ExpiresAt);
    }
}