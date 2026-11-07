using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class RotateRefreshTokenCommandHandler
{
    private readonly ISecurityService _securityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISessionRepository _sessionRepository;

    public RotateRefreshTokenCommandHandler(
        ISecurityService securityService,
        IRefreshTokenRepository refreshTokenRepository,
        ISessionRepository sessionRepository)
    {
        _securityService = securityService;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<RotateRefreshTokenResponse> HandleAsync(RotateRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        // Validate refresh token
        var tokenHash = _securityService.ComputeSha256Hash(command.RefreshToken);
        var userId = await _securityService.ValidateRefreshTokenAsync(tokenHash, cancellationToken);
        
        if (userId == null)
        {
            throw new DomainException("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");
        }

        // Get the refresh token to rotate it
        var oldRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        
        if (oldRefreshToken == null || oldRefreshToken.IsRevoked || oldRefreshToken.IsExpired)
        {
            throw new DomainException("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");
        }

        // Create new session or get existing
        var sessions = await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
        var session = sessions?.FirstOrDefault(s => !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow);
        
        if (session == null)
        {
            var newSession = Session.Create(userId, DateTimeOffset.UtcNow.AddDays(30));
            await _sessionRepository.AddAsync(session, cancellationToken);
        }
        else
        {
            session.RecordAuthentication(oldRefreshToken.Id, DateTimeOffset.UtcNow.AddDays(7));
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        // Generate new access token
        var roles = new List<string>(); // TODO: Get user roles
        var accessToken = await _securityService.GenerateAccessTokenAsync(userId, roles, cancellationToken);

        // Generate new refresh token (rotation)
        var newRefreshToken = await _securityService.GenerateRefreshTokenAsync(userId, cancellationToken);

        // Revoke old refresh token
        oldRefreshToken.MarkAsReplaced(Guid.NewGuid().ToString());
        await _refreshTokenRepository.UpdateAsync(oldRefreshToken, cancellationToken);

        // Store new refresh token
        var newTokenHash = _securityService.ComputeSha256Hash(newRefreshToken);
        var newRefreshTokenEntity = RefreshToken.Create(userId, newTokenHash, DateTimeOffset.UtcNow.AddDays(7));
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        return new RotateRefreshTokenResponse(
            newRefreshToken,
            DateTimeOffset.UtcNow.AddDays(7));
    }
}