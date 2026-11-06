using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Repositories;

/// <summary>
/// Redis-based implementation of <see cref="ISessionRepository"/>.
/// </summary>
public class RedisSessionRepository : ISessionRepository
{
    private readonly IDatabase _db;

    public RedisSessionRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamSession(id);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Session>(json!.ToString());
    }

    public async Task<List<Session>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUser(userId);
        var members = await _db.SetMembersAsync(key);
        var sessions = new List<Session>();

        foreach (var member in members)
        {
            var session = await GetAsync(SessionId.From(member.ToString()), cancellationToken);
            if (session != null)
                sessions.Add(session);
        }

        return sessions;
    }

    public async Task<List<Session>> GetActiveSessionsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var sessions = await GetByUserIdAsync(userId, cancellationToken);
        return sessions.Where(s => !s.IsRevoked && s.ExpiresAt > DateTimeOffset.UtcNow).ToList();
    }

    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamSession(session.Id);
        var json = JsonSerializer.Serialize(session);
        await _db.StringSetAsync(key, json);

        var userKey = RedisKeyHelper.IamUser(session.UserId);
        await _db.SetAddAsync(userKey, session.Id.Value.ToString());
    }

    public async Task UpdateAsync(Session session, CancellationToken cancellationToken = default)
    {
        await AddAsync(session, cancellationToken);
    }

    public async Task DeleteAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        var session = await GetAsync(id, cancellationToken);
        if (session == null) return;

        var key = RedisKeyHelper.IamSession(id);
        await _db.KeyDeleteAsync(key);

        var userKey = RedisKeyHelper.IamUser(session.UserId);
        await _db.SetRemoveAsync(userKey, id.Value.ToString());
    }
}
