using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based local credential persistence in IAM namespace.
/// </summary>
public class RedisLocalCredentialRepository : ILocalCredentialRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _db;

    public RedisLocalCredentialRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<LocalCredential?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var json = await _db.StringGetAsync(RedisKeyHelper.IamLocalCredential(userId));
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<LocalCredential>(json.ToString(), JsonOptions);
    }

    public async Task AddAsync(LocalCredential credential, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(credential, JsonOptions);
        await _db.StringSetAsync(RedisKeyHelper.IamLocalCredential(credential.UserId), json);
    }

    public async Task UpdateAsync(LocalCredential credential, CancellationToken cancellationToken = default)
    {
        await AddAsync(credential, cancellationToken);
    }
}
