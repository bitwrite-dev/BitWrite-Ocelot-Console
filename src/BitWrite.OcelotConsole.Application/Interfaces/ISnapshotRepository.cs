using BitWrite.OcelotConsole.Domain.Aggregates.Snapshot;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface ISnapshotRepository
{
    Task<Snapshot?> GetAsync(SnapshotVersion version, CancellationToken cancellationToken = default);
    Task<Snapshot?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<List<Snapshot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Snapshot snapshot, CancellationToken cancellationToken = default);
    Task UpdateAsync(Snapshot snapshot, CancellationToken cancellationToken = default);
}
