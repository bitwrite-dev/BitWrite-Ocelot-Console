using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.Role;

/// <summary>
/// Role Aggregate - Represents a role in the RBAC system (§6.2).
/// </summary>
/// <remarks>
/// A Role contains one or more Permissions. Users are assigned Roles via UserRoleAssignment.
/// Roles are managed by operators/administrators.
/// </summary>
public sealed class Role
{
    private readonly List<DomainEvent> _domainEvents = new();
    private readonly List<string> _permissions = new();

    public RoleId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }

    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Role() { }

    /// <summary>
    /// Creates a new Role (§6.2).
    /// </summary>
    public static Role Create(
        RoleId id,
        string name,
        string? description,
        string createdBy,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty", "INVALID_ROLE_NAME");

        var normalizedName = name.Trim().ToLowerInvariant();
        if (normalizedName.Length < 3)
            throw new DomainException("Role name must be at least 3 characters", "ROLE_NAME_TOO_SHORT");

        var role = new Role
        {
            Id = id,
            Name = normalizedName,
            Description = description?.Trim(),
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        role.AddDomainEvent(new RoleCreated(role.Id, role.Name, createdBy, correlationId));
        return role;
    }

    /// <summary>
    /// Adds a permission to the role (§6.2).
    /// </summary>
    public void AddPermission(string permission, string updatedBy, string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new DomainException("Permission cannot be empty", "INVALID_PERMISSION");

        var normalizedPermission = permission.Trim().ToLowerInvariant();
        if (_permissions.Contains(normalizedPermission))
            throw new DomainException($"Permission {normalizedPermission} is already assigned to role", "PERMISSION_ALREADY_ASSIGNED");

        _permissions.Add(normalizedPermission);
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new PermissionAddedToRole(Id, normalizedPermission, updatedBy, correlationId));
    }

    /// <summary>
    /// Removes a permission from the role (§6.2).
    /// </summary>
    public void RemovePermission(string permission, string updatedBy, string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new DomainException("Permission cannot be empty", "INVALID_PERMISSION");

        var normalizedPermission = permission.Trim().ToLowerInvariant();
        if (!_permissions.Contains(normalizedPermission))
            throw new DomainException($"Permission {normalizedPermission} is not assigned to role", "PERMISSION_NOT_ASSIGNED");

        _permissions.Remove(normalizedPermission);
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new PermissionRemovedFromRole(Id, normalizedPermission, updatedBy, correlationId));
    }

    /// <summary>
    /// Updates the role description.
    /// </summary>
    public void UpdateDescription(string? description, string updatedBy, string correlationId = "")
    {
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new RoleUpdated(Id, Name, description, updatedBy, correlationId));
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
