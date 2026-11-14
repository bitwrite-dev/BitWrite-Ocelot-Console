using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Tests.Aggregates;

public class RoleAggregateTests
{
    [Fact]
    public void Create_ValidInput_CreatesActiveRoleWithNormalizedName()
    {
        var id = RoleId.New();
        var role = Role.Create(id, "  Operator  ", "  Desc  ", "admin", "c1");

        Assert.Equal(id, role.Id);
        Assert.Equal("operator", role.Name);
        Assert.Equal("Desc", role.Description);
        Assert.False(role.IsSystemRole);
        Assert.True(role.Status.IsActive);
        Assert.True(role.IsActive);
        Assert.Empty(role.Permissions);
        var created = Assert.IsType<RoleCreated>(Assert.Single(role.DomainEvents));
        Assert.Equal(id, created.RoleId);
        Assert.Equal("operator", created.Name);
        Assert.Equal("admin", created.CreatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => Role.Create(RoleId.New(), name!, null, "admin"));
        Assert.Equal("INVALID_ROLE_NAME", ex.ErrorCode);
    }

    [Fact]
    public void Create_ShortName_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => Role.Create(RoleId.New(), "ab", null, "admin"));
        Assert.Equal("ROLE_NAME_TOO_SHORT", ex.ErrorCode);
    }

    [Fact]
    public void Rename_NoStoredRefs_UpdatesNameKeepsIdentityAndAssignmentsAndEmitsEvent()
    {
        var id = RoleId.New();
        var role = Role.Create(id, "operator", null, "admin");
        role.ClearDomainEvents();

        role.Rename("  OPS-Manager ", "editor", "c2");

        Assert.Equal(id, role.Id);
        Assert.Equal("ops-manager", role.Name);
        Assert.Equal("editor", role.UpdatedBy);
        var renamed = Assert.IsType<RoleRenamed>(Assert.Single(role.DomainEvents));
        Assert.Equal(id, renamed.RoleId);
        Assert.Equal("operator", renamed.OldName);
        Assert.Equal("ops-manager", renamed.NewName);
        Assert.Equal("editor", renamed.UpdatedBy);
    }

    [Fact]
    public void Rename_SameNormalizedName_DoesNotChangeOrEmit()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.ClearDomainEvents();

        role.Rename("  OPERATOR  ", "editor");

        Assert.Equal("operator", role.Name);
        Assert.Empty(role.DomainEvents);
        Assert.Null(role.UpdatedBy);
    }

    [Fact]
    public void Rename_WithStoredPermissionRefs_ThrowsAndKeepsState()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        role.ClearDomainEvents();
        var before = role.UpdatedAt;

        var ex = Assert.Throws<DomainException>(() => role.Rename("new-name", "editor"));

        Assert.Equal("ROLE_RENAME_NOT_ALLOWED", ex.ErrorCode);
        Assert.Equal("operator", role.Name);
        Assert.Equal(before, role.UpdatedAt);
        Assert.Empty(role.DomainEvents);
        Assert.Equal(new[] { "catalog.read" }, role.Permissions);
    }

    [Fact]
    public void Rename_AfterRemovingAllStoredPermissionRefs_Succeeds()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        role.RemovePermission("catalog.read", "admin");

        role.Rename("ops", "editor");

        Assert.Equal("ops", role.Name);
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Rename_AllowedWhenInactiveOrActiveRegardlessOfAssignmentState()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.Deactivate("admin");

        role.Rename("ops", "editor");
        Assert.Equal("ops", role.Name);
        Assert.True(role.Status.IsInactive);

        role.Activate("admin");
        role.Rename("operator", "editor");
        Assert.Equal("operator", role.Name);
        Assert.True(role.Status.IsActive);
    }

    [Fact]
    public void Lifecycle_ActivateDeactivate_TransitionsAndRetainsAssignmentsSemantics()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.ClearDomainEvents();

        role.Deactivate("admin", "c1");
        Assert.True(role.Status.IsInactive);
        Assert.IsType<RoleDeactivated>(Assert.Single(role.DomainEvents));

        var exDeactivate = Assert.Throws<DomainException>(() => role.Deactivate("admin"));
        Assert.Equal("ROLE_ALREADY_INACTIVE", exDeactivate.ErrorCode);

        role.ClearDomainEvents();
        role.Activate("admin", "c2");
        Assert.True(role.Status.IsActive);
        Assert.IsType<RoleActivated>(Assert.Single(role.DomainEvents));

        var exActivate = Assert.Throws<DomainException>(() => role.Activate("admin"));
        Assert.Equal("ROLE_ALREADY_ACTIVE", exActivate.ErrorCode);
    }

    [Fact]
    public void AddPermission_NormalizesStoresAndEmitsEvent()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.ClearDomainEvents();

        role.AddPermission("  Catalog.Read  ", "admin", "c1");

        Assert.Equal(new[] { "catalog.read" }, role.Permissions);
        var added = Assert.IsType<PermissionAddedToRole>(Assert.Single(role.DomainEvents));
        Assert.Equal("catalog.read", added.Permission);
    }

    [Fact]
    public void AddPermission_Duplicate_ThrowsPreservesInvariant()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");

        var ex = Assert.Throws<DomainException>(() => role.AddPermission(" CATALOG.READ ", "admin"));

        Assert.Equal("PERMISSION_ALREADY_ASSIGNED", ex.ErrorCode);
        Assert.Single(role.Permissions);
    }

    [Fact]
    public void RemovePermission_Existing_RemovesAndEmitsEvent()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        role.ClearDomainEvents();

        role.RemovePermission("CATALOG.READ", "admin", "c1");

        Assert.Empty(role.Permissions);
        var removed = Assert.IsType<PermissionRemovedFromRole>(Assert.Single(role.DomainEvents));
        Assert.Equal("catalog.read", removed.Permission);
    }

    [Fact]
    public void RemovePermission_NotAssigned_Throws()
    {
        var role = Role.Create(RoleId.New(), "operator", null, "admin");

        var ex = Assert.Throws<DomainException>(() => role.RemovePermission("catalog.read", "admin"));

        Assert.Equal("PERMISSION_NOT_ASSIGNED", ex.ErrorCode);
    }

    [Fact]
    public void UpdateDescription_UpdatesAndEmitsEvent()
    {
        var role = Role.Create(RoleId.New(), "operator", "old", "admin");
        role.ClearDomainEvents();

        role.UpdateDescription("  new desc  ", "editor", "c1");

        Assert.Equal("new desc", role.Description);
        var updated = Assert.IsType<RoleUpdated>(Assert.Single(role.DomainEvents));
        Assert.Equal("operator", updated.Name);
        Assert.Equal("new desc", updated.Description);
    }

    [Fact]
    public void RoleId_IsImmutableAcrossOperations()
    {
        var id = RoleId.New();
        var role = Role.Create(id, "operator", null, "admin");

        role.Deactivate("admin");
        role.Activate("admin");
        role.AddPermission("catalog.read", "admin");
        role.RemovePermission("catalog.read", "admin");
        role.Rename("ops", "admin");
        role.UpdateDescription("d", "admin");

        Assert.Equal(id, role.Id);
    }
}
