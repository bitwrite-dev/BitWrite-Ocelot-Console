using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity.Bootstrap;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Identity.Bootstrap;

public class BootstrapInitialAdminCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenLockUnavailable_ReturnsInProgress()
    {
        var sut = CreateSut(
            options: EnabledOptions(),
            lockResult: new LockResult(false, null, "Lock acquisition timeout"));

        var result = await sut.HandleAsync(new BootstrapInitialAdminCommand("test"));

        result.Completed.Should().BeFalse();
        result.Created.Should().BeFalse();
        result.Message.Should().Contain("in progress");
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyCompleted_IsIdempotent()
    {
        var atomic = new Mock<IAtomicOperationService>();
        atomic.Setup(a => a.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("2026-01-01|done");
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var sut = CreateSut(options: EnabledOptions(), atomic: atomic, users: users);

        var result = await sut.HandleAsync(new BootstrapInitialAdminCommand("test"));

        result.Completed.Should().BeTrue();
        result.Created.Should().BeFalse();
        users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenDisabled_MarksCompletedWithoutCreatingUser()
    {
        var atomic = new Mock<IAtomicOperationService>();
        atomic.Setup(a => a.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        var audit = new List<AuditRecorded>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => { if (e is AuditRecorded a) audit.Add(a); })
            .Returns(Task.CompletedTask);
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DomainEvent>, CancellationToken>((events, _) => audit.AddRange(events.OfType<AuditRecorded>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(options: new BootstrapInitialAdminOptions { Enabled = false }, atomic: atomic, users: users, dispatcher: dispatcher);

        var result = await sut.HandleAsync(new BootstrapInitialAdminCommand("test"));

        result.Completed.Should().BeTrue();
        result.Created.Should().BeFalse();
        atomic.Verify(a => a.SetIfNotExistsAsync(It.IsAny<string>(), It.Is<string>(v => v.Contains("|disabled")), null, It.IsAny<CancellationToken>()), Times.Once);
        audit.Should().ContainSingle(a => a.Action == "BootstrapInitialAdmin" && a.Result == "Skipped");
        users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenEnabled_CreatesAdminUserRoleCredentialAndAuditsWithoutSecrets()
    {
        var atomic = new Mock<IAtomicOperationService>();
        atomic.Setup(a => a.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByUsernameAsync("initialadmin", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        User? createdUser = null;
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Callback<User, CancellationToken>((u, _) => createdUser = u).Returns(Task.CompletedTask);

        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByNameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        roles.Setup(r => r.AddAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var creds = new Mock<ILocalCredentialRepository>();
        LocalCredential? createdCred = null;
        creds.Setup(c => c.GetByUserIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>())).ReturnsAsync((LocalCredential?)null);
        creds.Setup(c => c.AddAsync(It.IsAny<LocalCredential>(), It.IsAny<CancellationToken>())).Callback<LocalCredential, CancellationToken>((c, _) => createdCred = c).Returns(Task.CompletedTask);

        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.HashPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("salt:hash");

        var audit = new List<AuditRecorded>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => { if (e is AuditRecorded a) audit.Add(a); })
            .Returns(Task.CompletedTask);
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DomainEvent>, CancellationToken>((events, _) => audit.AddRange(events.OfType<AuditRecorded>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(options: EnabledOptions(), atomic: atomic, users: users, roles: roles, creds: creds, hasher: hasher, dispatcher: dispatcher);

        var result = await sut.HandleAsync(new BootstrapInitialAdminCommand("bootstrap:test", "corr-1"));

        result.Completed.Should().BeTrue();
        result.Created.Should().BeTrue();
        result.Username.Should().Be("initialadmin");
        createdUser.Should().NotBeNull();
        createdUser!.RoleIds.Should().Contain("admin");
        createdCred.Should().NotBeNull();
        createdCred!.MustChangePassword.Should().BeTrue();
        audit.Should().ContainSingle(a => a.Action == "BootstrapInitialAdmin" && a.Result == "Success");
        audit.Select(a => a.Result).Should().NotContain(r => r.Contains("Sup3r!Secret!Password"));
        atomic.Verify(a => a.SetIfNotExistsAsync(It.IsAny<string>(), It.Is<string>(v => !v.Contains("Sup3r!Secret!Password")), null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WhenUsernameAlreadyExists_DoesNotCreateDuplicateAndCompletes()
    {
        var atomic = new Mock<IAtomicOperationService>();
        atomic.Setup(a => a.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var existing = User.CreateLocal(UserId.New(), "initialadmin", "Admin", null, "test");
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByUsernameAsync("initialadmin", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        users.Setup(u => u.UpdateAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var roles = new Mock<IRoleRepository>();
        roles.Setup(r => r.GetByNameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(RoleEntity.Create(RoleId.From("admin"), "admin", null, "test"));
        var creds = new Mock<ILocalCredentialRepository>();
        creds.Setup(c => c.GetByUserIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(LocalCredential.Create(existing.Id, "Sup3r!Secret!Password", PasswordHash.From("salt:hash", "PBKDF2-SHA256", 10000)));

        var sut = CreateSut(options: EnabledOptions(), atomic: atomic, users: users, roles: roles, creds: creds);

        var result = await sut.HandleAsync(new BootstrapInitialAdminCommand("test"));

        result.Completed.Should().BeTrue();
        result.Created.Should().BeFalse();
        result.UserId.Should().Be(existing.Id.Value);
        users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        atomic.Verify(a => a.SetIfNotExistsAsync(It.IsAny<string>(), It.Is<string>(v => v.Contains(existing.Id.Value.ToString())), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static BootstrapInitialAdminCommandHandler CreateSut(
        BootstrapInitialAdminOptions options,
        LockResult? lockResult = null,
        Mock<IUserRepository>? users = null,
        Mock<IRoleRepository>? roles = null,
        Mock<ILocalCredentialRepository>? creds = null,
        Mock<IPasswordHasher>? hasher = null,
        Mock<IAtomicOperationService>? atomic = null,
        Mock<IDistributedLock>? distributedLock = null,
        Mock<IDomainEventDispatcher>? dispatcher = null)
    {
        var dispatcherProvided = dispatcher is not null;
        users ??= new Mock<IUserRepository>();
        roles ??= new Mock<IRoleRepository>();
        creds ??= new Mock<ILocalCredentialRepository>();
        hasher ??= new Mock<IPasswordHasher>();
        atomic ??= new Mock<IAtomicOperationService>();
        dispatcher ??= new Mock<IDomainEventDispatcher>();
        distributedLock ??= new Mock<IDistributedLock>();
        distributedLock.Setup(l => l.AcquireAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockResult ?? new LockResult(true, "lock-id", null));
        distributedLock.Setup(l => l.ReleaseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        if (!dispatcherProvided)
        {
            dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dispatcher.Setup(d => d.DispatchAsync(It.IsAny<IEnumerable<DomainEvent>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        return new BootstrapInitialAdminCommandHandler(
            users.Object,
            roles.Object,
            creds.Object,
            hasher.Object,
            atomic.Object,
            distributedLock.Object,
            dispatcher.Object,
            options);
    }

    private static BootstrapInitialAdminOptions EnabledOptions() => new()
    {
        Enabled = true,
        Username = "initialadmin",
        Password = "Sup3r!Secret!Password",
        DisplayName = "Initial Admin",
        RoleName = "admin"
    };
}
