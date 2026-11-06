using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Repository for publication progress tracking in Redis.
/// Provides abstraction over Redis so the Application layer does not depend
/// directly on StackExchange.Redis.
/// </summary>
public interface IPublicationProgressRepository
{
    /// <summary>
    /// Stores publication progress data for real-time tracking.
    /// </summary>
    Task SetProgressAsync(
        PublicationId publicationId,
        PublicationProgressData progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads publication progress data from cache.
    /// </summary>
    Task<PublicationProgressData?> GetProgressAsync(
        PublicationId publicationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes publication progress data from cache.
    /// </summary>
    Task DeleteProgressAsync(
        PublicationId publicationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Data structure for publication progress stored in Redis.
/// </summary>
public record PublicationProgressData(
    string PublicationId,
    int SnapshotVersion,
    string Status,
    double ProgressPercentage,
    List<GatewayProgressData> GatewayProgress,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? FailureReason
);

/// <summary>
/// Gateway progress data within publication progress.
/// </summary>
public record GatewayProgressData(
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
