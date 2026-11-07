using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

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
        // Validate refresh token
        var tokenHash = _securityService.ComputeSha256Hash(command.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        
        if (refreshToken == null)
        {
            throw new DomainException("Refresh token not found", "REFRESH_TOKEN_NOT_FOUND");
        }

        if (refreshToken.IsRevoked)
        {
            throw new DomainException("Refresh token is already revoked", "TOKEN_ALREADY_REVOKED");
        }

        // Revoke the token
        refreshToken.Revoke(command.RevokedBy, command.Reason);
        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        return new RevokeRefreshTokenResponse(true, "Refresh token revoked successfully");
    }
}