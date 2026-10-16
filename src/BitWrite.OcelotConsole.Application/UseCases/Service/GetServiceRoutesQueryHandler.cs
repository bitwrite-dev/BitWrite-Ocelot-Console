using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Service;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainRoute = BitWrite.OcelotConsole.Domain.Aggregates.Route.Route;
using DomainService = BitWrite.OcelotConsole.Domain.Aggregates.Service.Service;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public class GetServiceRoutesQueryHandler
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IRouteRepository _routeRepository;

    public GetServiceRoutesQueryHandler(
        IServiceRepository serviceRepository,
        IRouteRepository routeRepository)
    {
        _serviceRepository = serviceRepository;
        _routeRepository = routeRepository;
    }

    public async Task<ServiceRoutesResponse?> HandleAsync(GetServiceRoutesQuery query, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetAsync(query.Id, cancellationToken);
        if (service == null)
            return null;

        // Get all routes for this service
        var allRoutes = await _routeRepository.GetAllAsync(cancellationToken);
        var serviceRoutes = allRoutes.Where(r => r.ServiceId == query.Id).ToList();

        var response = new ServiceRoutesResponse(
            serviceRoutes.Select(r => new RouteSummary(
                r.Id,
                r.Key!,
                r.Method.Value,
                r.UpstreamPath.Value,
                r.IsEnabled,
                r.IsDraft
            )).ToList()
        );

        return response;
    }
}
