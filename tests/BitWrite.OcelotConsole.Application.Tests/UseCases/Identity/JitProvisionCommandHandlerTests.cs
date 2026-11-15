using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Identity;

public class JitProvisionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingExternalIdentity_ReturnsExistingUserWithoutProvisioning()
    {
        var userId = UserId.New();
        var existingIdentity = ExternalIdentity.Create(userId, "issuer", "subj", "oidc");
        var existingUser = User.CreateExternal(userId, "ext-user", "Ext", "e@example.com", "sys");
        existingUser.AssignRole(RoleId.New(), "c1");

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingUser);
        var extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync("issuer", "subj", It.IsAny<CancellationToken>())).ReturnsAsync(existingIdentity);
        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);
        var dispatcher = new Mock<IDomainEventDispatcher>(MockBehavior.Strict);

        var sut = CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, ValidOptions());

        var result = await sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc"));

        result.IsNewlyCreated.Should().BeFalse();
        result.UserId.Should().Be(userId.Value);
        result.Username.Should().Be("ext-user");
        result.AssignedRoles.Should().BeEquivalentTo(existingUser.RoleIds);
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
        roleRepo.VerifyNoOtherCalls();
        dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ConfiguredActiveDefaultRole_ProvisionsUserAssignsRoleAndLinksIdentity()
    {
        var role = RoleEntity.Create(RoleId.New(), "restricted", null, "admin");
        var options = new JitProvisionOptions { DefaultRoleId = role.Id.Value };

        var userRepo = new Mock<IUserRepository>();
        var extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync("issuer", "subj", It.IsAny<CancellationToken>())).ReturnsAsync((ExternalIdentity?)null);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        User? savedUser = null;
        ExternalIdentity? savedIdentity = null;
        userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => savedUser = u).Returns(Task.CompletedTask);
        extRepo.Setup(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalIdentity, CancellationToken>((e, _) => savedIdentity = e).Returns(Task.CompletedTask);

        var sut = CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, options);

        var result = await sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc", "alice", "Alice", "a@example.com"));

        result.IsNewlyCreated.Should().BeTrue();
        result.Username.Should().Be("alice");
        result.AssignedRoles.Should().ContainSingle().Which.Should().Be(role.Id.Value);

        savedUser.Should().NotBeNull();
        savedUser!.Id.Value.Should().Be(result.UserId);
        savedUser.RoleIds.Should().ContainSingle().Which.Should().Be(role.Id.Value);
        savedUser.UserType.Should().Be("External");
        savedUser.Status.IsActive.Should().BeTrue();

        savedIdentity.Should().NotBeNull();
        savedIdentity!.UserId.Value.Should().Be(result.UserId);
        savedIdentity.Issuer.Should().Be("issuer");
        savedIdentity.Subject.Should().Be("subj");

        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Once);
        dispatcher.Verify(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_DefaultRoleNotConfigured_FailsSafelyWithoutCreatingState()
    {
        var sut = CreateSutForFailure(new JitProvisionOptions { DefaultRoleId = null }, out var userRepo, out var extRepo);

        var act = () => sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "JIT_DEFAULT_ROLE_NOT_CONFIGURED");
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DefaultRoleIdInvalid_FailsSafelyWithoutCreatingState()
    {
        var sut = CreateSutForFailure(new JitProvisionOptions { DefaultRoleId = "not-a-valid-role-id" }, out var userRepo, out var extRepo);

        var act = () => sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "JIT_DEFAULT_ROLE_INVALID");
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DefaultRoleNotFound_FailsSafelyWithoutCreatingState()
    {
        var roleId = RoleId.New();
        var userRepo = new Mock<IUserRepository>();
        var extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExternalIdentity?)null);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        var dispatcher = new Mock<IDomainEventDispatcher>(MockBehavior.Strict);

        var sut = CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, new JitProvisionOptions { DefaultRoleId = roleId.Value });

        var act = () => sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "JIT_DEFAULT_ROLE_NOT_FOUND");
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
        dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_DefaultRoleInactive_FailsSafelyWithoutCreatingState()
    {
        var role = RoleEntity.Create(RoleId.New(), "restricted", null, "admin");
        role.Deactivate("admin");
        var userRepo = new Mock<IUserRepository>();
        var extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExternalIdentity?)null);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>(MockBehavior.Strict);

        var sut = CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, new JitProvisionOptions { DefaultRoleId = role.Id.Value });

        var act = () => sut.HandleAsync(new JitProvisionRequest("issuer", "subj", "oidc"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "JIT_DEFAULT_ROLE_INACTIVE");
        userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        extRepo.Verify(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()), Times.Never);
        dispatcher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_Success_PreservesIssuerSubjectLinking()
    {
        var role = RoleEntity.Create(RoleId.New(), "restricted", null, "admin");
        var options = new JitProvisionOptions { DefaultRoleId = role.Id.Value };

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync("https://idp.example", "sub-123", It.IsAny<CancellationToken>())).ReturnsAsync((ExternalIdentity?)null);

        ExternalIdentity? savedIdentity = null;
        extRepo.Setup(r => r.AddAsync(It.IsAny<ExternalIdentity>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalIdentity, CancellationToken>((e, _) => savedIdentity = e).Returns(Task.CompletedTask);

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DomainEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, options);
        await sut.HandleAsync(new JitProvisionRequest("https://idp.example", "sub-123", "oidc"));

        extRepo.Verify(r => r.GetByIssuerAndSubjectAsync("https://idp.example", "sub-123", It.IsAny<CancellationToken>()), Times.Once);
        savedIdentity.Should().NotBeNull();
        savedIdentity!.Issuer.Should().Be("https://idp.example");
        savedIdentity.Subject.Should().Be("sub-123");
        savedIdentity.ProviderType.Should().Be("oidc");
    }

    private static JitProvisionCommandHandler CreateSut(
        IUserRepository userRepository,
        IExternalIdentityRepository externalIdentityRepository,
        IRoleRepository roleRepository,
        IDomainEventDispatcher dispatcher,
        JitProvisionOptions options)
    {
        return new JitProvisionCommandHandler(userRepository, externalIdentityRepository, roleRepository, dispatcher, options);
    }

    private static JitProvisionOptions ValidOptions()
    {
        return new JitProvisionOptions { DefaultRoleId = RoleId.New().Value };
    }

    private static JitProvisionCommandHandler CreateSutForFailure(
        JitProvisionOptions options,
        out Mock<IUserRepository> userRepo,
        out Mock<IExternalIdentityRepository> extRepo)
    {
        userRepo = new Mock<IUserRepository>();
        extRepo = new Mock<IExternalIdentityRepository>();
        extRepo.Setup(r => r.GetByIssuerAndSubjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((ExternalIdentity?)null);
        var roleRepo = new Mock<IRoleRepository>();
        var dispatcher = new Mock<IDomainEventDispatcher>(MockBehavior.Strict);
        return CreateSut(userRepo.Object, extRepo.Object, roleRepo.Object, dispatcher.Object, options);
    }
}
