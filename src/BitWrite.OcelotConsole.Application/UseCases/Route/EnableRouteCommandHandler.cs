using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Route;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainRoute = BitWrite.OcelotConsole.Domain.Aggregates.Route.Route;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public class EnableRouteCommandHandler
{
    private readonly IRouteRepository _routeRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public EnableRouteCommandHandler(
        IRouteRepository routeRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _routeRepository = routeRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RouteResponse?> HandleAsync(EnableRouteCommand command, CancellationToken cancellationToken = default)
    {
        var route = await _routeRepository.GetAsync(command.RouteId, cancellationToken);
        if (route == null)
            return null;

        if (route.IsEnabled)
            return RouteResponseMapper.Map(route);

        route.Enable(command.CorrelationId);

        await _routeRepository.UpdateAsync(route, cancellationToken);

        // Dispatch domain events from aggregate
        foreach (var domainEvent in route.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        route.ClearDomainEvents();

        // Dispatch audit event
        await _eventDispatcher.DispatchAsync(new AuditRecorded(
            command.InitiatedBy,
            "Enable",
            "Route",
            command.RouteId.ToString(),
            "Success"), cancellationToken);

        return RouteResponseMapper.Map(route);
    }

    private static RouteResponse MapToResponse(Domain.Aggregates.Route.Route route)
    {
        return RouteResponseMapper.Map(route);
    }
}
