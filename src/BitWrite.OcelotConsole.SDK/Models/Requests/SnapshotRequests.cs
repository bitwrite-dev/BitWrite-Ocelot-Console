using System.ComponentModel.DataAnnotations;

namespace BitWrite.OcelotConsole.SDK.Models.Requests;

public record CreateSnapshotRequest(
    [Required] string InitiatedBy,
    string? CorrelationId
);

public record ValidateSnapshotRequest(
    [Required] string Content,
    [Required] string InitiatedBy,
    string? CorrelationId
);

public record CompareSnapshotsRequest(
    [Required] int VersionA,
    [Required] int VersionB
);

public record CloneSnapshotRequest(
    [Required] int Version,
    [Required] string NewName,
    [Required] string InitiatedBy
);

public record ExportSnapshotRequest(
    [Required] int Version
);

public record PublishSnapshotRequest(
    [Required] int Version,
    [Required] string InitiatedBy,
    [Required] string Environment,
    List<string>? TargetGatewayIds
);

public record RollbackSnapshotRequest(
    [Required] string InitiatedBy,
    [Required] int TargetVersion,
    [Required] string Environment,
    [Required] string Reason
);
