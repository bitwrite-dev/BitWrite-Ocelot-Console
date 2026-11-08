using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Rotates a refresh token (§8).
/// </summary>
public record RotateRefreshTokenCommand(
    string RefreshToken,
    string CorrelationId = "");

/// <summary>
/// Response for refresh token rotation.
/// </summary>
public record RotateRefreshTokenResponse(
    string NewRefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
