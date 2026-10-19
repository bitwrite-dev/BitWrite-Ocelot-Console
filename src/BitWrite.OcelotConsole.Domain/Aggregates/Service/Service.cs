using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.Service;

/// <summary>
/// Service Aggregate Root - Logical Downstream Service (§9A.2)
/// Represents a logical service with one or more host endpoints.
/// </summary>
public class Service
{
    private readonly List<DomainEvent> _domainEvents = new();
    private readonly List<ServiceEndpoint> _endpoints = new();

    public ServiceId Id { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this service has changed since a snapshot last captured it.
    /// </summary>
    /// <remarks>
    /// Set by every change to the service and cleared when a snapshot is created,
    /// because a snapshot captures the whole current management state — so a
    /// service that is not draft was sealed exactly as it stands. See
    /// <see cref="Route.Route.IsDraft"/> for why the flag exists.
    /// </remarks>
    public bool IsDraft { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ServiceEndpoint> Endpoints => _endpoints.AsReadOnly();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Service() { }

    /// <summary>
    /// Factory method to create a new service.
    /// </summary>
    public static Service Create(string name, string? description = null, string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty", "INVALID_SERVICE_NAME");

        var service = new Service
        {
            Id = ServiceId.New(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsDraft = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        service.AddDomainEvent(new ServiceCreated(service.Id, service.Name));
        return service;
    }

    /// <summary>
    /// Reconstitutes a service from persisted state, preserving its identity,
    /// timestamps and endpoints.
    ///
    /// Infrastructure adapters must use this rather than <see cref="Create"/>:
    /// Create mints a new <see cref="ServiceId"/> and stamps
    /// <see cref="DateTimeOffset.UtcNow"/>, so round-tripping through a
    /// repository would hand out a different id on every read and break any
    /// aggregate that references this service. It also has no endpoint
    /// parameter, so endpoints would be silently dropped.
    /// </summary>
    public static Service Reconstitute(
        ServiceId id,
        string name,
        string? description,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        bool isDraft = false,
        IReadOnlyList<ServiceEndpoint>? endpoints = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty", "INVALID_SERVICE_NAME");

        var service = new Service
        {
            Id = id,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsDraft = isDraft,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Added directly rather than through AddHost, which would re-validate and
        // re-stamp state that is already known-good, and would raise events for a
        // fact that has already been recorded.
        if (endpoints is not null)
        {
            service._endpoints.AddRange(endpoints);
        }

        return service;
    }

    /// <summary>
    /// Marks this service as captured by the snapshot being created.
    /// </summary>
    /// <remarks>
    /// Called for every service when a snapshot is sealed, because the snapshot is
    /// built from the whole management state. Does not stamp
    /// <see cref="UpdatedAt"/>: clearing the flag records that the content was
    /// taken, not that it changed.
    /// </remarks>
    public void MarkAsCaptured(string correlationId = "")
    {
        IsDraft = false;
    }

    /// <summary>
    /// The one place a change to this service is recorded.
    /// </summary>
    /// <remarks>
    /// Every mutator goes through here so a change cannot stamp
    /// <see cref="UpdatedAt"/> without flagging the service as pending capture.
    /// </remarks>
    private void MarkDirty()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        IsDraft = true;
    }

    /// <summary>
    /// Adds a host endpoint to the service.
    /// </summary>
    public void AddHost(string host, int port, int weight = 1, string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new DomainException("Host cannot be empty", "INVALID_HOST");

        if (port <= 0 || port > 65535)
            throw new DomainException($"Invalid port: {port}", "INVALID_PORT");

        if (weight <= 0)
            throw new DomainException("Weight must be positive", "INVALID_WEIGHT");

        if (_endpoints.Any(e => e.Host == host.Trim().ToLowerInvariant() && e.Port == port))
            throw new DomainException($"Endpoint {host}:{port} already exists", "DUPLICATE_ENDPOINT");

        var endpoint = new ServiceEndpoint
        {
            Host = host.Trim().ToLowerInvariant(),
            Port = port,
            Weight = weight,
            IsActive = true
        };

        _endpoints.Add(endpoint);
        MarkDirty();

        AddDomainEvent(new ServiceHostAdded(Id, endpoint.Host, endpoint.Port));
    }

    /// <summary>
    /// Removes a host endpoint from the service.
    /// </summary>
    public void RemoveHost(string host, int port, string correlationId = "")
    {
        var endpoint = _endpoints.FirstOrDefault(e =>
            e.Host == host.Trim().ToLowerInvariant() && e.Port == port);

        if (endpoint == null)
            throw new DomainException($"Endpoint {host}:{port} not found", "ENDPOINT_NOT_FOUND");

        if (_endpoints.Count <= 1)
            throw new DomainException("Service must have at least one endpoint", "NO_ENDPOINTS");

        _endpoints.Remove(endpoint);
        MarkDirty();

        AddDomainEvent(new ServiceHostRemoved(Id, host, port));
    }

    /// <summary>
    /// Updates the weight of a host endpoint.
    /// </summary>
    public void UpdateHostWeight(string host, int port, int weight)
    {
        if (weight <= 0)
            throw new DomainException("Weight must be positive", "INVALID_WEIGHT");

        var endpoint = _endpoints.FirstOrDefault(e =>
            e.Host == host.Trim().ToLowerInvariant() && e.Port == port);

        if (endpoint == null)
            throw new DomainException($"Endpoint {host}:{port} not found", "ENDPOINT_NOT_FOUND");

        endpoint.Weight = weight;
        MarkDirty();
    }

    /// <summary>
    /// Enables or disables a host endpoint.
    /// </summary>
    public void SetHostActive(string host, int port, bool isActive)
    {
        var endpoint = _endpoints.FirstOrDefault(e =>
            e.Host == host.Trim().ToLowerInvariant() && e.Port == port);

        if (endpoint == null)
            throw new DomainException($"Endpoint {host}:{port} not found", "ENDPOINT_NOT_FOUND");

        endpoint.IsActive = isActive;
        MarkDirty();
    }

    /// <summary>
    /// Updates the service name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty", "INVALID_SERVICE_NAME");

        var oldName = Name;
        Name = name.Trim();
        MarkDirty();

        AddDomainEvent(new ServiceUpdated(Id));
    }

    /// <summary>
    /// Updates the service description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        MarkDirty();

        AddDomainEvent(new ServiceUpdated(Id));
    }

    /// <summary>
    /// Updates the service name and description.
    /// </summary>
    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name cannot be empty", "INVALID_SERVICE_NAME");

        Name = name.Trim();
        Description = description?.Trim();
        MarkDirty();

        AddDomainEvent(new ServiceUpdated(Id));
    }

    /// <summary>
    /// Gets all active endpoints.
    /// </summary>
    public IReadOnlyList<ServiceEndpoint> GetActiveEndpoints()
    {
        return _endpoints.Where(e => e.IsActive).ToList().AsReadOnly();
    }

    /// <summary>
    /// Marks the service as deleted.
    /// </summary>
    public void Delete()
    {
        AddDomainEvent(new ServiceDeleted(Id));
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

/// <summary>
/// Service endpoint entity.
/// </summary>
public class ServiceEndpoint
{
    public string Host { get; internal set; } = string.Empty;
    public int Port { get; internal set; }
    public int Weight { get; internal set; } = 1;
    public bool IsActive { get; internal set; } = true;
}
