using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class RevokeSessionCommandHandler
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RevokeSessionCommandHandler(
        ISessionRepository sessionRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _sessionRepository = sessionRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RevokeSessionResponse> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetAsync(command.SessionId, cancellationToken);
        if (session == null)
        {
            throw new DomainException($"Session {command.SessionId} not found", "SESSION_NOT_FOUND");
        }

        if (session.IsRevoked)
        {
            throw new DomainException($"Session {command.SessionId} is already revoked", "SESSION_ALREADY_REVOKED");
        }

        session.Revoke(command.Reason, command.RevokedBy);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in session.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        session.ClearDomainEvents();

        return new RevokeSessionResponse(
            session.Id,
            true,
            $"Session {command.SessionId} revoked successfully");
    }
}