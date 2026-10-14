using BitWrite.OcelotControl.Domain.Events;
using BitWrite.OcelotControl.Domain.Exceptions;

namespace BitWrite.OcelotControl.Domain.Aggregates.GlobalConfiguration;

/// <summary>
/// GlobalConfiguration Aggregate Root - Gateway-Wide Settings (§9A.2)
/// Singleton aggregate - one per gateway (§10.1).
/// </summary>
public class GlobalConfiguration
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public string? BaseUrl { get; private set; }
    public string? RequestIdKey { get; private set; }
    public string? DownstreamScheme { get; private set; }
    public int? Timeout { get; private set; }
    public RateLimitConfig? RateLimit { get; private set; }
    public QoSConfig? QoS { get; private set; }
    public HttpHandlerConfig? HttpHandler { get; private set; }
    public ServiceDiscoveryConfig? ServiceDiscovery { get; private set; }

    /// <summary>
    /// Whether this configuration has changed since a snapshot last captured it.
    /// </summary>
    /// <remarks>
    /// Set by every change and cleared when a snapshot is created, because a
    /// snapshot captures the whole current management state — the settings
    /// included. See <see cref="Route.Route.IsDraft"/> for why the flag exists.
    /// </remarks>
    public bool IsDraft { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private GlobalConfiguration() { }

    /// <summary>
    /// Creates or retrieves the global configuration (singleton pattern).
    /// </summary>
    public static GlobalConfiguration Create()
    {
        return new GlobalConfiguration
        {
            Id = Guid.NewGuid(),
            BaseUrl = "http://localhost:5000",
            RequestIdKey = "X-Request-Id",
            DownstreamScheme = "http",
            Timeout = 90000,
            IsDraft = true,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Reconstitutes the global configuration from persisted state, preserving
    /// its identity, updated-at stamp and nested configuration objects.
    ///
    /// Infrastructure adapters must use this rather than <see cref="Create"/>:
    /// Create mints a new <see cref="Guid"/> and resets every field to its
    /// default, so a read followed by a save would silently revert the stored
    /// configuration.
    /// </summary>
    public static GlobalConfiguration Reconstitute(
        Guid id,
        string? baseUrl,
        string? requestIdKey,
        string? downstreamScheme,
        int? timeout,
        RateLimitConfig? rateLimit,
        QoSConfig? qoS,
        HttpHandlerConfig? httpHandler,
        ServiceDiscoveryConfig? serviceDiscovery,
        DateTimeOffset updatedAt,
        bool isDraft = false)
    {
        var config = new GlobalConfiguration
        {
            Id = id,
            BaseUrl = baseUrl,
            RequestIdKey = requestIdKey,
            DownstreamScheme = downstreamScheme,
            Timeout = timeout,
            RateLimit = rateLimit,
            QoS = qoS,
            HttpHandler = httpHandler,
            ServiceDiscovery = serviceDiscovery,
            IsDraft = isDraft,
            UpdatedAt = updatedAt
        };

        return config;
    }

    /// <summary>
    /// Marks the configuration as captured by the snapshot being created.
    /// </summary>
    /// <remarks>
    /// Called when a snapshot is sealed, because the snapshot is built from the
    /// whole management state, settings included. Does not stamp
    /// <see cref="UpdatedAt"/>: clearing the flag records that the settings were
    /// taken, not that they changed.
    /// </remarks>
    public void MarkAsCaptured(string correlationId = "")
    {
        IsDraft = false;
    }

    /// <summary>
    /// The one place a change to this configuration is recorded.
    /// </summary>
    /// <remarks>
    /// Every mutator goes through here so a change cannot stamp
    /// <see cref="UpdatedAt"/> without flagging the configuration as pending
    /// capture.
    /// </remarks>
    private void MarkDirty()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        IsDraft = true;
    }

    /// <summary>
    /// Updates the base URL.
    /// </summary>
    public void SetBaseUrl(string? baseUrl)
    {
        BaseUrl = baseUrl?.Trim();
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates the request ID key.
    /// </summary>
    public void SetRequestIdKey(string? requestIdKey)
    {
        RequestIdKey = requestIdKey?.Trim();
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates the downstream scheme.
    /// </summary>
    public void SetDownstreamScheme(string? scheme)
    {
        if (!string.IsNullOrWhiteSpace(scheme))
        {
            var validSchemes = new[] { "http", "https", "grpc", "grpcs" };
            if (!validSchemes.Contains(scheme.ToLowerInvariant()))
                throw new DomainException($"Invalid scheme: {scheme}", "INVALID_SCHEME");
        }

        DownstreamScheme = scheme?.Trim();
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates the timeout.
    /// </summary>
    public void SetTimeout(int? timeout)
    {
        if (timeout.HasValue && timeout <= 0)
            throw new DomainException("Timeout must be positive", "INVALID_TIMEOUT");

        Timeout = timeout;
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates rate limit configuration.
    /// </summary>
    public void SetRateLimit(RateLimitConfig? config)
    {
        RateLimit = config;
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates QoS configuration.
    /// </summary>
    public void SetQoS(QoSConfig? config)
    {
        QoS = config;
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates HTTP handler configuration.
    /// </summary>
    public void SetHttpHandler(HttpHandlerConfig? config)
    {
        HttpHandler = config;
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    /// <summary>
    /// Updates service discovery configuration.
    /// </summary>
    public void SetServiceDiscovery(ServiceDiscoveryConfig? config)
    {
        ServiceDiscovery = config;
        MarkDirty();
        AddDomainEvent(new GlobalConfigurationUpdated());
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class RateLimitConfig
{
    public bool EnableRateLimiting { get; set; }
    public string? HttpStatusCode { get; set; }
}

public class QoSConfig
{
    public int TimeoutValue { get; set; } = 90000;
    public int DurationOfBreak { get; set; } = 30000;
}

public class HttpHandlerConfig
{
    public bool UseProxy { get; set; } = true;
    public bool Expect100Continue { get; set; }
    public int? MaxConnectionsPerServer { get; set; }
}

public class ServiceDiscoveryConfig
{
    public string? Provider { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Type { get; set; }
    public Dictionary<string, string> Configuration { get; set; } = new();
}
