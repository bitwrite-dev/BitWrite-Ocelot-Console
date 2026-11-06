using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class GetPublicationProgressQueryHandler
{
    private readonly IPublicationRepository _publicationRepository;
    private readonly IPublicationProgressRepository _progressRepository;
    private readonly IGatewayRepository _gatewayRepository;

    public GetPublicationProgressQueryHandler(
        IPublicationRepository publicationRepository,
        IPublicationProgressRepository progressRepository,
        IGatewayRepository gatewayRepository)
    {
        _publicationRepository = publicationRepository;
        _progressRepository = progressRepository;
        _gatewayRepository = gatewayRepository;
    }

    public async Task<PublicationProgressResponse?> HandleAsync(GetPublicationProgressQuery query, CancellationToken cancellationToken = default)
    {
        var publicationId = PublicationId.From(query.PublicationId);

        // Try to read from Redis cache first (real-time data from GatewayActivationProcessor)
        var cachedProgress = await _progressRepository.GetProgressAsync(publicationId, cancellationToken);

        if (cachedProgress != null)
        {
            return new PublicationProgressResponse(
                PublicationId: cachedProgress.PublicationId,
                SnapshotVersion: cachedProgress.SnapshotVersion,
                Status: cachedProgress.Status,
                ProgressPercentage: cachedProgress.ProgressPercentage,
                GatewayProgress: cachedProgress.GatewayProgress.Select(g => new GatewayProgressResponse(
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
                StartedAt: cachedProgress.StartedAt,
                CompletedAt: cachedProgress.CompletedAt,
                FailureReason: cachedProgress.FailureReason
            );
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
}
