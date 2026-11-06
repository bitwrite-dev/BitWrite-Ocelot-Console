using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
public class RedisRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDatabase _db;

    public RedisRefreshTokenRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<RefreshToken?> GetAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRefreshToken(id);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RefreshToken>(json!.ToString());
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamTokenBlacklist(tokenHash);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RefreshToken>(json!.ToString());
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUser(userId);
        var members = await _db.SetMembersAsync(key);
        var tokens = new List<RefreshToken>();

        foreach (var member in members)
        {
            var token = await GetAsync(RefreshTokenId.From(member.ToString()), cancellationToken);
            if (token != null)
                tokens.Add(token);
        }

        return tokens;
    }

    public async Task<List<RefreshToken>> GetActiveRefreshTokensAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var tokens = await GetByUserIdAsync(userId, cancellationToken);
        return tokens.Where(t => !t.IsRevoked && !t.IsExpired).ToList();
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRefreshToken(refreshToken.Id);
        var json = JsonSerializer.Serialize(refreshToken);
        await _db.StringSetAsync(key, json);

        var userKey = RedisKeyHelper.IamUser(refreshToken.UserId);
        await _db.SetAddAsync(userKey, refreshToken.Id.Value.ToString());
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await AddAsync(refreshToken, cancellationToken);
    }

    public async Task DeleteAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
    {
        var token = await GetAsync(id, cancellationToken);
        if (token == null) return;

        var key = RedisKeyHelper.IamRefreshToken(id);
        await _db.KeyDeleteAsync(key);

        var userKey = RedisKeyHelper.IamUser(token.UserId);
        await _db.SetRemoveAsync(userKey, id.Value.ToString());
    }
}
