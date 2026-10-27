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

    public GetPublicationProgressQueryHandler(
        IPublicationRepository publicationRepository,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _publicationRepository = publicationRepository;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<PublicationProgressResponse?> HandleAsync(GetPublicationProgressQuery query, CancellationToken cancellationToken = default)
    {
        var publicationId = PublicationId.From(query.PublicationId);

        // Always read from database (authoritative source)
        var publication = await _publicationRepository.GetAsync(publicationId, CancellationToken.None);

        if (publication == null)
        {
            return null;
        }

        var gatewayProgress = publication.GatewayStates.Select(kvp => new GatewayProgressResponse(
            GatewayId: kvp.Key.Value.ToString(),
            GatewayName: kvp.Key.Value.ToString(),
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
