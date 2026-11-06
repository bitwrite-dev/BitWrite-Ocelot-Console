using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IRuntimeConfigurationRepository"/>.
/// </summary>
public class RedisRuntimeConfigurationRepository : IRuntimeConfigurationRepository
{
    private readonly IDatabase _database;

    public RedisRuntimeConfigurationRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task SetCurrentVersionAsync(
        string environment,
        string version,
        CancellationToken cancellationToken = default)
    {
        var env = EnvironmentName.From(environment);
        var key = RedisKeyHelper.RuntimeCurrent(env);
        await _database.StringSetAsync(key, version, TimeSpan.FromDays(7));
    }

    public async Task<string?> GetCurrentVersionAsync(
        string environment,
        CancellationToken cancellationToken = default)
    {
        var env = EnvironmentName.From(environment);
        var key = RedisKeyHelper.RuntimeCurrent(env);
        var version = await _database.StringGetAsync(key);
        return version.IsNullOrEmpty ? null : version.ToString();
    }
}
