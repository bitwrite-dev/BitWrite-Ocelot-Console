using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IRefreshTokenRepository"/>.
/// </summary>
public class RedisRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        return JsonSerializer.Deserialize<RefreshToken>(json!.ToString(), JsonOptions);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return null;

        var hashKey = RedisKeyHelper.IamRefreshTokenByHash(tokenHash);
        var tokenIdValue = await _db.StringGetAsync(hashKey);
        if (tokenIdValue.IsNullOrEmpty)
            return null;

        if (!Guid.TryParse(tokenIdValue.ToString(), out var tokenGuid))
            return null;

        return await GetAsync(RefreshTokenId.From(tokenGuid), cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUserRefreshTokens(userId);
        var members = await _db.SetMembersAsync(key);
        var tokens = new List<RefreshToken>(members.Length);

        foreach (var member in members)
        {
            if (!Guid.TryParse(member.ToString(), out var tokenGuid))
                continue;

            var token = await GetAsync(RefreshTokenId.From(tokenGuid), cancellationToken);
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
        var json = JsonSerializer.Serialize(refreshToken, JsonOptions);
        var ttl = refreshToken.ExpiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
            ttl = TimeSpan.FromMinutes(1);

        await _db.StringSetAsync(key, json, ttl);
        await _db.SetAddAsync(RedisKeyHelper.IamUserRefreshTokens(refreshToken.UserId), refreshToken.Id.Value.ToString());
        await _db.StringSetAsync(RedisKeyHelper.IamRefreshTokenByHash(refreshToken.TokenHash), refreshToken.Id.Value.ToString(), ttl);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await AddAsync(refreshToken, cancellationToken);

        // If the token is no longer usable, remove hash lookup to prevent replay/use.
        if (refreshToken.IsRevoked || refreshToken.IsExpired)
        {
            await _db.KeyDeleteAsync(RedisKeyHelper.IamRefreshTokenByHash(refreshToken.TokenHash));
        }
    }

    public async Task DeleteAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
    {
        var token = await GetAsync(id, cancellationToken);
        if (token == null) return;

        await _db.KeyDeleteAsync(RedisKeyHelper.IamRefreshToken(id));
        await _db.KeyDeleteAsync(RedisKeyHelper.IamRefreshTokenByHash(token.TokenHash));
        await _db.SetRemoveAsync(RedisKeyHelper.IamUserRefreshTokens(token.UserId), id.Value.ToString());
    }
}
