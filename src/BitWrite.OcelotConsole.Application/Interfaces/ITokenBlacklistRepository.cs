using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Repository for TokenBlacklist entity (§8).
/// </summary>
public interface ITokenBlacklistRepository
{
    Task<TokenBlacklist?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> IsTokenBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(TokenBlacklist blacklistEntry, CancellationToken cancellationToken = default);
    Task UpdateAsync(TokenBlacklist blacklistEntry, CancellationToken cancellationToken = default);
}
