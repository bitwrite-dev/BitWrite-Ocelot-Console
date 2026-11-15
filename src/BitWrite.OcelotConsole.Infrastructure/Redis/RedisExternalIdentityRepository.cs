using System.Reflection;
using System.Text.Json;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using StackExchange.Redis;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

public class RedisExternalIdentityRepository : IExternalIdentityRepository
{
    private readonly IDatabase _db;

    public RedisExternalIdentityRepository(IConnectionMultiplexer connectionMultiplexer)
    {
        _db = connectionMultiplexer.GetDatabase();
    }

    public async Task<ExternalIdentity?> GetByIssuerAndSubjectAsync(string issuer, string subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
            return null;

        var linkKey = RedisKeyHelper.IamExternalIdentity(issuer.Trim(), subject.Trim());
        var idValue = await _db.StringGetAsync(linkKey);
        if (idValue.IsNullOrEmpty || !Guid.TryParse(idValue.ToString(), out var id))
            return null;

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ExternalIdentity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var json = await _db.StringGetAsync(RedisKeyHelper.IamExternalIdentityData(id));
        if (json.IsNullOrEmpty)
            return null;

        var dto = JsonSerializer.Deserialize<ExternalIdentityDto>(json.ToString());
        return dto is null ? null : ToEntity(dto);
    }

    public async Task<List<ExternalIdentity>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(RedisKeyHelper.IamUserExternalIdentities(userId));
        var result = new List<ExternalIdentity>();
        foreach (var member in members)
        {
            if (!Guid.TryParse(member.ToString(), out var id))
                continue;
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity is not null)
                result.Add(entity);
        }
        return result;
    }

    public async Task AddAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken = default)
    {
        await PersistAsync(externalIdentity);
    }

    public async Task UpdateAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken = default)
    {
        await PersistAsync(externalIdentity);
    }

    private async Task PersistAsync(ExternalIdentity externalIdentity)
    {
        var dto = ToDto(externalIdentity);
        var json = JsonSerializer.Serialize(dto);
        await _db.StringSetAsync(RedisKeyHelper.IamExternalIdentityData(externalIdentity.Id), json);
        await _db.StringSetAsync(RedisKeyHelper.IamExternalIdentity(externalIdentity.Issuer, externalIdentity.Subject), externalIdentity.Id.ToString("D"));
        await _db.SetAddAsync(RedisKeyHelper.IamUserExternalIdentities(externalIdentity.UserId), externalIdentity.Id.ToString("D"));
    }

    private static ExternalIdentityDto ToDto(ExternalIdentity entity) => new(
        entity.Id,
        entity.UserId.Value.ToString(),
        entity.Issuer,
        entity.Subject,
        entity.ProviderType,
        entity.CreatedAt,
        entity.LastAuthenticatedAt);

    private static ExternalIdentity ToEntity(ExternalIdentityDto dto)
    {
        var entity = (ExternalIdentity)Activator.CreateInstance(typeof(ExternalIdentity), nonPublic: true)!;
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.Id), dto.Id);
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.UserId), UserId.From(dto.UserId));
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.Issuer), dto.Issuer);
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.Subject), dto.Subject);
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.ProviderType), dto.ProviderType);
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.CreatedAt), dto.CreatedAt);
        SetPrivateAutoProperty(entity, nameof(ExternalIdentity.LastAuthenticatedAt), dto.LastAuthenticatedAt);
        SetPrivateField(entity, "_domainEvents", new List<BitWrite.OcelotConsole.Domain.Events.DomainEvent>());
        return entity;
    }

    private static void SetPrivateAutoProperty(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop?.SetMethod is not null)
        {
            prop.SetValue(target, value);
            return;
        }

        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private sealed record ExternalIdentityDto(
        Guid Id,
        string UserId,
        string Issuer,
        string Subject,
        string ProviderType,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastAuthenticatedAt);
}
