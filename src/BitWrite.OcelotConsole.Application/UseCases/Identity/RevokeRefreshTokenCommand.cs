using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Revokes a refresh token (§8).
/// </summary>
public record RevokeRefreshTokenCommand(
    string RefreshToken,
    string RevokedBy,
    string Reason,
    string CorrelationId = "");

/// <summary>
/// Response for refresh token revocation.
/// </summary>
public record RevokeRefreshTokenResponse(
    bool Success,
    string Message);
