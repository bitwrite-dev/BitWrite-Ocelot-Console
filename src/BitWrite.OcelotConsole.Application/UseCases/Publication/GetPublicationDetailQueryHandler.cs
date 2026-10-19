using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainGatewayDeploymentState = BitWrite.OcelotConsole.Domain.Aggregates.Publication.GatewayDeploymentState;
using DomainPublication = BitWrite.OcelotConsole.Domain.Aggregates.Publication.Publication;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class GetPublicationDetailQueryHandler
{
    private readonly IPublicationRepository _publicationRepository;

    public GetPublicationDetailQueryHandler(IPublicationRepository publicationRepository)
    {
        _publicationRepository = publicationRepository;
    }

    public async Task<PublicationResponse?> HandleAsync(
        GetPublicationDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        var publication = await _publicationRepository.GetAsync(
            PublicationId.From(Guid.Parse(query.Id)),
            cancellationToken);

        if (publication is null)
            return null;

        return MapToResponse(publication);
    }

    private static PublicationResponse MapToResponse(DomainPublication publication)
    {
        return new PublicationResponse(
            publication.Id,
            publication.SnapshotVersion,
            publication.Status,
            publication.InitiatedBy,
            publication.StartedAt,
            publication.CompletedAt,
            publication.FailureReason,
            publication.ProgressPercentage,
            publication.GatewayStates.Values.Select(MapGatewayState).ToList()
        );
    }

    private static GatewayDeploymentStateResponse MapGatewayState(DomainGatewayDeploymentState state)
    {
        return new GatewayDeploymentStateResponse(
            state.GatewayId,
            state.Status,
            state.ReceivedAt,
            state.ValidatedAt,
            state.AppliedAt,
            state.HealthyAt,
            state.IsValid,
            state.FailedAt,
            state.FailureReason
        );
    }
}
