using System.Security.Claims;
using BitWrite.OcelotConsole.Api.Authorization;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using UserEntity = BitWrite.OcelotConsole.Domain.Aggregates.User.User;

namespace BitWrite.OcelotConsole.Api.Tests.Authorization;

public class CurrentStatePermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Succeeds_WhenUserActiveAssignedActiveRoleAndEffectivePermissionGranted()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.read", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Fails_WhenRequestedPermissionMissing_DefaultDeny()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.write", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenUserDisabled_CurrentState()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        user.Disable("admin", "c1");
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.read", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenRoleInactive_CurrentState()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        role.Deactivate("admin");
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.read", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenRoleUnassigned_StaleClaim()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        user.RemoveRole(role.Id);
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.read", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenClaimedRoleNameUnresolvable_StaleOrRenamed()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        var users = MockUsers(user);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        roles.Setup(r => r.GetIdByNameAsync("operator", It.IsAny<CancellationToken>())).ReturnsAsync((RoleId?)null);
        var sut = new CurrentStatePermissionAuthorizationHandler(users.Object, roles.Object, new EffectivePermissionResolver(users.Object, roles.Object, catalog));
        var context = CreateContext(user.Id, "perm.read", "operator");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenRetiredOrUnknownPermissionWouldBeRequired()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("perm.retired", "admin");
        role.AddPermission("perm.unknown", "admin");
        user.AssignRole(role.Id, "c1");
        var catalog = new SourceCodePermissionCatalog(new[] { new PermissionDefinition("perm.retired", "R", null, null, PermissionStatus.Retired, true) });
        var sut = CreateSut(user, catalog, role);

        var ctxRetired = CreateContext(user.Id, "perm.retired", "operator");
        await sut.HandleAsync(ctxRetired);
        ctxRetired.HasSucceeded.Should().BeFalse();

        var ctxUnknown = CreateContext(user.Id, "perm.unknown", "operator");
        await sut.HandleAsync(ctxUnknown);
        ctxUnknown.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenNoRoleClaimsPresent()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        var sut = CreateSut(user, catalog, role);
        var context = CreateContext(user.Id, "perm.read");

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_WhenUnauthenticated()
    {
        var (user, role, catalog) = EligibleState("operator", "perm.read");
        var sut = CreateSut(user, catalog, role);
        var identity = new ClaimsIdentity(); // not authenticated
        identity.AddClaim(new Claim("sub", user.Id.Value.ToString()));
        identity.AddClaim(new Claim(SecurityService.RoleClaimType, "operator"));
        var principal = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement("perm.read");
        var context = new AuthorizationHandlerContext(new[] { requirement }, principal, null);

        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    private static (UserEntity user, RoleEntity role, IPermissionCatalog catalog) EligibleState(string roleName, string permission)
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), roleName, null, "admin");
        role.AddPermission(permission, "admin");
        user.AssignRole(role.Id, "c1");
        var catalog = new SourceCodePermissionCatalog(new[] { new PermissionDefinition(permission, permission, null, null, PermissionStatus.Active, true) });
        return (user, role, catalog);
    }

    private static CurrentStatePermissionAuthorizationHandler CreateSut(UserEntity user, IPermissionCatalog catalog, params RoleEntity[] roles)
    {
        var users = MockUsers(user);
        var roleRepo = new Mock<IRoleRepository>();
        foreach (var role in roles)
        {
            roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
            roleRepo.Setup(r => r.GetIdByNameAsync(role.Name, It.IsAny<CancellationToken>())).ReturnsAsync(role.Id);
        }
        var resolver = new EffectivePermissionResolver(users.Object, roleRepo.Object, catalog);
        return new CurrentStatePermissionAuthorizationHandler(users.Object, roleRepo.Object, resolver);
    }

    private static Mock<IUserRepository> MockUsers(UserEntity user)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return users;
    }

    private static AuthorizationHandlerContext CreateContext(UserId userId, string permission, params string[] roleClaims)
    {
        var claims = new List<Claim> { new("sub", userId.Value.ToString()) };
        claims.AddRange(roleClaims.Select(r => new Claim(SecurityService.RoleClaimType, r)));
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(permission);
        return new AuthorizationHandlerContext(new[] { requirement }, principal, null);
    }
}
