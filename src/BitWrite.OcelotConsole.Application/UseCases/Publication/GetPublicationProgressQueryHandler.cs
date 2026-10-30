using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class GetPublicationProgressQueryHandler
{
    private readonly IPublicationRepository _publicationRepository;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IGatewayRepository _gatewayRepository;

    public GetPublicationProgressQueryHandler(
        IPublicationRepository publicationRepository,
        IConnectionMultiplexer connectionMultiplexer,
        IGatewayRepository gatewayRepository)
    {
        _publicationRepository = publicationRepository;
        _connectionMultiplexer = connectionMultiplexer;
        _gatewayRepository = gatewayRepository;
    }

    public async Task<PublicationProgressResponse?> HandleAsync(GetPublicationProgressQuery query, CancellationToken cancellationToken = default)
    {
        var publicationId = PublicationId.From(query.PublicationId);

        // Try to read from Redis cache first (real-time data from GatewayActivationProcessor)
        var db = _connectionMultiplexer.GetDatabase();
        var progressKey = $"publication:progress:{publicationId.Value}";
        var cachedProgress = await db.StringGetAsync(progressKey);

        if (cachedProgress.HasValue)
        {
            try
            {
                string jsonString = cachedProgress!;
                var cached = JsonSerializer.Deserialize<CachedPublicationProgress>(jsonString);
                if (cached != null)
                {
                    return new PublicationProgressResponse(
                        PublicationId: cached.PublicationId,
                        SnapshotVersion: cached.SnapshotVersion,
                        Status: cached.Status,
                        ProgressPercentage: cached.ProgressPercentage,
                        GatewayProgress: cached.GatewayProgress.Select(g => new GatewayProgressResponse(
                            GatewayId: g.GatewayId,
                            GatewayName: g.GatewayName,
                            Status: g.Status,
                            ReceivedAt: g.ReceivedAt,
                            ValidatedAt: g.ValidatedAt,
                            AppliedAt: g.AppliedAt,
                            HealthyAt: g.HealthyAt,
                            FailedAt: g.FailedAt,
                            IsValid: g.IsValid,
                            FailureReason: g.FailureReason
                        )).ToList(),
                        StartedAt: cached.StartedAt,
                        CompletedAt: cached.CompletedAt,
                        FailureReason: cached.FailureReason
                    );
                }
            }
            catch
            {
                // Fall through to database if cache is corrupted
            }
        }

        // Fallback to database (authoritative source)
        var publication = await _publicationRepository.GetAsync(publicationId, CancellationToken.None);

        if (publication == null)
        {
            return null;
        }

        var gateways = await _gatewayRepository.GetAllAsync(cancellationToken);
        var gatewayMap = gateways.ToDictionary(g => g.Id, g => g.Name);

        var gatewayProgress = publication.GatewayStates.Select(kvp => new GatewayProgressResponse(
            GatewayId: kvp.Key.Value.ToString(),
            GatewayName: gatewayMap.TryGetValue(kvp.Key, out var name) ? name : kvp.Key.Value.ToString(),
            Status: kvp.Value.Status,
            ReceivedAt: kvp.Value.ReceivedAt,
            ValidatedAt: kvp.Value.ValidatedAt,
            AppliedAt: kvp.Value.AppliedAt,
            HealthyAt: kvp.Value.HealthyAt,
            FailedAt: kvp.Value.FailedAt,
            IsValid: kvp.Value.IsValid,
            FailureReason: kvp.Value.FailureReason
        )).ToList();

        return new PublicationProgressResponse(
            PublicationId: publication.Id.Value.ToString(),
            SnapshotVersion: publication.SnapshotVersion.Value,
            Status: publication.Status.Value,
            ProgressPercentage: publication.ProgressPercentage,
            GatewayProgress: gatewayProgress,
            StartedAt: publication.StartedAt,
            CompletedAt: publication.CompletedAt,
            FailureReason: publication.FailureReason
        );
    }

    private class CachedPublicationProgress
    {
        public string PublicationId { get; set; } = "";
        public int SnapshotVersion { get; set; }
        public string Status { get; set; } = "";
        public double ProgressPercentage { get; set; }
        public List<CachedGatewayProgress> GatewayProgress { get; set; } = new();
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
    }

    private class CachedGatewayProgress
    {
        public string GatewayId { get; set; } = "";
        public string GatewayName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTimeOffset? ReceivedAt { get; set; }
        public DateTimeOffset? ValidatedAt { get; set; }
        public DateTimeOffset? AppliedAt { get; set; }
        public DateTimeOffset? HealthyAt { get; set; }
        public DateTimeOffset? FailedAt { get; set; }
        public bool? IsValid { get; set; }
        public string? FailureReason { get; set; }
    }
}
