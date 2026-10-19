using BitWrite.OcelotConsole.Domain.Events;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : DomainEvent;
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
