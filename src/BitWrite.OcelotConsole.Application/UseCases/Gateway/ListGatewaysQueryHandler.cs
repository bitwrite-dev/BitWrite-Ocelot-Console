using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Gateway;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainGateway = BitWrite.OcelotConsole.Domain.Aggregates.Gateway.Gateway;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public class ListGatewaysQueryHandler
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IRuntimeInstanceRepository _runtimeInstanceRepository;

    public ListGatewaysQueryHandler(
        IGatewayRepository gatewayRepository,
        IRuntimeInstanceRepository runtimeInstanceRepository)
    {
        _gatewayRepository = gatewayRepository;
        _runtimeInstanceRepository = runtimeInstanceRepository;
    }

    public async Task<GatewayListResponse> HandleAsync(ListGatewaysQuery query, CancellationToken cancellationToken = default)
    {
        var gateways = await _gatewayRepository.GetAllAsync(cancellationToken);

        var totalCount = gateways.Count;
        var pagedGateways = gateways
            .OrderBy(g => g.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Read once for the page rather than per gateway, so the list stays a
        // fixed number of round trips rather than one per row.
        var heartbeats = await _runtimeInstanceRepository.GetAllAsync(cancellationToken);
        var lastSeen = heartbeats.ToDictionary(
            instance => instance.GatewayId,
            instance => instance.LastHeartbeat);

        var response = new GatewayListResponse(
            pagedGateways.Select(gateway =>
            {
                var heartbeat = gateway.LastHeartbeat;
                if (!heartbeat.HasValue && lastSeen.TryGetValue(gateway.Id, out var beat))
                    heartbeat = beat;
                return MapToResponse(gateway, heartbeat);
            }).ToList(),
            totalCount,
            query.Page,
            query.PageSize
        );

        return response;
    }

    private static GatewayResponse MapToResponse(DomainGateway gateway, DateTimeOffset? lastHeartbeat)
    {
        return new GatewayResponse(
            gateway.Id,
            gateway.Name,
            gateway.Description,
            gateway.Status,
            gateway.CreatedAt,
            gateway.UpdatedAt,
            lastHeartbeat
        );
    }
}
