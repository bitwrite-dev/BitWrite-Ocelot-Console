using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements ISnapshotVersionAllocator using Domain.SnapshotVersionAllocator.
/// </summary>
public class SnapshotVersionAllocatorAdapter : ISnapshotVersionAllocator
{
    private readonly SnapshotVersionAllocator _domainAllocator;

    public SnapshotVersionAllocatorAdapter(SnapshotVersionAllocator domainAllocator)
    {
        _domainAllocator = domainAllocator;
    }

    public SnapshotVersion CurrentVersion => _domainAllocator.CurrentVersion;

    public SnapshotVersion AllocateNext()
    {
        return _domainAllocator.AllocateNext();
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
