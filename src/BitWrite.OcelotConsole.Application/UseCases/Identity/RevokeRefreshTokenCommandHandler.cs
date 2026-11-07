using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class RevokeRefreshTokenCommandHandler
{
    private readonly ISecurityService _securityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeRefreshTokenCommandHandler(
        ISecurityService securityService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _securityService = securityService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<RevokeRefreshTokenResponse> HandleAsync(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            throw new DomainException("Refresh token is required", "INVALID_REFRESH_TOKEN");

        var tokenHash = _securityService.ComputeSha256Hash(command.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
            throw new DomainException("Refresh token not found", "REFRESH_TOKEN_NOT_FOUND");

        if (refreshToken.IsRevoked)
            return new RevokeRefreshTokenResponse(true, "Refresh token already revoked");

        await _securityService.RevokeRefreshTokenAsync(
            tokenHash,
            command.RevokedBy,
            command.Reason,
            cancellationToken);

        if (!refreshToken.IsExpired)
        {
            await _securityService.BlacklistTokenAsync(
                tokenHash,
                "refresh",
                refreshToken.ExpiresAt,
                command.RevokedBy,
                command.Reason,
                cancellationToken);
        }

        return new RevokeRefreshTokenResponse(true, "Refresh token revoked successfully");
    }
}