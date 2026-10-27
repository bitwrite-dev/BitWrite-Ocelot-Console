namespace BitWrite.OcelotConsole.Api.DTOs;

public record PublicationResponse(
    string Id,
    int SnapshotVersion,
    string Status,
    string InitiatedBy,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason,
    List<GatewayDeploymentStateResponse> GatewayStates
);

public record GatewayDeploymentStateResponse(
    string GatewayId,
    string Status,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? AppliedAt,
    DateTimeOffset? HealthyAt,
    bool? IsValid,
    DateTimeOffset? FailedAt,
    string? FailureReason
);

public record PublicationListResponse(
    List<PublicationResponse> Publications,
    int TotalCount,
    int Page,
    int PageSize
);

public record CurrentPublicationResponse(
    PublicationResponse? Current,
    List<PublicationResponse> History
);

public record CreatePublicationRequest(
    string SnapshotVersion,
    string InitiatedBy,
    string Environment,
    string? CorrelationId = null,
    List<string>? TargetGatewayIds = null
);

public record CreatePublicationResponse(
    string PublicationId,
    int SnapshotVersion,
    string Status,
    List<GatewayProgressResponse> GatewayProgress
);

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

public record ExecutePublicationRequest(
    string InitiatedBy,
    string? Environment = null,
    string? CorrelationId = null
);

public record ExecutePublicationResponse(
    string PublicationId
);

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

public record CancelPublicationRequest(
    string InitiatedBy,
    string? CorrelationId = null
);

public record PublicationDiffResponse(
    bool HasDifferences,
    List<string> Differences,
    int TargetSnapshotVersion,
    int? CurrentSnapshotVersion
);
