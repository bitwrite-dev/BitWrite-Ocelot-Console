using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Revokes a user session (§8).
/// </summary>
public record RevokeSessionCommand(
    SessionId SessionId,
    string Reason,
    string RevokedBy,
    string CorrelationId = "");

/// <summary>
/// Response for session revocation.
/// </summary>
public record RevokeSessionResponse(
    SessionId SessionId,
    bool Success,
    string Message);