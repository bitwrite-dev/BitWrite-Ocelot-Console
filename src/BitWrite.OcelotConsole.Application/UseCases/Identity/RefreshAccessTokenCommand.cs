using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Refreshes an access token using a refresh token (§8).
/// </summary>
public record RefreshAccessTokenCommand(
    string RefreshToken,
    string CorrelationId = "");

/// <summary>
/// Response for access token refresh.
/// </summary>
public record RefreshAccessTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);