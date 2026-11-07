using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using StackExchange.Redis;
using System.Text.Json;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public class RedisTokenBlacklistRepository : ITokenBlacklistRepository
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RedisTokenBlacklistRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<TokenBlacklist?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamTokenBlacklist(tokenHash);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<TokenBlacklist>(json.ToString(), JsonOptions);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamTokenBlacklist(tokenHash);
        return await _db.KeyExistsAsync(key);
    }

    public async Task AddAsync(TokenBlacklist blacklistEntry, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamTokenBlacklist(blacklistEntry.TokenHash);
        var expiry = blacklistEntry.ExpiresAt - DateTimeOffset.UtcNow;
        if (expiry <= TimeSpan.Zero)
            return;

        var json = JsonSerializer.Serialize(blacklistEntry, JsonOptions);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task UpdateAsync(TokenBlacklist blacklistEntry, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamTokenBlacklist(blacklistEntry.TokenHash);
        var expiry = blacklistEntry.ExpiresAt - DateTimeOffset.UtcNow;
        if (expiry <= TimeSpan.Zero)
        {
            await _db.KeyDeleteAsync(key);
            return;
        }

        var json = JsonSerializer.Serialize(blacklistEntry, JsonOptions);
        await _db.StringSetAsync(key, json, expiry);
    }
}