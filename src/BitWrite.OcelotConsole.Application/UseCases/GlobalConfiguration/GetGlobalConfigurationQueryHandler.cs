using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.GlobalConfiguration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.FeatureConfig;
using DomainGlobalConfig = BitWrite.OcelotConsole.Domain.Aggregates.GlobalConfiguration.GlobalConfiguration;

namespace BitWrite.OcelotConsole.Application.UseCases.GlobalConfiguration;

public class GetGlobalConfigurationQueryHandler
{
    private readonly IGlobalConfigurationRepository _globalConfigRepository;

    public GetGlobalConfigurationQueryHandler(IGlobalConfigurationRepository globalConfigRepository)
    {
        _globalConfigRepository = globalConfigRepository;
    }

    public async Task<GlobalConfigurationResponse> HandleAsync(GetGlobalConfigurationQuery query, CancellationToken cancellationToken = default)
    {
        var config = await _globalConfigRepository.GetAsync(cancellationToken);
        return MapToResponse(config);
    }

    private static GlobalConfigurationResponse MapToResponse(DomainGlobalConfig config)
    {
        return new GlobalConfigurationResponse(
            config.Id.ToString(),
            config.BaseUrl,
            config.RequestIdKey,
            config.DownstreamScheme,
            config.Timeout,
            config.RateLimit,
            config.QoS,
            config.HttpHandler,
            config.ServiceDiscovery,
            config.UpdatedAt,
            config.IsDraft
        );
    }
}
