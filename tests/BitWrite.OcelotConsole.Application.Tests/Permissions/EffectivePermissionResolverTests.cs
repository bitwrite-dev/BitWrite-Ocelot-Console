using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using UserEntity = BitWrite.OcelotConsole.Domain.Aggregates.User.User;

namespace BitWrite.OcelotConsole.Application.Tests.Permissions;

public class EffectivePermissionResolverTests
{
    [Fact]
    public async Task Resolve_ActiveUserWithActiveRoles_ReturnsUnionOfValidActivePermissionsDeduped()
    {
        var user = ActiveUser();
        var roleA = RoleEntity.Create(RoleId.New(), "role-a", null, "admin");
        roleA.AddPermission("perm.read", "admin");
        roleA.AddPermission("perm.write", "admin");
        var roleB = RoleEntity.Create(RoleId.New(), "role-b", null, "admin");
        roleB.AddPermission(" PERM.READ ", "admin"); // duplicate, different casing/spacing
        roleB.AddPermission("perm.delete", "admin");

        user.AssignRole(roleA.Id, "c1");
        user.AssignRole(roleB.Id, "c1");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            Def("perm.read", PermissionStatus.Active, true),
            Def("perm.write", PermissionStatus.Active, true),
            Def("perm.delete", PermissionStatus.Active, true)
        });
        var sut = CreateSut(user, catalog, roleA, roleB);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEquivalentTo(new[] { "perm.read", "perm.write", "perm.delete" });
    }

    [Fact]
    public async Task Resolve_InactiveRole_ContributesNone()
    {
        var user = ActiveUser();
        var activeRole = RoleEntity.Create(RoleId.New(), "active-role", null, "admin");
        activeRole.AddPermission("perm.read", "admin");
        var inactiveRole = RoleEntity.Create(RoleId.New(), "inactive-role", null, "admin");
        inactiveRole.AddPermission("perm.write", "admin");
        inactiveRole.Deactivate("admin");

        user.AssignRole(activeRole.Id, "c1");
        user.AssignRole(inactiveRole.Id, "c1");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            Def("perm.read", PermissionStatus.Active, true),
            Def("perm.write", PermissionStatus.Active, true)
        });
        var sut = CreateSut(user, catalog, activeRole, inactiveRole);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEquivalentTo(new[] { "perm.read" });
    }

    [Fact]
    public async Task Resolve_DisabledUser_ReturnsNone()
    {
        var user = ActiveUser();
        var role = RoleEntity.Create(RoleId.New(), "role-a", null, "admin");
        role.AddPermission("perm.read", "admin");
        user.AssignRole(role.Id, "c1");
        user.Disable("admin", "c2");

        var catalog = new SourceCodePermissionCatalog(new[] { Def("perm.read", PermissionStatus.Active, true) });
        var sut = CreateSut(user, catalog, role);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_UserNotFound_ReturnsNone()
    {
        var catalog = new SourceCodePermissionCatalog(Array.Empty<PermissionDefinition>());
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((UserEntity?)null);
        var roles = new Mock<IRoleRepository>(MockBehavior.Strict);
        var sut = new EffectivePermissionResolver(users.Object, roles.Object, catalog);

        var result = await sut.ResolveAsync(UserId.New());

        result.Should().BeEmpty();
        roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resolve_RetiredAndUnknownPermissions_NeverContribute()
    {
        var user = ActiveUser();
        var role = RoleEntity.Create(RoleId.New(), "role-a", null, "admin");
        role.AddPermission("perm.active", "admin");
        role.AddPermission("perm.retired", "admin");
        role.AddPermission("perm.unknown", "admin");
        user.AssignRole(role.Id, "c1");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            Def("perm.active", PermissionStatus.Active, true),
            Def("perm.retired", PermissionStatus.Retired, true)
            // perm.unknown not in catalog
        });
        var sut = CreateSut(user, catalog, role);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEquivalentTo(new[] { "perm.active" });
    }

    [Fact]
    public async Task Resolve_RoleWithOnlyRetiredOrUnknownPermissions_ReturnsNone()
    {
        var user = ActiveUser();
        var role = RoleEntity.Create(RoleId.New(), "role-a", null, "admin");
        role.AddPermission("perm.retired", "admin");
        role.AddPermission("perm.unknown", "admin");
        user.AssignRole(role.Id, "c1");

        var catalog = new SourceCodePermissionCatalog(new[] { Def("perm.retired", PermissionStatus.Retired, true) });
        var sut = CreateSut(user, catalog, role);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Resolve_MixedRolesAndPermissions_ReflectsCurrentStateAndNormalizes()
    {
        var user = ActiveUser();
        var role1 = RoleEntity.Create(RoleId.New(), "role-1", null, "admin");
        role1.AddPermission("Perm.A", "admin");
        role1.AddPermission("perm.retired", "admin");
        var role2 = RoleEntity.Create(RoleId.New(), "role-2", null, "admin");
        role2.AddPermission("perm.b", "admin");
        role2.AddPermission("PERM.A", "admin");
        var inactive = RoleEntity.Create(RoleId.New(), "role-3", null, "admin");
        inactive.AddPermission("perm.c", "admin");
        inactive.Deactivate("admin");

        user.AssignRole(role1.Id, "c1");
        user.AssignRole(role2.Id, "c1");
        user.AssignRole(inactive.Id, "c1");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            Def("perm.a", PermissionStatus.Active, true),
            Def("perm.b", PermissionStatus.Active, false), // active but non-assignable still grants if present
            Def("perm.c", PermissionStatus.Active, true),
            Def("perm.retired", PermissionStatus.Retired, true)
        });
        var sut = CreateSut(user, catalog, role1, role2, inactive);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEquivalentTo(new[] { "perm.a", "perm.b" });
    }

    [Fact]
    public async Task Resolve_AssignmentToMissingRole_IsIgnoredSideEffectFree()
    {
        var user = ActiveUser();
        var existing = RoleEntity.Create(RoleId.New(), "role-a", null, "admin");
        existing.AddPermission("perm.read", "admin");
        var missingId = RoleId.New();
        user.AssignRole(existing.Id, "c1");
        user.AssignRole(missingId, "c1");

        var catalog = new SourceCodePermissionCatalog(new[] { Def("perm.read", PermissionStatus.Active, true) });
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        roles.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        var sut = new EffectivePermissionResolver(users.Object, roles.Object, catalog);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEquivalentTo(new[] { "perm.read" });
        users.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resolve_ActiveUserWithNoRoles_ReturnsEmpty()
    {
        var user = ActiveUser();
        var catalog = new SourceCodePermissionCatalog(Array.Empty<PermissionDefinition>());
        var sut = CreateSut(user, catalog);

        var result = await sut.ResolveAsync(user.Id);

        result.Should().BeEmpty();
    }

    private static EffectivePermissionResolver CreateSut(UserEntity user, IPermissionCatalog catalog, params RoleEntity[] roles)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var roleRepo = new Mock<IRoleRepository>();
        foreach (var role in roles)
            roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        return new EffectivePermissionResolver(users.Object, roleRepo.Object, catalog);
    }

    private static UserEntity ActiveUser() =>
        UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");

    private static PermissionDefinition Def(string code, PermissionStatus status, bool assignable) =>
        new(code, code, null, "test", status, assignable);
}
