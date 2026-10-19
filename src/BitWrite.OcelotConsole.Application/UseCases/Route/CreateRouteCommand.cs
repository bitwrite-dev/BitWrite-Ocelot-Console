using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.FeatureConfig;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainHttpMethod = BitWrite.OcelotConsole.Domain.ValueObjects.Configuration.HttpMethod;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public record CreateRouteCommand(
    string Key,
    DomainHttpMethod Method,
    UpstreamPath UpstreamPath,
    ServiceId ServiceId,
    IReadOnlyList<DownstreamTarget> DownstreamTargets,
    string? Host = null,
    AuthenticationOptions? AuthenticationOptions = null,
    AuthorizationOptions? AuthorizationOptions = null,
    RateLimitOptions? RateLimitOptions = null,
    QoSOptions? QoSOptions = null,
    CacheOptions? CacheOptions = null,
    LoadBalancerOptions? LoadBalancerOptions = null,
    HeaderOptions? HeaderOptions = null,
    ClaimOptions? ClaimOptions = null,
    QueryOptions? QueryOptions = null,
    int priority = 0,
    bool routeIsCaseSensitive = false,
    string InitiatedBy = "",
    DownstreamPathTemplate? DownstreamTemplate = null,
    DomainHttpMethod? DownstreamMethod = null,
    string? DownstreamHttpVersion = null,
    string? DownstreamHttpVersionPolicy = null,
    bool AcceptAnyServerCertificate = false,
    IReadOnlyList<string>? DelegatingHandlers = null,
    HttpClientOptions? HttpClientOptions = null,
    int? TimeoutSeconds = null,
    string CorrelationId = ""
);
