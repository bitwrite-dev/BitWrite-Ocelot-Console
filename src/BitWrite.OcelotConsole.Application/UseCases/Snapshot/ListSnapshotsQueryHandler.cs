using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Snapshot;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using DomainSnapshot = BitWrite.OcelotConsole.Domain.Aggregates.Snapshot.Snapshot;

namespace BitWrite.OcelotConsole.Application.UseCases.Snapshot;

public class ListSnapshotsQueryHandler
{
    private readonly ISnapshotRepository _snapshotRepository;

    public ListSnapshotsQueryHandler(ISnapshotRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<SnapshotListResponse> HandleAsync(ListSnapshotsQuery query, CancellationToken cancellationToken = default)
    {
        var snapshots = await _snapshotRepository.GetAllAsync(cancellationToken);

        // Apply status filter if provided
        if (query.Status != null)
        {
            snapshots = snapshots.Where(s => s.Status == query.Status).ToList();
        }

        var totalCount = snapshots.Count;
        var pagedSnapshots = snapshots
            .OrderByDescending(s => s.Version.Value)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var response = new SnapshotListResponse(
            pagedSnapshots.Select(MapToResponse).ToList(),
            totalCount,
            query.Page,
            query.PageSize
        );

        return response;
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
