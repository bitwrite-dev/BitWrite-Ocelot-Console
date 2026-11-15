using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class RefreshAccessTokenCommandHandler
{
    private readonly ISecurityService _securityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;

    public RefreshAccessTokenCommandHandler(
        ISecurityService securityService,
        IRefreshTokenRepository refreshTokenRepository,
        ISessionRepository sessionRepository,
        IUserRepository userRepository)
    {
        _securityService = securityService;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
    }

    public async Task<RefreshAccessTokenResponse> HandleAsync(RefreshAccessTokenCommand command, CancellationToken cancellationToken = default)
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

        var user = await _userRepository.GetAsync(userId, cancellationToken);
        if (user is null)
            throw new DomainException("User not found", "USER_NOT_FOUND");

        if (!user.Status.IsActive)
            throw new DomainException("User is not active", "USER_NOT_ACTIVE");

        var (newRefreshToken, newRefreshTokenEntity) = await _securityService.IssueRefreshTokenAsync(userId, cancellationToken);

        oldRefreshToken.MarkAsReplaced(newRefreshTokenEntity.Id.Value.ToString());
        await _refreshTokenRepository.UpdateAsync(oldRefreshToken, cancellationToken);

        var session = await GetOrCreateActiveSessionAsync(userId, newRefreshTokenEntity, cancellationToken);
        session.RecordAuthentication(newRefreshTokenEntity.Id.Value.ToString(), newRefreshTokenEntity.ExpiresAt);
        await _sessionRepository.UpdateAsync(session, cancellationToken);

        var accessToken = await _securityService.GenerateAccessTokenAsync(userId, cancellationToken);
        var accessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        return new RefreshAccessTokenResponse(
            accessToken,
            newRefreshToken,
            accessTokenExpiresAt,
            newRefreshTokenEntity.ExpiresAt);
    }

    private async Task<Session> GetOrCreateActiveSessionAsync(UserId userId, RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetByUserIdAsync(userId, cancellationToken);
        var session = sessions.FirstOrDefault(s => !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow);
        if (session is not null)
            return session;

        session = Session.Create(userId, refreshToken.ExpiresAt);
        await _sessionRepository.AddAsync(session, cancellationToken);
        return session;
    }
}
