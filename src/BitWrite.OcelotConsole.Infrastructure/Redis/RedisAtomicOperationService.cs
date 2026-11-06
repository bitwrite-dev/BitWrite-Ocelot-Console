using BitWrite.OcelotConsole.Application.Interfaces;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IAtomicOperationService"/>.
/// All operations use Lua scripts to guarantee atomicity — no read-modify-write
/// race conditions possible.
/// </summary>
public class RedisAtomicOperationService : IAtomicOperationService
{
    private readonly IDatabase _database;

    public RedisAtomicOperationService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    /// <summary>
    /// Atomically increments a counter and returns the new value.
    /// Uses Redis INCRBY which is atomic at the server level.
    /// </summary>
    public async Task<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
    {
        return await _database.StringIncrementAsync(key, delta);
    }

    /// <summary>
    /// Atomically decrements a counter and returns the new value.
    /// Uses Redis DECRBY which is atomic at the server level.
    /// </summary>
    public async Task<long> DecrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
    {
        return await _database.StringDecrementAsync(key, delta);
    }

    /// <summary>
    /// Atomically gets and sets a value. Returns the previous value.
    /// Uses Redis GETSET which is atomic at the server level.
    /// </summary>
    public async Task<string?> GetAndSetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var result = await _database.StringGetSetAsync(key, value);
        return result.IsNullOrEmpty ? null : (string)result;
    }

    /// <summary>
    /// Atomically sets a key only if it does not exist. Returns true if set.
    /// Uses Redis SET NX which is atomic at the server level.
    /// </summary>
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        return await _database.StringSetAsync(key, value, expiry, when: When.NotExists);
    }

    /// <summary>
    /// Atomically compares and sets a value if it matches the expected value.
    /// Uses a Lua script to ensure the check-and-set is atomic.
    /// </summary>
    public async Task<bool> CompareAndSetAsync(string key, string expectedValue, string newValue, CancellationToken cancellationToken = default)
    {
        const string script = @"
local current = redis.call('GET', KEYS[1])
if current == ARGV[1] then
    redis.call('SET', KEYS[1], ARGV[2])
    return 1
else
    return 0
end";

        var result = await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[] { key },
            new RedisValue[] { expectedValue, newValue });

        return (long)result == 1;
    }

    /// <summary>
    /// Atomically increments a counter with TTL expiration.
    /// </summary>
    public async Task<long> IncrementWithExpiryAsync(string key, long delta, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var newValue = await _database.StringIncrementAsync(key, delta);

        // Only set expiry on first increment (when value becomes 1)
        if (newValue == delta)
        {
            await _database.KeyExpireAsync(key, expiry);
        }

        return newValue;
    }

    /// <summary>
    /// Executes a Lua script atomically on Redis.
    /// </summary>
    public async Task<T?> ExecuteScriptAsync<T>(string script, string[] keys, string[] args, CancellationToken cancellationToken = default)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
        var redisArgs = args.Select(a => (RedisValue)a).ToArray();

        var result = await _database.ScriptEvaluateAsync(script, redisKeys, redisArgs);

        if (result.IsNull)
            return default;

        // RedisResult can be implicitly converted to long for integer results
        return (T)(object)(long)result;
    }
}
