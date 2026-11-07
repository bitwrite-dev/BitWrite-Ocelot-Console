using BitWrite.OcelotConsole.Application.Interfaces;
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
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            throw new DomainException("Refresh token is required", "INVALID_REFRESH_TOKEN");

        var tokenHash = _securityService.ComputeSha256Hash(command.RefreshToken);
        var userId = await _securityService.ValidateRefreshTokenAsync(tokenHash, cancellationToken);
        if (userId is null)
            throw new DomainException("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");

        var oldRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (oldRefreshToken is null || oldRefreshToken.IsRevoked || oldRefreshToken.IsExpired)
            throw new DomainException("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN");

        var (newRefreshToken, newRefreshTokenEntity) = await _securityService.IssueRefreshTokenAsync(userId, cancellationToken);

        oldRefreshToken.MarkAsReplaced(newRefreshTokenEntity.Id.Value.ToString());
        await _refreshTokenRepository.UpdateAsync(oldRefreshToken, cancellationToken);

        var sessions = await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
        var session = sessions.FirstOrDefault(s => !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow);
        if (session is null)
        {
            session = Session.Create(userId, newRefreshTokenEntity.ExpiresAt);
            await _sessionRepository.AddAsync(session, cancellationToken);
        }

        session.RecordAuthentication(newRefreshTokenEntity.Id.Value.ToString(), newRefreshTokenEntity.ExpiresAt);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        return new RotateRefreshTokenResponse(newRefreshToken, newRefreshTokenEntity.ExpiresAt);
    }
}
