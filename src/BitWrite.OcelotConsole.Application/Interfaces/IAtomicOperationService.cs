using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Redis-based atomic operations service for concurrency-safe operations (§10.3).
/// </summary>
/// <remarks>
/// All operations use Redis Lua scripts to ensure atomicity — no read-modify-write
/// race conditions. All operations are executed atomically on the Redis server.
/// </summary>
public interface IAtomicOperationService
{
    /// <summary>
    /// Atomically increments a counter and returns the new value.
    /// </summary>
    Task<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically decrements a counter and returns the new value.
    /// </summary>
    Task<long> DecrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value. Used for read checks where atomic mutation is not required.
    /// </summary>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically gets and sets a value. Returns the previous value.
    /// </summary>
    Task<string?> GetAndSetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically sets a key only if it does not exist. Returns true if set.
    /// </summary>
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically compares and sets a value if it matches the expected value.
    /// Returns true if the value was updated.
    /// </summary>
    Task<bool> CompareAndSetAsync(string key, string expectedValue, string newValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments a counter with TTL expiration.
    /// </summary>
    Task<long> IncrementWithExpiryAsync(string key, long delta, TimeSpan expiry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a Lua script atomically on Redis.
    /// </summary>
    Task<T?> ExecuteScriptAsync<T>(string script, string[] keys, string[] args, CancellationToken cancellationToken = default);
}
