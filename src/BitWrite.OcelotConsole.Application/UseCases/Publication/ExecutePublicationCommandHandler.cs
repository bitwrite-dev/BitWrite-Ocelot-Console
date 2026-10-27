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

public class ExecutePublicationCommandHandler
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IPublicationRepository _publicationRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ISnapshotIntegrityVerifier _integrityVerifier;
    private readonly IDistributedLock _distributedLock;
    private readonly IRedisPublisher _redisPublisher;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public ExecutePublicationCommandHandler(
        ISnapshotRepository snapshotRepository,
        IPublicationRepository publicationRepository,
        IGatewayRepository gatewayRepository,
        ISnapshotIntegrityVerifier integrityVerifier,
        IDistributedLock distributedLock,
        IRedisPublisher redisPublisher,
        IDomainEventDispatcher eventDispatcher,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _snapshotRepository = snapshotRepository;
        _publicationRepository = publicationRepository;
        _gatewayRepository = gatewayRepository;
        _integrityVerifier = integrityVerifier;
        _distributedLock = distributedLock;
        _redisPublisher = redisPublisher;
        _eventDispatcher = eventDispatcher;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<string> HandleAsync(ExecutePublicationCommand command, CancellationToken cancellationToken = default)
    {
        var publicationId = PublicationId.From(command.PublicationId);

        // 1. Get the publication
        var publication = await _publicationRepository.GetAsync(publicationId, cancellationToken);
        if (publication == null)
        {
            throw new InvalidOperationException($"Publication {publicationId} not found");
        }

        if (publication.Status != PublicationStatus.Pending)
        {
            throw new InvalidOperationException($"Publication is not in pending status (current: {publication.Status})");
        }

        // 2. Get the snapshot
        var snapshotVersion = publication.SnapshotVersion;
        var snapshot = await _snapshotRepository.GetAsync(snapshotVersion, cancellationToken);
        if (snapshot == null)
        {
            throw new InvalidOperationException($"Snapshot {snapshotVersion} not found");
        }

        // 3. Verify snapshot integrity
        var computedHash = _integrityVerifier.ComputeHash(snapshot.Content);
        if (!snapshot.VerifyIntegrity(computedHash))
        {
            throw new InvalidOperationException($"Snapshot {snapshotVersion} integrity verification failed");
        }

        // 4. Acquire publish lock
        var lockResult = await _distributedLock.AcquireAsync(
            $"publish:{snapshot.Version.Value}",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);

        if (!lockResult.Success)
        {
            throw new InvalidOperationException($"Failed to acquire publish lock: {lockResult.ErrorMessage}");
        }

        try
        {
            // 5. Mark publication as in progress (persist to allow progress tracking)
            // Publication.Start already sets status to Pending
            // We just need to ensure it's persisted with gateway states
            await _publicationRepository.UpdateAsync(publication, cancellationToken);

            // 6. Atomic: Set ocelot:runtime:current:{env} = version
            var db = _connectionMultiplexer.GetDatabase();
            await db.StringSetAsync(
                $"ocelot:runtime:current:{command.Environment.Value.ToLowerInvariant()}",
                snapshot.Version.Value.ToString());

            // 6.5. Redis Pub/Sub: Notify gateways of new version
            await _redisPublisher.PublishAsync(
                "ocelot:snapshot:published",
                new
                {
                    Version = snapshot.Version.Value.ToString(),
                    Environment = command.Environment.Value,
                    PublicationId = publication.Id.Value.ToString()
                },
                cancellationToken);

            // 7. Mark publication as completed
            publication.Complete();
            await _publicationRepository.UpdateAsync(publication, cancellationToken);

            // 8. Mark snapshot as Published (if not already)
            if (snapshot.Status != SnapshotStatus.Published)
            {
                snapshot.Publish();
                await _snapshotRepository.UpdateAsync(snapshot, cancellationToken);
            }

            // 6.6. Update publication progress in Redis for real-time tracking (after Complete)
            var dbProgress = _connectionMultiplexer.GetDatabase();
            var progressKey = $"publication:progress:{publication.Id.Value}";
            var gatewayProgress = publication.GatewayStates.Select(kvp => new
            {
                GatewayId = kvp.Key.Value.ToString(),
                GatewayName = kvp.Key.Value.ToString(),
                Status = kvp.Value.Status,
                ReceivedAt = (DateTimeOffset?)kvp.Value.ReceivedAt,
                ValidatedAt = (DateTimeOffset?)kvp.Value.ValidatedAt,
                AppliedAt = (DateTimeOffset?)kvp.Value.AppliedAt,
                HealthyAt = (DateTimeOffset?)kvp.Value.HealthyAt,
                FailedAt = (DateTimeOffset?)kvp.Value.FailedAt,
                IsValid = kvp.Value.IsValid,
                FailureReason = kvp.Value.FailureReason
            }).ToList();

            var progressData = new
            {
                PublicationId = publication.Id.Value.ToString(),
                SnapshotVersion = snapshot.Version.Value,
                Status = publication.Status.Value,
                ProgressPercentage = publication.ProgressPercentage,
                GatewayProgress = gatewayProgress,
                StartedAt = publication.StartedAt,
                CompletedAt = publication.CompletedAt,
                FailureReason = publication.FailureReason
            };

            await dbProgress.StringSetAsync(progressKey, System.Text.Json.JsonSerializer.Serialize(progressData), TimeSpan.FromHours(24));

            // 9. Raise domain events
            foreach (var domainEvent in publication.DomainEvents)
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }

            // 10. Raise audit event
            var auditEvent = new AuditRecorded(
                command.InitiatedBy,
                "PublishSnapshot",
                "Publication",
                publication.Id.Value.ToString(),
                "Completed"
            );
            await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

            // 11. Raise integration event
            var integrationEvent = new SnapshotPublishedIntegrationEvent(snapshot.Version, DateTimeOffset.UtcNow);
            await _eventDispatcher.DispatchAsync(integrationEvent, cancellationToken);

            return publication.Id.Value.ToString();
        }
        catch (Exception ex)
        {
            // Mark publication as failed
            publication.RecordFailureWithoutGateway(ex.Message);
            await _publicationRepository.UpdateAsync(publication, cancellationToken);

            // Update Redis progress with failed status
            try
            {
                var dbProgress = _connectionMultiplexer.GetDatabase();
                var progressKey = $"publication:progress:{publication.Id.Value}";
                var gatewayProgress = publication.GatewayStates.Select(kvp => new
                {
                    GatewayId = kvp.Key.Value.ToString(),
                    GatewayName = kvp.Key.Value.ToString(),
                    Status = kvp.Value.Status,
                    ReceivedAt = (DateTimeOffset?)kvp.Value.ReceivedAt,
                    ValidatedAt = (DateTimeOffset?)kvp.Value.ValidatedAt,
                    AppliedAt = (DateTimeOffset?)kvp.Value.AppliedAt,
                    HealthyAt = (DateTimeOffset?)kvp.Value.HealthyAt,
                    FailedAt = (DateTimeOffset?)kvp.Value.FailedAt,
                    IsValid = kvp.Value.IsValid,
                    FailureReason = kvp.Value.FailureReason
                }).ToList();

                var progressData = new
                {
                    PublicationId = publication.Id.Value.ToString(),
                    SnapshotVersion = snapshot.Version.Value,
                    Status = publication.Status.Value,
                    ProgressPercentage = publication.ProgressPercentage,
                    GatewayProgress = gatewayProgress,
                    StartedAt = publication.StartedAt,
                    CompletedAt = publication.CompletedAt,
                    FailureReason = publication.FailureReason
                };

                await dbProgress.StringSetAsync(progressKey, System.Text.Json.JsonSerializer.Serialize(progressData), TimeSpan.FromHours(24));
            }
            catch
            {
                // Ignore Redis errors in catch block
            }

            // Release lock on failure
            if (lockResult.LockId != null)
            {
                await _distributedLock.ReleaseAsync($"publish:{snapshot.Version.Value}", lockResult.LockId, cancellationToken);
            }
            throw;
        }
    }
}
