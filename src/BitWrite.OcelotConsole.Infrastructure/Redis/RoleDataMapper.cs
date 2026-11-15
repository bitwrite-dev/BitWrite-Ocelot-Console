using System.Reflection;
using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Infrastructure.Redis;

internal static class RoleDataMapper
{
    internal static Role ToEntity(RoleSnapshot snapshot)
    {
        var role = (Role)Activator.CreateInstance(typeof(Role), nonPublic: true)!;
        SetPrivateAutoProperty(role, nameof(Role.Id), RoleId.From(snapshot.Id));
        SetPrivateAutoProperty(role, nameof(Role.Name), RedisKeyHelper.NormalizeRoleName(snapshot.Name));
        SetPrivateAutoProperty(role, nameof(Role.Description), snapshot.Description?.Trim() ?? string.Empty);
        SetPrivateAutoProperty(role, nameof(Role.IsSystemRole), snapshot.IsSystemRole);
        SetPrivateAutoProperty(role, nameof(Role.Status), snapshot.Status is null ? RoleStatus.Active : RoleStatus.From(snapshot.Status));
        SetPrivateAutoProperty(role, nameof(Role.CreatedAt), snapshot.CreatedAt);
        SetPrivateAutoProperty(role, nameof(Role.UpdatedAt), snapshot.UpdatedAt);
        SetPrivateAutoProperty(role, nameof(Role.UpdatedBy), snapshot.UpdatedBy);

        SetPrivateField(role, "_permissions", (snapshot.Permissions ?? [])
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList());
        SetPrivateField(role, "_domainEvents", new List<BitWrite.OcelotConsole.Domain.Events.DomainEvent>());
        return role;
    }

    internal static UserRoleMigrationResult MigrateUserRoleRefs(
        IReadOnlyList<string> storedRefs,
        IReadOnlyDictionary<string, string> normalizedNameToRoleId)
    {
        var migrated = new List<string>();
        var unmappable = new List<string>();
        var changed = false;

        foreach (var raw in storedRefs)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            var normalizedName = trimmed.ToLowerInvariant();

            // Legacy name ref takes precedence if a role with that name exists.
            if (normalizedNameToRoleId.TryGetValue(normalizedName, out var roleIdByName))
            {
                if (!migrated.Contains(roleIdByName, StringComparer.Ordinal))
                    migrated.Add(roleIdByName);
                if (!string.Equals(trimmed, roleIdByName, StringComparison.Ordinal))
                    changed = true;
                continue;
            }

            // Known RoleId value.
            if (normalizedNameToRoleId.Values.Contains(trimmed, StringComparer.Ordinal))
            {
                if (!migrated.Contains(trimmed, StringComparer.Ordinal))
                    migrated.Add(trimmed);
                if (!string.Equals(raw, trimmed, StringComparison.Ordinal))
                    changed = true;
                continue;
            }

            // Valid RoleId-shaped value not currently known/mappable: preserve explicitly as-is candidate is unsafe;
            // treat as unmappable to avoid privilege escalation via invented ids.
            if (!unmappable.Contains(trimmed, StringComparer.Ordinal))
                unmappable.Add(trimmed);
        }

        if (migrated.Count != storedRefs.Count(r => !string.IsNullOrWhiteSpace(r)) || unmappable.Count > 0)
            changed = true;

        return new UserRoleMigrationResult(migrated, unmappable, changed);
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
}

internal sealed record RoleSnapshot(
    string Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    string? Status,
    List<string>? Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

internal sealed record UserRoleMigrationResult(
    List<string> MigratedRoleIds,
    List<string> UnmappableRefs,
    bool Changed);
