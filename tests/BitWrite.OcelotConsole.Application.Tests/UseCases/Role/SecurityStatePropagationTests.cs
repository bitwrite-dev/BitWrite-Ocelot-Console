using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.EventHandlers;
using BitWrite.OcelotConsole.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Role;

public class SecurityStatePropagationTests
{
    [Fact]
    public async Task UserDisabled_RevokesSessionsAndInvalidatesUser()
    {
        var userId = UserId.New();
        var security = new Mock<ISecurityService>();
        var atomic = new Mock<IAtomicOperationService>();
        var users = new Mock<IUserRepository>();
        var sut = new AuthorizationStatePropagationService(security.Object, users.Object, atomic.Object, NullLogger<AuthorizationStatePropagationService>.Instance);

        await sut.OnUserDisabledAsync(userId);

        security.Verify(s => s.RevokeAllUserSessionsAsync(userId, "system", "UserDisabled", It.IsAny<CancellationToken>()), Times.Once);
        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:user:{userId.Value}", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleChanged_InvalidatesRoleAndAllAssignedUsers()
    {
        var roleId = RoleId.New();
        var u1 = User.CreateLocal(UserId.New(), "user1", "User 1", "user1@example.com", "sys");
        var u2 = User.CreateLocal(UserId.New(), "user2", "User 2", "user2@example.com", "sys");
        var u3 = User.CreateLocal(UserId.New(), "user3", "User 3", "user3@example.com", "sys");
        u1.AssignRole(roleId, "sys");
        u3.AssignRole(roleId, "sys");

        var security = new Mock<ISecurityService>();
        var atomic = new Mock<IAtomicOperationService>();
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<User> { u1, u2, u3 });
        var sut = new AuthorizationStatePropagationService(security.Object, users.Object, atomic.Object, NullLogger<AuthorizationStatePropagationService>.Instance);

        await sut.OnRoleChangedAsync(roleId, "RoleDeactivated");

        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:role:{roleId.Value}", 1, It.IsAny<CancellationToken>()), Times.Once);
        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:user:{u1.Id.Value}", 1, It.IsAny<CancellationToken>()), Times.Once);
        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:user:{u3.Id.Value}", 1, It.IsAny<CancellationToken>()), Times.Once);
        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:user:{u2.Id.Value}", 1, It.IsAny<CancellationToken>()), Times.Never);
        security.Verify(s => s.RevokeAllUserSessionsAsync(It.IsAny<UserId>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CredentialOrSecurityStateChanged_RevokesSessionsAndInvalidatesUser()
    {
        var userId = UserId.New();
        var security = new Mock<ISecurityService>();
        var atomic = new Mock<IAtomicOperationService>();
        var users = new Mock<IUserRepository>();
        var sut = new AuthorizationStatePropagationService(security.Object, users.Object, atomic.Object, NullLogger<AuthorizationStatePropagationService>.Instance);

        await sut.OnCredentialOrSecurityStateChangedAsync(userId, "LocalCredentialChanged");

        security.Verify(s => s.RevokeAllUserSessionsAsync(userId, "system", "LocalCredentialChanged", It.IsAny<CancellationToken>()), Times.Once);
        atomic.Verify(a => a.IncrementAsync($"ocelot:iam:v1:authz:ver:user:{userId.Value}", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DomainEventHandlers_MapToPropagationActions()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();

        await new UserDisabledPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.UserDisabled(userId, "sys", "c"));
        await new RoleDeactivatedPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.RoleDeactivated(roleId, "sys", "c"));
        await new RoleRenamedPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.RoleRenamed(roleId, "old", "new", "sys", "c"));
        await new PermissionAddedToRolePropagationHandler(propagation.Object).HandleAsync(new Domain.Events.PermissionAddedToRole(roleId, "perm", "sys", "c"));
        await new RoleAssignedToUserPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.RoleAssignedToUser(userId, roleId.Value, "c"));
        await new LocalCredentialChangedPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.LocalCredentialChanged(userId, "sys", "c"));
        await new LocalCredentialLockedOutPropagationHandler(propagation.Object).HandleAsync(new Domain.Events.LocalCredentialLockedOut(userId, DateTimeOffset.UtcNow, "c"));

        propagation.Verify(p => p.OnUserDisabledAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnRoleChangedAsync(roleId, "RoleDeactivated", It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnRoleChangedAsync(roleId, "RoleRenamed", It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnRoleChangedAsync(roleId, "PermissionAddedToRole", It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnUserRoleAssignmentChangedAsync(userId, roleId, "RoleAssignedToUser", It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnCredentialOrSecurityStateChangedAsync(userId, "LocalCredentialChanged", It.IsAny<CancellationToken>()), Times.Once);
        propagation.Verify(p => p.OnCredentialOrSecurityStateChangedAsync(userId, "LocalCredentialLockedOut", It.IsAny<CancellationToken>()), Times.Once);
    }
}
