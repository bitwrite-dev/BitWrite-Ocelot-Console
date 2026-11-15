using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Role;

public class RolePermissionManagementTests
{
    [Fact]
    public async Task Assign_ActiveAssignablePermission_AddsAndPersists()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.ClearDomainEvents();
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, true));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var result = await sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "  CATALOG.READ ", "admin", "c1"));

        result.Assigned.Should().BeTrue();
        result.AlreadyAssigned.Should().BeFalse();
        result.PermissionCode.Should().Be("catalog.read");
        role.Permissions.Should().ContainSingle().Which.Should().Be("catalog.read");
        role.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PermissionAddedToRole>();
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Assign_AlreadyAssignedPermission_IsIdempotentNoOp()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        role.ClearDomainEvents();
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, true));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var result = await sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "catalog.read", "admin", "c1"));

        result.Assigned.Should().BeFalse();
        result.AlreadyAssigned.Should().BeTrue();
        role.Permissions.Should().ContainSingle().Which.Should().Be("catalog.read");
        role.DomainEvents.Should().BeEmpty();
        repo.Verify(r => r.UpdateAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_UnknownPermission_ThrowsAndDoesNotMutate()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var catalog = Catalog();
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var act = () => sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "unknown.perm", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "PERMISSION_NOT_FOUND");
        role.Permissions.Should().BeEmpty();
        repo.Verify(r => r.UpdateAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_RetiredPermission_ThrowsNotAssignable()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Retired, true));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var act = () => sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "catalog.read", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "PERMISSION_NOT_ASSIGNABLE");
        role.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Assign_ActiveNotAssignablePermission_ThrowsNotAssignable()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, false));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var act = () => sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "catalog.read", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "PERMISSION_NOT_ASSIGNABLE");
    }

    [Fact]
    public async Task Assign_RoleNotFound_Throws()
    {
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, true));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RoleId>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new AlwaysLock());

        var act = () => sut.HandleAsync(new AssignPermissionToRoleCommand(RoleId.New(), "catalog.read", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task Assign_LockNotAcquired_Throws()
    {
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, true));
        var repo = new Mock<IRoleRepository>(MockBehavior.Strict);
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new FailingLock());

        var act = () => sut.HandleAsync(new AssignPermissionToRoleCommand(RoleId.New(), "catalog.read", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_PERMISSION_LOCK_FAILED");
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Remove_ExistingStoredPermission_RemovesAndPersists()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        role.ClearDomainEvents();
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new RemovePermissionFromRoleCommandHandler(repo.Object, new AlwaysLock());

        var result = await sut.HandleAsync(new RemovePermissionFromRoleCommand(role.Id, " CATALOG.READ ", "admin", "c1"));

        result.PermissionCode.Should().Be("catalog.read");
        role.Permissions.Should().BeEmpty();
        role.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PermissionRemovedFromRole>();
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_NotAssigned_ThrowsDomainInvariant()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new RemovePermissionFromRoleCommandHandler(repo.Object, new AlwaysLock());

        var act = () => sut.HandleAsync(new RemovePermissionFromRoleCommand(role.Id, "catalog.read", "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "PERMISSION_NOT_ASSIGNED");
        repo.Verify(r => r.UpdateAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_ConcurrentReassignments_DoNotCorruptState()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var catalog = Catalog(new PermissionDefinition("catalog.read", "Catalog Read", null, "Catalog", PermissionStatus.Active, true));
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        repo.Setup(r => r.UpdateAsync(role, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var gate = new AsyncGate();
        var sut = new AssignPermissionToRoleCommandHandler(repo.Object, catalog, new GateLock(gate));

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => sut.HandleAsync(new AssignPermissionToRoleCommand(role.Id, "catalog.read", "admin")));
        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Assigned).Should().Be(1);
        results.Count(r => r.AlreadyAssigned).Should().Be(4);
        role.Permissions.Should().ContainSingle().Which.Should().Be("catalog.read");
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IPermissionCatalog Catalog(params PermissionDefinition[] definitions) =>
        new SourceCodePermissionCatalog(definitions);

    private sealed class AlwaysLock : IDistributedLock
    {
        public Task<LockResult> AcquireAsync(string resource, TimeSpan expiry, TimeSpan? waitTimeout = null, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new LockResult(true, "lock", null));

        public Task ReleaseAsync(string resource, string lockId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FailingLock : IDistributedLock
    {
        public Task<LockResult> AcquireAsync(string resource, TimeSpan expiry, TimeSpan? waitTimeout = null, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new LockResult(false, null, "timeout"));

        public Task ReleaseAsync(string resource, string lockId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class GateLock : IDistributedLock
    {
        private readonly AsyncGate _gate;
        public GateLock(AsyncGate gate) => _gate = gate;

        public async Task<LockResult> AcquireAsync(string resource, TimeSpan expiry, TimeSpan? waitTimeout = null, TimeSpan? retryInterval = null, CancellationToken cancellationToken = default)
        {
            await _gate.EnterAsync(cancellationToken);
            return new LockResult(true, Guid.NewGuid().ToString("N"), null);
        }

        public Task ReleaseAsync(string resource, string lockId, CancellationToken cancellationToken = default)
        {
            _gate.Exit();
            return Task.CompletedTask;
        }
    }

    private sealed class AsyncGate
    {
        private readonly SemaphoreSlim _sem = new(1, 1);
        public Task EnterAsync(CancellationToken ct) => _sem.WaitAsync(ct);
        public void Exit() => _sem.Release();
    }
}
