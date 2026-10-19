using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Application.UseCases.Snapshot;

public record ListSnapshotsQuery(
    int Page = 1,
    int PageSize = 20,
    SnapshotStatus? Status = null
);

public record GetSnapshotQuery(
    SnapshotVersion Version
);

public record ValidateSnapshotCommand(
    string Content,
    string InitiatedBy,
    string CorrelationId = ""
);
