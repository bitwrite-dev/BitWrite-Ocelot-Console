using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.EventHandlers;
using FluentAssertions;
using Moq;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using UserEntity = BitWrite.OcelotConsole.Domain.Aggregates.User.User;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Role;

public class IamAuditCoverageTests
{
    [Fact]
    public async Task UserDisabledPropagationHandler_EmitsAuditRecorded()
    {
        var userId = UserId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new UserDisabledPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new UserDisabled(userId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "UserDisabled" && a.ResourceType == "User" &&
            a.ResourceId == userId.Value.ToString() && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleDeactivatedPropagationHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new RoleDeactivatedPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new RoleDeactivated(roleId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "RoleDeactivated" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleActivatedPropagationHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new RoleActivatedPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new RoleActivated(roleId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "RoleActivated" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RoleRenamedPropagationHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new RoleRenamedPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new RoleRenamed(roleId, "old", "new", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "RoleRenamed" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PermissionAddedToRolePropagationHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new PermissionAddedToRolePropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new PermissionAddedToRole(roleId, "perm.read", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "PermissionAddedToRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LocalCredentialChangedPropagationHandler_EmitsAuditRecorded()
    {
        var userId = UserId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new LocalCredentialChangedPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new LocalCredentialChanged(userId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "LocalCredentialChanged" && a.ResourceType == "User" &&
            a.ResourceId == userId.Value.ToString() && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LocalCredentialLockedOutPropagationHandler_EmitsAuditRecorded()
    {
        var userId = UserId.New();
        var propagation = new Mock<IAuthorizationStatePropagationService>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var handler = new LocalCredentialLockedOutPropagationHandler(propagation.Object, dispatcher.Object);
        await handler.HandleAsync(new LocalCredentialLockedOut(userId, DateTimeOffset.UtcNow, "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "system" && a.Action == "LocalCredentialLockedOut" && a.ResourceType == "User" &&
            a.ResourceId == userId.Value.ToString() && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRoleCommandHandler_EmitsAuditRecorded()
    {
        var repo = new Mock<IRoleRepository>();
        RoleEntity? saved = null;
        repo.Setup(r => r.AddAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RoleEntity, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new CreateRoleCommandHandler(repo.Object, dispatcher.Object);
        await sut.HandleAsync(new CreateRoleCommand("operator", "desc", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "CreateRole" && a.ResourceType == "Role" &&
            a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleDescriptionCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new UpdateRoleDescriptionCommandHandler(repo.Object, dispatcher.Object);
        await sut.HandleAsync(new UpdateRoleDescriptionCommand(roleId, "new desc", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "UpdateRoleDescription" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameRoleCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new RenameRoleCommandHandler(repo.Object, dispatcher.Object);
        await sut.HandleAsync(new RenameRoleCommand(roleId, "new-name", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "RenameRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateRoleCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        role.Deactivate("admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new ActivateRoleCommandHandler(repo.Object, dispatcher.Object);
        await sut.HandleAsync(new ActivateRoleCommand(roleId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "ActivateRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateRoleCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new DeactivateRoleCommandHandler(repo.Object, dispatcher.Object);
        await sut.HandleAsync(new DeactivateRoleCommand(roleId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "DeactivateRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRoleCommandHandler_EmitsAuditRecorded()
    {
        var userId = UserId.New();
        var roleId = RoleId.New();
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(UserEntity.CreateLocal(userId, "user", "User", null, "sys"));
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(RoleEntity.Create(roleId, "operator", "desc", "admin"));
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new AssignRoleCommandHandler(userRepo.Object, roleRepo.Object, dispatcher.Object);
        await sut.HandleAsync(new AssignRoleCommand(userId, roleId, "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "AssignRoleToUser" && a.ResourceType == "User" &&
            a.ResourceId == userId.Value.ToString() && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignPermissionToRoleCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var catalog = new SourceCodePermissionCatalog(new[] { new PermissionDefinition("perm.read", "Read", null, null, PermissionStatus.Active, true) });
        var lockRepo = new Mock<IDistributedLock>();
        lockRepo.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockResult(true, "lock-id", null));
        lockRepo.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new AssignPermissionToRoleCommandHandler(roleRepo.Object, catalog, lockRepo.Object, dispatcher.Object);
        await sut.HandleAsync(new AssignPermissionToRoleCommand(roleId, "perm.read", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "AssignPermissionToRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePermissionFromRoleCommandHandler_EmitsAuditRecorded()
    {
        var roleId = RoleId.New();
        var role = RoleEntity.Create(roleId, "operator", "desc", "admin");
        role.AddPermission("perm.read", "admin");
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var lockRepo = new Mock<IDistributedLock>();
        lockRepo.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LockResult(true, "lock-id", null));
        lockRepo.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var dispatched = new List<DomainEvent>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new RemovePermissionFromRoleCommandHandler(roleRepo.Object, lockRepo.Object, dispatcher.Object);
        await sut.HandleAsync(new RemovePermissionFromRoleCommand(roleId, "perm.read", "admin", "c1"), default);

        dispatcher.Verify(d => d.DispatchAsync(It.Is<AuditRecorded>(a =>
            a.Actor == "admin" && a.Action == "RemovePermissionFromRole" && a.ResourceType == "Role" &&
            a.ResourceId == roleId.Value && a.Result == "Success"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
