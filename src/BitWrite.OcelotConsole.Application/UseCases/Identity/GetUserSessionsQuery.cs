using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Gets all active sessions for a user (§8).
/// </summary>
public record GetUserSessionsQuery(
    UserId UserId);

/// <summary>
/// Response for user sessions query.
/// </summary>
public record GetUserSessionsResponse(
    IReadOnlyList<SessionInfo> Sessions);

/// <summary>
/// Session information for user sessions query.
/// </summary>
public record SessionInfo(
    SessionId SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastAccessedAt,
    bool IsRevoked);
