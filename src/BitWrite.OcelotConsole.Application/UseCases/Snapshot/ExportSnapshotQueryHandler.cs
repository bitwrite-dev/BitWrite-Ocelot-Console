using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Snapshot;
using BitWrite.OcelotConsole.Domain.Aggregates.Snapshot;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Snapshot;

public class ExportSnapshotQueryHandler
{
    private readonly ISnapshotRepository _snapshotRepository;

    public ExportSnapshotQueryHandler(ISnapshotRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<ExportSnapshotResponse?> HandleAsync(ExportSnapshotQuery query, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshotRepository.GetAsync(query.Version, cancellationToken);
        if (snapshot == null)
            return null;

        return new ExportSnapshotResponse(
            snapshot.Version,
            snapshot.Content,
            "json"
        );
    }
}
