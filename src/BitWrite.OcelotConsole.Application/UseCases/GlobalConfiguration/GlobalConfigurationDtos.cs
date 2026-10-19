using BitWrite.OcelotConsole.Domain.Aggregates.GlobalConfiguration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.FeatureConfig;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.GlobalConfiguration;

public record GlobalConfigurationResponse(
    string Id,
    string? BaseUrl,
    string? RequestIdKey,
    string? DownstreamScheme,
    int? Timeout,
    RateLimitConfig? RateLimit,
    QoSConfig? QoS,
    HttpHandlerConfig? HttpHandler,
    ServiceDiscoveryConfig? ServiceDiscovery,
    DateTimeOffset UpdatedAt,
    bool IsDraft
);
