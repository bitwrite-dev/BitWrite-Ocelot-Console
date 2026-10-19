using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface ISnapshotVersionAllocator
{
    SnapshotVersion CurrentVersion { get; }
    SnapshotVersion AllocateNext();
    SnapshotVersion AllocateSpecific(SnapshotVersion version);
    void ResetTo(SnapshotVersion version);
}
