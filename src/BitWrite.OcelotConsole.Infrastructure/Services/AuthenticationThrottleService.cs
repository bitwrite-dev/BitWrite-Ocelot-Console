using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Services;

/// <summary>
/// Tracks authentication attempts per identifier (IP address or username) using Redis,
/// implementing sliding-window rate limiting and account lockout after too many failures.
/// </summary>
public interface IAuthenticationThrottleService
{
    /// <summary>Records a failed authentication attempt for the given identifier.</summary>
    Task RecordFailureAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>Records a successful authentication, clearing any failure count.</summary>
    Task RecordSuccessAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the identifier is currently locked out.</summary>
    Task<bool> IsLockedOutAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>Returns the number of remaining attempts before lockout.</summary>
    Task<int> RemainingAttemptsAsync(string identifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis-backed implementation of <see cref="IAuthenticationThrottleService"/>.
/// Uses a sliding window: each failed attempt increments a counter within a time window;
/// when the count reaches <see cref="MaxFailedAttempts"/>, the identifier is locked out
/// for <see cref="LockoutMinutes"/>.
/// </summary>
public class AuthenticationThrottleService : IAuthenticationThrottleService
{
    private readonly IDatabase _redis;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutMinutes;
    private readonly int _windowSeconds;

    public AuthenticationThrottleService(
        IConnectionMultiplexer connectionMultiplexer,
        int maxFailedAttempts = 5,
        int lockoutMinutes = 15,
        int windowSeconds = 300)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _maxFailedAttempts = maxFailedAttempts;
        _lockoutMinutes = lockoutMinutes;
        _windowSeconds = windowSeconds;
    }

    public async Task RecordFailureAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.AuthAttemptsKey(identifier);
        var lockoutKey = RedisKeyHelper.AuthLockoutKey(identifier);

        // If already locked out, extend the lockout rather than counting further.
        if (await _redis.KeyExistsAsync(lockoutKey))
        {
            await _redis.KeyExpireAsync(lockoutKey, TimeSpan.FromMinutes(_lockoutMinutes));
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (now % _windowSeconds);

        var count = await _redis.HashIncrementAsync(key, "count", 1);
        await _redis.HashSetAsync(key, "windowStart", windowStart.ToString());
        await _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(_windowSeconds * 2));

        if (count >= _maxFailedAttempts)
        {
            await _redis.KeyExpireAsync(lockoutKey, TimeSpan.FromMinutes(_lockoutMinutes));
            await _redis.KeyDeleteAsync(key);
        }
    }

    public async Task RecordSuccessAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.AuthAttemptsKey(identifier);
        var lockoutKey = RedisKeyHelper.AuthLockoutKey(identifier);

        await _redis.KeyDeleteAsync(key);
        await _redis.KeyDeleteAsync(lockoutKey);
    }

    public async Task<bool> IsLockedOutAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var lockoutKey = RedisKeyHelper.AuthLockoutKey(identifier);
        return await _redis.KeyExistsAsync(lockoutKey);
    }

    public async Task<int> RemainingAttemptsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.AuthAttemptsKey(identifier);
        var lockoutKey = RedisKeyHelper.AuthLockoutKey(identifier);

        if (await _redis.KeyExistsAsync(lockoutKey))
            return 0;

        var count = await _redis.HashGetAsync(key, "count");
        var current = count.IsNullOrEmpty ? 0 : (int)count!;

        return Math.Max(0, _maxFailedAttempts - current);
    }
}
