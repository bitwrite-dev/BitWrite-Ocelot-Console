using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements ISnapshotVersionAllocator by deriving the next
/// version from the snapshot repository (persisted in Redis).
///
/// The domain allocator holds state in memory and is registered scoped, so
/// every request would restart at version 1 and allocate 2. By reading the
/// highest stored snapshot version from Redis and incrementing it, the
/// counter survives across requests and service restarts.
/// </summary>
public class SnapshotVersionAllocatorAdapter : ISnapshotVersionAllocator
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly SnapshotVersionAllocator _domainAllocator;

    public SnapshotVersionAllocatorAdapter(
        ISnapshotRepository snapshotRepository,
        SnapshotVersionAllocator domainAllocator)
    {
        _snapshotRepository = snapshotRepository;
        _domainAllocator = domainAllocator;
    }

    public SnapshotVersion CurrentVersion
    {
        get
        {
            var latest = _snapshotRepository.GetLatestAsync().GetAwaiter().GetResult();
            return latest?.Version ?? SnapshotVersion.First();
        }
    }

    public SnapshotVersion AllocateNext()
    {
        return AllocateNextAsync().GetAwaiter().GetResult();
    }

    public async Task<SnapshotVersion> AllocateNextAsync(CancellationToken cancellationToken = default)
    {
        var latest = await _snapshotRepository.GetLatestAsync(cancellationToken);
        return latest?.Version.Next() ?? SnapshotVersion.First();
    }

    public SnapshotVersion AllocateSpecific(SnapshotVersion version)
    {
        return _domainAllocator.AllocateSpecific(version);
    }

    public void ResetTo(SnapshotVersion version)
    {
        _domainAllocator.ResetTo(version);
    }
}
