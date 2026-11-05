using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User;

/// <summary>
/// User Aggregate Root - Internal User representing every person who accesses
/// the BitWrite Management Plane (§4.1, §4.2).
/// </summary>
/// <remarks>
/// Every authenticated identity must be mapped to an internal BitWrite User (§4.3).
/// The User owns its lifecycle: Active / Disabled (§5.1).
/// Physical deletion is not part of the initial lifecycle (§5.6).
/// </summary>
public sealed class User
{
    private readonly List<DomainEvent> _domainEvents = new();
    private readonly List<string> _roleIds = new();
    private readonly List<Guid> _externalIdentityIds = new();

    public UserId Id { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string UserType { get; private set; } = null!; // "Local" or "External"
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public DateTimeOffset? LastAuthenticatedAt { get; private set; }

    public IReadOnlyList<string> RoleIds => _roleIds.AsReadOnly();
    public IReadOnlyList<Guid> ExternalIdentityIds => _externalIdentityIds.AsReadOnly();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private User() { }

    /// <summary>
    /// Creates a new local User (§5.2).
    /// </summary>
    public static User CreateLocal(
        UserId id,
        string username,
        string? displayName,
        string? email,
        string createdBy,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty", "INVALID_USERNAME");

        var normalizedUsername = username.Trim().ToLowerInvariant();
        if (normalizedUsername.Length < 3)
            throw new DomainException("Username must be at least 3 characters", "USERNAME_TOO_SHORT");

        var user = new User
        {
            Id = id,
            Username = normalizedUsername,
            DisplayName = displayName?.Trim(),
            Email = email?.Trim(),
            Status = UserStatus.Active,
            UserType = "Local",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            LastAuthenticatedAt = null
        };

        user.AddDomainEvent(new UserCreated(id, normalizedUsername, "Local", createdBy, correlationId));
        return user;
    }

    /// <summary>
    /// Creates a new external User via JIT provisioning (§5.2, §7.2).
    /// </summary>
    public static User CreateExternal(
        UserId id,
        string username,
        string? displayName,
        string? email,
        string createdBy,
        string correlationId = "")
    {
        var user = CreateLocal(id, username, displayName, email, createdBy, correlationId);
        user.UserType = "External";
        return user;
    }

    /// <summary>
    /// Updates user profile (§5.3).
    /// </summary>
    public void UpdateProfile(
        string? displayName,
        string? email,
        string updatedBy,
        string correlationId = "")
    {
        if (Status == UserStatus.Disabled)
            throw new DomainException("Cannot update a disabled user", "USER_DISABLED");

        DisplayName = displayName?.Trim();
        Email = email?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;

        AddDomainEvent(new UserUpdated(Id, updatedBy, correlationId));
    }

    /// <summary>
    /// Disables the user (§5.4).
    /// </summary>
    public void Disable(string disabledBy, string correlationId = "")
    {
        if (Status == UserStatus.Disabled)
            throw new DomainException("User is already disabled", "USER_ALREADY_DISABLED");

        Status = UserStatus.Disabled;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = disabledBy;

        AddDomainEvent(new UserDisabled(Id, disabledBy, correlationId));
    }

    /// <summary>
    /// Enables the user (§5.5).
    /// </summary>
    public void Enable(string enabledBy, string correlationId = "")
    {
        if (Status == UserStatus.Active)
            throw new DomainException("User is already active", "USER_ALREADY_ACTIVE");

        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = enabledBy;

        AddDomainEvent(new UserEnabled(Id, enabledBy, correlationId));
    }

    /// <summary>
    /// Records successful authentication (§9).
    /// </summary>
    public void RecordAuthentication(string correlationId = "")
    {
        LastAuthenticatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new UserAuthenticated(Id, correlationId));
    }

    /// <summary>
    /// Assigns a role to the user (§6.2).
    /// </summary>
    public void AssignRole(string roleId, string correlationId = "")
    {
        if (Status == UserStatus.Disabled)
            throw new DomainException("Cannot assign role to disabled user", "USER_DISABLED");

        if (_roleIds.Contains(roleId))
            throw new DomainException($"Role {roleId} is already assigned to user", "ROLE_ALREADY_ASSIGNED");

        _roleIds.Add(roleId);
        AddDomainEvent(new RoleAssignedToUser(Id, roleId, correlationId));
    }

    /// <summary>
    /// Removes a role from the user (§6.2).
    /// </summary>
    public void RemoveRole(string roleId, string correlationId = "")
    {
        if (!_roleIds.Contains(roleId))
            throw new DomainException($"Role {roleId} is not assigned to user", "ROLE_NOT_ASSIGNED");

        _roleIds.Remove(roleId);
        AddDomainEvent(new RoleRemovedFromUser(Id, roleId, correlationId));
    }

    /// <summary>
    /// Links an external identity to the user (§7.4).
    /// </summary>
    public void LinkExternalIdentity(Guid externalIdentityId, string correlationId = "")
    {
        if (_externalIdentityIds.Contains(externalIdentityId))
            throw new DomainException($"External identity {externalIdentityId} is already linked", "EXTERNAL_IDENTITY_ALREADY_LINKED");

        _externalIdentityIds.Add(externalIdentityId);
        AddDomainEvent(new ExternalIdentityLinked(Id, externalIdentityId, correlationId));
    }

    /// <summary>
    /// Unlinks an external identity from the user (§7.4).
    /// </summary>
    public void UnlinkExternalIdentity(Guid externalIdentityId, string correlationId = "")
    {
        if (!_externalIdentityIds.Contains(externalIdentityId))
            throw new DomainException($"External identity {externalIdentityId} is not linked", "EXTERNAL_IDENTITY_NOT_LINKED");

        _externalIdentityIds.Remove(externalIdentityId);
        AddDomainEvent(new ExternalIdentityUnlinked(Id, externalIdentityId, correlationId));
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
