using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Gateway;
using BitWrite.OcelotConsole.Domain.Aggregates.Publication;
using BitWrite.OcelotConsole.Domain.Aggregates.Snapshot;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using StackExchange.Redis;
using DomainPublication = BitWrite.OcelotConsole.Domain.Aggregates.Publication.Publication;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

public class CreatePublicationCommandHandler
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IPublicationRepository _publicationRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ISnapshotIntegrityVerifier _integrityVerifier;

    public CreatePublicationCommandHandler(
        ISnapshotRepository snapshotRepository,
        IPublicationRepository publicationRepository,
        IGatewayRepository gatewayRepository,
        ISnapshotIntegrityVerifier integrityVerifier)
    {
        _snapshotRepository = snapshotRepository;
        _publicationRepository = publicationRepository;
        _gatewayRepository = gatewayRepository;
        _integrityVerifier = integrityVerifier;
    }

    public async Task<CreatePublicationResponse> HandleAsync(CreatePublicationCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Parse and verify Snapshot exists
        if (!int.TryParse(command.SnapshotVersion, out var versionInt))
        {
            throw new ArgumentException($"Invalid snapshot version: {command.SnapshotVersion}");
        }
        var snapshotVersion = SnapshotVersion.From(versionInt);

        var snapshot = await _snapshotRepository.GetAsync(snapshotVersion, cancellationToken);
        if (snapshot == null)
        {
            throw new InvalidOperationException($"Snapshot {snapshotVersion} not found");
        }

        // 1.5. Check for existing pending publication for the same snapshot (dedupe)
        var existingPublications = await _publicationRepository.GetAllAsync(cancellationToken);
        var pendingForSameSnapshot = existingPublications
            .FirstOrDefault(p => p.SnapshotVersion == snapshotVersion && p.Status == PublicationStatus.Pending);
        if (pendingForSameSnapshot != null)
        {
            throw new InvalidOperationException($"A publication for snapshot {snapshotVersion} is already pending (publication {pendingForSameSnapshot.Id}). Cancel it first or wait for it to complete.");
        }
        // 2. Verify status allows publishing (Ready or Published)
        if (snapshot.Status != SnapshotStatus.Ready && snapshot.Status != SnapshotStatus.Published)
        {
            throw new InvalidOperationException($"Snapshot {snapshotVersion} cannot be published. Current status: {snapshot.Status}");
        }

        // 3. Verify snapshot integrity
        var computedHash = _integrityVerifier.ComputeHash(snapshot.Content);
        if (!snapshot.VerifyIntegrity(computedHash))
        {
            throw new InvalidOperationException($"Snapshot {snapshotVersion} integrity verification failed");
        }

        // 4. Get target gateways
        var targetGatewayIds = command.TargetGatewayIds?.Select(GatewayId.From).ToList()
            ?? (await _gatewayRepository.GetAllAsync(cancellationToken)).Select(g => g.Id).ToList();

        if (targetGatewayIds.Count == 0)
        {
            throw new InvalidOperationException("No target gateways available for publication");
        }

        // 5. Verify gateway environment matches publication environment
        var gateways = await _gatewayRepository.GetAllAsync(cancellationToken);
        var mismatched = gateways
            .Where(gateway => targetGatewayIds.Contains(gateway.Id))
            .Where(gateway => gateway.Environment != command.Environment)
            .ToList();

        if (mismatched.Count > 0)
        {
            throw new InvalidOperationException(
                $"Publication is for environment '{command.Environment}' but these gateways serve another: " +
                string.Join(", ", mismatched.Select(g => g.Id.Value.ToString())));
        }

        // 6. Create Publication aggregate (but don't execute yet)
        var publication = DomainPublication.Start(
            snapshotVersion,
            command.InitiatedBy,
            targetGatewayIds,
            command.CorrelationId);

        // 7. Persist Publication (status = Pending)
        await _publicationRepository.AddAsync(publication, cancellationToken);

        // 8. Return response for review
        var gatewayProgress = targetGatewayIds.Select(id => new GatewayProgressResponse(
            GatewayId: id.Value.ToString(),
            GatewayName: gateways.First(g => g.Id == id).Name,
            Status: "Pending",
            ReceivedAt: null,
            ValidatedAt: null,
            AppliedAt: null,
            HealthyAt: null,
            FailedAt: null,
            IsValid: null,
            FailureReason: null
        )).ToList();

        return new CreatePublicationResponse(
            PublicationId: publication.Id.Value.ToString(),
            SnapshotVersion: snapshotVersion.Value,
            Status: publication.Status.Value,
            GatewayProgress: gatewayProgress
        );
    }
}
