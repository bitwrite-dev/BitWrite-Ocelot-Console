using BitWrite.OcelotConsole.Domain.Events;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
