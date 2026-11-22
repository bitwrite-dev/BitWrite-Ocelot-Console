using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using Xunit;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Identity;

public class UserAdministrationUseCaseTests
{
    [Fact]
    public async Task CreateUser_Succeeds_PersistsAndAudits()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var credentialRepo = new Mock<ILocalCredentialRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var sut = new CreateUserCommandHandler(repo.Object, credentialRepo.Object, passwordHasher.Object, dispatcher.Object);

        var result = await sut.HandleAsync(new CreateUserCommand("alice", "Alice", "alice@example.com", null, "admin", "c1"));

        result.Username.Should().Be("alice");
        result.Status.Should().Be("Active");
        repo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        dispatcher.Verify(d => d.DispatchAsync(It.IsAny<AuditRecorded>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_RejectsDuplicateUsername()
    {
        var existing = User.CreateLocal(UserId.New(), "alice", "Alice", null, "sys");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var credentialRepo = new Mock<ILocalCredentialRepository>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var sut = new CreateUserCommandHandler(repo.Object, credentialRepo.Object, passwordHasher.Object, dispatcher.Object);

        var act = () => sut.HandleAsync(new CreateUserCommand("alice", null, null, null, "admin", "c1"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "USERNAME_ALREADY_TAKEN");
    }

    [Fact]
    public async Task DisableUser_DispatchesDomainEventsForPropagationAndAudits()
    {
        var user = User.CreateLocal(UserId.New(), "alice", "Alice", null, "sys");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var dispatched = new List<DomainEvent>();
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<DomainEvent, CancellationToken>((e, _) => dispatched.Add(e))
            .Returns(Task.CompletedTask);

        var sut = new DisableUserCommandHandler(repo.Object, dispatcher.Object);
        var result = await sut.HandleAsync(new DisableUserCommand(user.Id, "admin", "c1"));

        result.Status.Should().Be("Disabled");
        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        dispatched.OfType<UserDisabled>().Should().ContainSingle(e => e.UserId == user.Id);
        dispatched.OfType<AuditRecorded>().Should().ContainSingle(ar => ar.Action == "DisableUser" && ar.ResourceId == user.Id.Value.ToString());
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EnableUser_Succeeds_UpdatesStateAndAudits()
    {
        var user = User.CreateLocal(UserId.New(), "alice", "Alice", null, "sys");
        user.Disable("sys", "c");
        user.ClearDomainEvents();
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new EnableUserCommandHandler(repo.Object, dispatcher.Object);

        var result = await sut.HandleAsync(new EnableUserCommand(user.Id, "admin", "c1"));

        result.Status.Should().Be("Active");
        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        dispatcher.Verify(d => d.DispatchAsync(It.IsAny<AuditRecorded>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_Succeeds_UpdatesProfileAndAudits()
    {
        var user = User.CreateLocal(UserId.New(), "alice", "Alice", "old@example.com", "sys");
        user.ClearDomainEvents();
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new UpdateUserCommandHandler(repo.Object, dispatcher.Object);

        var result = await sut.HandleAsync(new UpdateUserCommand(user.Id, "Alice B", "new@example.com", "admin", "c1"));

        result.DisplayName.Should().Be("Alice B");
        result.Email.Should().Be("new@example.com");
        repo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        dispatcher.Verify(d => d.DispatchAsync(It.IsAny<AuditRecorded>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_ReturnsUser_WhenExists()
    {
        var user = User.CreateLocal(UserId.New(), "alice", "Alice", null, "sys");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var sut = new GetUserQueryHandler(repo.Object);

        var result = await sut.HandleAsync(new GetUserQuery(user.Id));

        result.Should().NotBeNull();
        result!.Username.Should().Be("alice");
    }

    [Fact]
    public async Task ListUsers_ReturnsAllUsers()
    {
        var u1 = User.CreateLocal(UserId.New(), "alice", "Alice", null, "sys");
        var u2 = User.CreateLocal(UserId.New(), "bob", "Bob", null, "sys");
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<User> { u1, u2 });
        var sut = new ListUsersQueryHandler(repo.Object);

        var result = await sut.HandleAsync(new ListUsersQuery());

        result.Users.Should().HaveCount(2);
        result.Users.Select(u => u.Username).Should().BeEquivalentTo(new[] { "alice", "bob" });
    }
}
