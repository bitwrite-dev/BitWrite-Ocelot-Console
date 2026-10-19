using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Route;
using BitWrite.OcelotConsole.Domain.Aggregates.Route;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public class DeleteRouteCommandHandler
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteRouteCommandHandler(
        IRouteRepository routeRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _routeRepository = routeRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(DeleteRouteCommand command, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetAsync(command.Id, cancellationToken);
        if (route == null)
            return false;

        route.Delete();

        // Persist deletion
        await _routeRepository.DeleteAsync(command.Id, cancellationToken);

        // Dispatch domain events from aggregate
        foreach (var domainEvent in route.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        route.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "DeleteRoute",
            "Route",
            command.Id.Value.ToString(),
            "Success"
        );
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return true;
    }
}
