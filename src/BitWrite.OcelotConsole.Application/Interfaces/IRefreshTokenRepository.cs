using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Repository for RefreshToken entity (§8).
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetAsync(RefreshTokenId id, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveRefreshTokensAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task DeleteAsync(RefreshTokenId id, CancellationToken cancellationToken = default);
}
