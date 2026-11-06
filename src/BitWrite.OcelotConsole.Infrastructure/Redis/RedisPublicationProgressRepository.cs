using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Publication;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IPublicationProgressRepository"/>.
/// </summary>
public class RedisPublicationProgressRepository : IPublicationProgressRepository
{
    private readonly IDatabase _database;
    private const string KeyPrefix = "ocelot:progress:";

    public RedisPublicationProgressRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task SetProgressAsync(
        PublicationId publicationId,
        PublicationProgressData progress,
        CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{publicationId.Value}";
        var json = JsonSerializer.Serialize(progress);
        await _database.StringSetAsync(key, json, TimeSpan.FromHours(24));
    }

    public async Task<PublicationProgressData?> GetProgressAsync(
        PublicationId publicationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{publicationId.Value}";
        var json = await _database.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        try
        {
            var jsonString = json.ToString();
            return JsonSerializer.Deserialize<PublicationProgressData>(jsonString);
        }
        catch
        {
            // If cache is corrupted, return null to fall through to database
            return null;
        }
    }

    public async Task DeleteProgressAsync(
        PublicationId publicationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{KeyPrefix}{publicationId.Value}";
        await _database.KeyDeleteAsync(key);
    }
}
