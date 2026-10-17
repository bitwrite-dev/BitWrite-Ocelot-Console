using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Gateway;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainGateway = BitWrite.OcelotConsole.Domain.Aggregates.Gateway.Gateway;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public class GetGatewayQueryHandler
{
    private readonly IGatewayRepository _gatewayRepository;

    public GetGatewayQueryHandler(IGatewayRepository gatewayRepository)
    {
        _gatewayRepository = gatewayRepository;
    }

    public async Task<GatewayResponse?> HandleAsync(GetGatewayQuery query, CancellationToken cancellationToken = default)
    {
        var gateway = await _gatewayRepository.GetAsync(query.Id, cancellationToken);
        return gateway != null ? MapToResponse(gateway) : null;
    }

    private static GatewayResponse MapToResponse(DomainGateway gateway)
    {
        return new GatewayResponse(
            gateway.Id,
            gateway.Name,
            gateway.Description,
            gateway.Status,
            gateway.CreatedAt,
            gateway.UpdatedAt,
            gateway.LastHeartbeat
        );
    }
}
