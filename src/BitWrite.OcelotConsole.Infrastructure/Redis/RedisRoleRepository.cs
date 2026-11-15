using System.Text.Json;
using System.Text.Json.Serialization;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public class RedisRoleRepository : IRoleRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IDatabase _db;

    public RedisRoleRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<RoleEntity?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var json = await _db.StringGetAsync(RedisKeyHelper.IamRole(id));
        if (json.IsNullOrEmpty)
            return null;

        return Deserialize(json.ToString());
    }

    public async Task<RoleId?> GetIdByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = RedisKeyHelper.NormalizeRoleName(name);
        var id = await _db.StringGetAsync(RedisKeyHelper.IamRoleByName(normalized));
        if (!id.IsNullOrEmpty)
            return RoleId.From(id.ToString());

        // Legacy compatibility: older data may not have name index entries.
        var candidates = await _db.SetMembersAsync(RedisKeyHelper.IndexRoles);
        foreach (var candidate in candidates)
        {
            var role = await GetByIdAsync(RoleId.From(candidate.ToString()), cancellationToken);
            if (role is null)
                continue;
            if (string.Equals(RedisKeyHelper.NormalizeRoleName(role.Name), normalized, StringComparison.Ordinal))
            {
                await _db.StringSetAsync(RedisKeyHelper.IamRoleByName(normalized), role.Id.Value);
                return role.Id;
            }
        }

        return null;
    }

    public async Task<RoleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var id = await GetIdByNameAsync(name, cancellationToken);
        return id is null ? null : await GetByIdAsync(id, cancellationToken);
    }

    public async Task<List<RoleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(RedisKeyHelper.IndexRoles);
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
        var normalized = RedisKeyHelper.NormalizeRoleName(role.Name);
        var nameKey = RedisKeyHelper.IamRoleByName(normalized);
        var roleKey = RedisKeyHelper.IamRole(role.Id);
        var json = Serialize(role);

        const string script = @"
local nameKey = KEYS[1]
local roleKey = KEYS[2]
local indexKey = KEYS[3]
local roleId = ARGV[1]
local roleJson = ARGV[2]
local existing = redis.call('GET', nameKey)
if existing and existing ~= roleId then
  return 0
end
redis.call('SET', roleKey, roleJson)
redis.call('SADD', indexKey, roleId)
redis.call('SET', nameKey, roleId)
return 1";

        var result = await _db.ScriptEvaluateAsync(script,
            new RedisKey[] { nameKey, roleKey, RedisKeyHelper.IndexRoles },
            new RedisValue[] { role.Id.Value, json });

        if ((long)result != 1)
            throw new DomainException($"Role name '{normalized}' already exists", "ROLE_NAME_ALREADY_EXISTS");
    }

    public async Task UpdateAsync(RoleEntity role, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(role.Id, cancellationToken);
        var normalized = RedisKeyHelper.NormalizeRoleName(role.Name);
        var nameKey = RedisKeyHelper.IamRoleByName(normalized);
        var roleKey = RedisKeyHelper.IamRole(role.Id);
        var json = Serialize(role);

        if (existing is not null)
        {
            var oldNormalized = RedisKeyHelper.NormalizeRoleName(existing.Name);
            if (!string.Equals(oldNormalized, normalized, StringComparison.Ordinal))
            {
                const string renameScript = @"
local oldNameKey = KEYS[1]
local newNameKey = KEYS[2]
local roleKey = KEYS[3]
local indexKey = KEYS[4]
local roleId = ARGV[1]
local roleJson = ARGV[2]
local mapped = redis.call('GET', newNameKey)
if mapped and mapped ~= roleId then
  return 0
end
redis.call('SET', roleKey, roleJson)
redis.call('SADD', indexKey, roleId)
redis.call('SET', newNameKey, roleId)
local oldMapped = redis.call('GET', oldNameKey)
if (not oldMapped) or oldMapped == roleId then
  redis.call('DEL', oldNameKey)
end
return 1";

                var result = await _db.ScriptEvaluateAsync(renameScript,
                    new RedisKey[] { RedisKeyHelper.IamRoleByName(oldNormalized), nameKey, roleKey, RedisKeyHelper.IndexRoles },
                    new RedisValue[] { role.Id.Value, json });

                if ((long)result != 1)
                    throw new DomainException($"Role name '{normalized}' already exists", "ROLE_NAME_ALREADY_EXISTS");
                return;
            }
        }

        const string updateScript = @"
local nameKey = KEYS[1]
local roleKey = KEYS[2]
local indexKey = KEYS[3]
local roleId = ARGV[1]
local roleJson = ARGV[2]
local mapped = redis.call('GET', nameKey)
if mapped and mapped ~= roleId then
  return 0
end
redis.call('SET', roleKey, roleJson)
redis.call('SADD', indexKey, roleId)
redis.call('SET', nameKey, roleId)
return 1";

        var updateResult = await _db.ScriptEvaluateAsync(updateScript,
            new RedisKey[] { nameKey, roleKey, RedisKeyHelper.IndexRoles },
            new RedisValue[] { role.Id.Value, json });

        if ((long)updateResult != 1)
            throw new DomainException($"Role name '{normalized}' already exists", "ROLE_NAME_ALREADY_EXISTS");
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(id, cancellationToken);
        if (role == null) return;

        var normalized = RedisKeyHelper.NormalizeRoleName(role.Name);
        var nameKey = RedisKeyHelper.IamRoleByName(normalized);

        const string script = @"
local nameKey = KEYS[1]
local roleKey = KEYS[2]
local indexKey = KEYS[3]
local roleId = ARGV[1]
local mapped = redis.call('GET', nameKey)
if (not mapped) or mapped == roleId then
  redis.call('DEL', nameKey)
end
redis.call('DEL', roleKey)
redis.call('SREM', indexKey, roleId)
return 1";

        await _db.ScriptEvaluateAsync(script,
            new RedisKey[] { nameKey, RedisKeyHelper.IamRole(id), RedisKeyHelper.IndexRoles },
            new RedisValue[] { id.Value });
    }

    private static string Serialize(RoleEntity role)
    {
        var dto = new RoleSnapshot(
            role.Id.Value,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.Status.Value,
            role.Permissions.ToList(),
            role.CreatedAt,
            role.UpdatedAt,
            role.UpdatedBy);
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static RoleEntity? Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<RoleSnapshot>(json, JsonOptions);
        return dto is null ? null : RoleDataMapper.ToEntity(dto);
    }
}
