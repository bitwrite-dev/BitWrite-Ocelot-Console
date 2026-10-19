using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Snapshot;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using DomainSnapshot = BitWrite.OcelotConsole.Domain.Aggregates.Snapshot.Snapshot;

namespace BitWrite.OcelotConsole.Application.UseCases.Snapshot;

public class GetSnapshotQueryHandler
{
    private readonly ISnapshotRepository _snapshotRepository;

    public GetSnapshotQueryHandler(ISnapshotRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotResponse?> HandleAsync(GetSnapshotQuery query, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshotRepository.GetAsync(query.Version, cancellationToken);
        return snapshot != null ? MapToResponse(snapshot) : null;
    }

    private static SnapshotResponse MapToResponse(DomainSnapshot snapshot)
    {
        return new SnapshotResponse(
            snapshot.Version,
            snapshot.Hash,
            snapshot.Content,
            snapshot.Status,
            snapshot.CreatedBy,
            snapshot.CreatedAt,
            snapshot.PublishedAt,
            snapshot.ArchivedAt,
            snapshot.ValidationResults.Select(v => new ValidationResult(v.Rule, v.IsValid, v.Message)).ToList()
        );
    }
}
