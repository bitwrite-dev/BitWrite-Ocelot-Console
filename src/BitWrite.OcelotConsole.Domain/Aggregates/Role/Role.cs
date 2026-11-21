using System.Text.Json.Serialization;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Domain.Aggregates.Role;

/// <summary>
/// Role Aggregate - Represents a role in the RBAC system (§6.2).
/// </summary>
/// <remarks>
/// A Role is an assignment mechanism that groups permission references.
/// Canonical identity is <see cref="RoleId"/>; <see cref="Name"/> is the unique technical,
/// JWT-facing role name. Active/Inactive lifecycle affects authorization eligibility;
/// user-role assignments are managed outside this aggregate and are not removed on deactivation.
/// </remarks>
public sealed class Role
{
    private readonly List<DomainEvent> _domainEvents = new();
    private List<string> _permissions = new();

    public RoleId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    public RoleStatus Status { get; private set; } = RoleStatus.Active;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public IReadOnlyList<string> Permissions
    {
        get => _permissions.AsReadOnly();
        private set => _permissions = new List<string>(value);
    }
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public bool IsActive => Status.IsActive;

    [JsonConstructor]
    private Role() { }

    public static Role Create(
        RoleId id,
        string name,
        string? description,
        string createdBy,
        string correlationId = "")
    {
        var normalizedName = NormalizeName(name);

        var role = new Role
        {
            Id = id,
            Name = normalizedName,
            Description = description?.Trim() ?? string.Empty,
            IsSystemRole = false,
            Status = RoleStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        role.AddDomainEvent(new RoleCreated(role.Id, role.Name, createdBy, correlationId));
        return role;
    }

    /// <summary>
    /// Renames the role. Allowed only when no stored permission references exist.
    /// </summary>
    /// <remarks>
    /// The check is based on stored permission references (including retained historical
    /// references), not on active/effective catalog permissions.
    /// </remarks>
    public void Rename(string newName, string updatedBy, string correlationId = "")
    {
        var normalizedName = NormalizeName(newName);

        if (_permissions.Count > 0)
            throw new DomainException(
                "Role name cannot be changed while the role has stored permission references",
                "ROLE_RENAME_NOT_ALLOWED");

        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
            return;

        var oldName = Name;
        Name = normalizedName;
        MarkUpdated(updatedBy);
        AddDomainEvent(new RoleRenamed(Id, oldName, normalizedName, updatedBy, correlationId));
    }

    public void Activate(string activatedBy, string correlationId = "")
    {
        if (Status.IsActive)
            throw new DomainException("Role is already active", "ROLE_ALREADY_ACTIVE");

        Status = RoleStatus.Active;
        MarkUpdated(activatedBy);
        AddDomainEvent(new RoleActivated(Id, activatedBy, correlationId));
    }

    public void Deactivate(string deactivatedBy, string correlationId = "")
    {
        if (Status.IsInactive)
            throw new DomainException("Role is already inactive", "ROLE_ALREADY_INACTIVE");

        Status = RoleStatus.Inactive;
        MarkUpdated(deactivatedBy);
        AddDomainEvent(new RoleDeactivated(Id, deactivatedBy, correlationId));
    }

    /// <summary>
    /// Adds a stored permission reference to the role.
    /// </summary>
    public void AddPermission(string permission, string updatedBy, string correlationId = "")
    {
        var normalizedPermission = NormalizePermission(permission);
        if (_permissions.Contains(normalizedPermission))
            throw new DomainException(
                $"Permission {normalizedPermission} is already assigned to role",
                "PERMISSION_ALREADY_ASSIGNED");

        _permissions.Add(normalizedPermission);
        MarkUpdated(updatedBy);
        AddDomainEvent(new PermissionAddedToRole(Id, normalizedPermission, updatedBy, correlationId));
    }

    /// <summary>
    /// Removes a stored permission reference from the role.
    /// </summary>
    public void RemovePermission(string permission, string updatedBy, string correlationId = "")
    {
        var normalizedPermission = NormalizePermission(permission);
        if (!_permissions.Contains(normalizedPermission))
            throw new DomainException(
                $"Permission {normalizedPermission} is not assigned to role",
                "PERMISSION_NOT_ASSIGNED");

        _permissions.Remove(normalizedPermission);
        MarkUpdated(updatedBy);
        AddDomainEvent(new PermissionRemovedFromRole(Id, normalizedPermission, updatedBy, correlationId));
    }

    public void UpdateDescription(string? description, string updatedBy, string correlationId = "")
    {
        Description = description?.Trim() ?? string.Empty;
        MarkUpdated(updatedBy);
        AddDomainEvent(new RoleUpdated(Id, Name, Description, updatedBy, correlationId));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty", "INVALID_ROLE_NAME");

        var normalizedName = name.Trim().ToLowerInvariant();
        if (normalizedName.Length < 3)
            throw new DomainException("Role name must be at least 3 characters", "ROLE_NAME_TOO_SHORT");

        return normalizedName;
    }

    private static string NormalizePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new DomainException("Permission cannot be empty", "INVALID_PERMISSION");

        return permission.Trim().ToLowerInvariant();
    }

    private void MarkUpdated(string updatedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
