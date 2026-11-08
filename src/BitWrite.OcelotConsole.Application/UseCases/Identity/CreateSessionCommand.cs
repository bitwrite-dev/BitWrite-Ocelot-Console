using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Creates a new user session.
/// </summary>
public record CreateSessionCommand(
    UserId UserId,
    DateTimeOffset ExpiresAt,
    string InitiatedBy,
    string CorrelationId = "");

/// <summary>
/// Response for session creation.
/// </summary>
public record CreateSessionResponse(
    SessionId SessionId,
    UserId UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
