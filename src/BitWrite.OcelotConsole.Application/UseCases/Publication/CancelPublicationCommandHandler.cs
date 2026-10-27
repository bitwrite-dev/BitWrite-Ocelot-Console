using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class CancelPublicationCommandHandler
{
    private readonly IPublicationRepository _publicationRepository;

    public CancelPublicationCommandHandler(IPublicationRepository publicationRepository)
    {
        _publicationRepository = publicationRepository;
    }

    public async Task<bool> HandleAsync(CancelPublicationCommand command, CancellationToken cancellationToken = default)
    {
        var publicationId = PublicationId.From(command.PublicationId);

        var publication = await _publicationRepository.GetAsync(publicationId, cancellationToken);
        if (publication == null)
        {
            throw new InvalidOperationException($"Publication {publicationId} not found");
        }

        if (publication.Status != PublicationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot cancel publication in {publication.Status} status. Only Pending publications can be cancelled.");
        }

        publication.RecordFailureWithoutGateway($"Cancelled by {command.InitiatedBy}: {command.CorrelationId}");
        await _publicationRepository.UpdateAsync(publication, cancellationToken);

        return true;
    }
}
