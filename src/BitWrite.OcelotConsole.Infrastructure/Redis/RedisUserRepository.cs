using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IUserRepository"/>.
/// </summary>
public class RedisUserRepository : IUserRepository
{
    private readonly IDatabase _db;

    public RedisUserRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<User?> GetAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUserData(id);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<User>(json.ToString());
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUserByUsername(username);
        var userId = await _db.StringGetAsync(key);
        if (userId.IsNullOrEmpty)
            return null;

        return await GetAsync(UserId.From(userId.ToString()), cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUsers;
        var members = await _db.SetMembersAsync(key);
        var users = new List<User>();

        foreach (var member in members)
        {
            var user = await GetAsync(UserId.From(member.ToString()), cancellationToken);
            if (user != null)
                users.Add(user);
        }

        return users;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamUserData(user.Id);
        var json = JsonSerializer.Serialize(user);
        await _db.StringSetAsync(key, json);

        var userKey = RedisKeyHelper.IamUserByUsername(user.Username);
        await _db.StringSetAsync(userKey, user.Id.Value.ToString());

var usersKey = RedisKeyHelper.IamUsers;
            await _db.SetAddAsync(usersKey, user.Id.Value.ToString());
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await AddAsync(user, cancellationToken);
    }
}
