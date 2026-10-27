using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;

namespace BitWrite.OcelotConsole.Application.UseCases.Publication;

/// <summary>
/// Create a publication record without immediately executing it.
/// </summary>
/// <remarks>
/// This separates the creation of a publication record (with its target gateways)
/// from the actual execution. The publication can then be reviewed and explicitly
/// confirmed before execution begins.
/// </remarks>
public record CreatePublicationCommand(
    string SnapshotVersion,
    string InitiatedBy,
    EnvironmentName Environment,
    string CorrelationId = "",
    IReadOnlyList<string>? TargetGatewayIds = null
);

/// <summary>
/// Execute a previously created publication.
/// </summary>
public record ExecutePublicationCommand(
    string PublicationId,
    string InitiatedBy,
    EnvironmentName Environment,
    string CorrelationId = ""
);

/// <summary>
/// Cancel a pending publication.
/// </summary>
public record CancelPublicationCommand(
    string PublicationId,
    string InitiatedBy,
    string CorrelationId = ""
);

/// <summary>
/// Get the current progress of a publication.
/// </summary>
public record GetPublicationProgressQuery(
    string PublicationId
);

/// <summary>
/// Get the differences between the current published snapshot and a target snapshot.
/// </summary>
public record GetPublicationDiffQuery(
    string TargetSnapshotVersion,
    string? CurrentSnapshotVersion = null,
    EnvironmentName? Environment = null
);

/// <summary>
/// Response for publication diff.
/// </summary>
public record PublicationDiffResponse(
    bool HasDifferences,
    List<string> Differences,
    int TargetSnapshotVersion,
    int? CurrentSnapshotVersion
);

/// <summary>
/// Response for publication progress.
/// </summary>
public record PublicationProgressResponse(
    string PublicationId,
    int SnapshotVersion,
    string Status,
    double ProgressPercentage,
    List<GatewayProgressResponse> GatewayProgress,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason
);

/// <summary>
/// Response for individual gateway progress.
/// </summary>
public record GatewayProgressResponse(
    string GatewayId,
    string GatewayName,
    string Status,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? AppliedAt,
    DateTimeOffset? HealthyAt,
    DateTimeOffset? FailedAt,
    bool? IsValid,
    string? FailureReason
);

/// <summary>
/// Response for creating a publication.
/// </summary>
public record CreatePublicationResponse(
    string PublicationId,
    int SnapshotVersion,
    string Status,
    List<GatewayProgressResponse> GatewayProgress
);
