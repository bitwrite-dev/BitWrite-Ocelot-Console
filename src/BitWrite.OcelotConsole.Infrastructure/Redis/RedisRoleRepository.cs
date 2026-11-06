using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public class RedisRoleRepository : IRoleRepository
{
    private readonly IDatabase _db;

    public RedisRoleRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(id);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Role>(json.ToString());
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(RoleId.From(name));
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<Role>(json.ToString());
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IndexRoles;
        var members = await _db.SetMembersAsync(key);
        var roles = new List<Role>();

        foreach (var member in members)
        {
            var role = await GetByIdAsync(RoleId.From(member.ToString()), cancellationToken);
            if (role != null)
                roles.Add(role);
        }

        return roles;
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(role.Id);
        var json = JsonSerializer.Serialize(role);
        await _db.StringSetAsync(key, json);

        var indexKey = RedisKeyHelper.IndexRoles;
        await _db.SetAddAsync(indexKey, role.Id.Value);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await AddAsync(role, cancellationToken);
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(id);
        var role = await GetAsync(id, cancellationToken);
        if (role == null) return;

        await _db.KeyDeleteAsync(key);
        var indexKey = RedisKeyHelper.IndexRoles;
        await _db.SetRemoveAsync(indexKey, id.Value);
    }
}
