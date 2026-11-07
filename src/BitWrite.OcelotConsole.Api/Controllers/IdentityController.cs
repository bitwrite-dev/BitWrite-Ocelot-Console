using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitWrite.OcelotConsole.Api.Controllers;

/// <summary>
/// Session and refresh token management endpoints (T-15 / #647).
/// </summary>
[Authorize(Policy = "Operator")]
public class IdentityController : BaseApiController
{
    private readonly CreateSessionCommandHandler _createSession;
    private readonly GetUserSessionsQueryHandler _getUserSessions;
    private readonly RevokeSessionCommandHandler _revokeSession;
    private readonly RefreshAccessTokenCommandHandler _refreshAccessToken;
    private readonly RotateRefreshTokenCommandHandler _rotateRefreshToken;
    private readonly RevokeRefreshTokenCommandHandler _revokeRefreshToken;

    public IdentityController(
        CreateSessionCommandHandler createSession,
        GetUserSessionsQueryHandler getUserSessions,
        RevokeSessionCommandHandler revokeSession,
        RefreshAccessTokenCommandHandler refreshAccessToken,
        RotateRefreshTokenCommandHandler rotateRefreshToken,
        RevokeRefreshTokenCommandHandler revokeRefreshToken)
    {
        _createSession = createSession;
        _getUserSessions = getUserSessions;
        _revokeSession = revokeSession;
        _refreshAccessToken = refreshAccessToken;
        _rotateRefreshToken = rotateRefreshToken;
        _revokeRefreshToken = revokeRefreshToken;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateSessionCommand(
                UserId.From(request.UserId),
                request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(30),
                InitiatedBy,
                CorrelationId);
            return HandleResult(await _createSession.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("users/{userId}/sessions")]
    public async Task<ActionResult<GetUserSessionsResponse>> GetUserSessions(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return HandleResult(await _getUserSessions.HandleAsync(new GetUserSessionsQuery(UserId.From(userId)), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("sessions/{sessionId}/revoke")]
    public async Task<ActionResult<RevokeSessionResponse>> RevokeSession(string sessionId, [FromBody] RevokeSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RevokeSessionCommand(
                SessionId.From(sessionId),
                request.Reason ?? "Revoked by operator",
                InitiatedBy,
                CorrelationId);
            return HandleResult(await _revokeSession.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("tokens/refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshAccessTokenResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return HandleResult(await _refreshAccessToken.HandleAsync(new RefreshAccessTokenCommand(request.RefreshToken, CorrelationId), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("tokens/rotate")]
    [AllowAnonymous]
    public async Task<ActionResult<RotateRefreshTokenResponse>> Rotate([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return HandleResult(await _rotateRefreshToken.HandleAsync(new RotateRefreshTokenCommand(request.RefreshToken, CorrelationId), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("tokens/revoke")]
    [Authorize(Policy = "Operator")]
    public async Task<ActionResult<RevokeRefreshTokenResponse>> RevokeRefreshToken([FromBody] RevokeRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RevokeRefreshTokenCommand(request.RefreshToken, request.Reason ?? "Revoked by operator", InitiatedBy, CorrelationId);
            return HandleResult(await _revokeRefreshToken.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private string InitiatedBy =>
        User.Identity?.Name
        ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
        ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
        ?? "unattributed";
}

public record CreateSessionRequest(string UserId, DateTimeOffset? ExpiresAt = null);
public record RevokeSessionRequest(string? Reason = null);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeRefreshTokenRequest(string RefreshToken, string? Reason = null);
