using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Redis;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public class RedisRoleRepository : IRoleRepository
{
    private readonly IDatabase _db;

    public RedisRoleRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<RoleEntity?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(id);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RoleEntity>(json.ToString());
    }

    public async Task<RoleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(RoleId.From(name));
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<RoleEntity>(json.ToString());
    }

    public async Task<List<RoleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IndexRoles;
        var members = await _db.SetMembersAsync(key);
        var roles = new List<RoleEntity>();

        foreach (var member in members)
        {
            var role = await GetByIdAsync(RoleId.From(member.ToString()), cancellationToken);
            if (role != null)
                roles.Add(role);
        }

        return roles;
    }

    public async Task AddAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(role.Id);
        var json = JsonSerializer.Serialize(role);
        await _db.StringSetAsync(key, json);

        var indexKey = RedisKeyHelper.IndexRoles;
        await _db.SetAddAsync(indexKey, role.Id.Value);
    }

    public async Task UpdateAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        await AddAsync(role, cancellationToken);
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyHelper.IamRole(id);
        var role = await GetByIdAsync(id, cancellationToken);
        if (role == null) return;

        await _db.KeyDeleteAsync(key);
        var indexKey = RedisKeyHelper.IndexRoles;
        await _db.SetRemoveAsync(indexKey, id.Value);
    }
}
