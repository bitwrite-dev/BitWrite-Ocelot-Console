namespace BitWrite.OcelotConsole.Application.UseCases.Snapshot;

public record CreateSnapshotCommand(
    string InitiatedBy,
    string CorrelationId = ""
);
