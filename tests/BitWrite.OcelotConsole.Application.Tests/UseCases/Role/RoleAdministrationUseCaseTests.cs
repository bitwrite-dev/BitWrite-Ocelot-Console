using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using UserEntity = BitWrite.OcelotConsole.Domain.Aggregates.User.User;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Role;

public class RoleAdministrationUseCaseTests
{
    [Fact]
    public async Task CreateRole_CreatesActiveRole()
    {
        var repo = new Mock<IRoleRepository>();
        RoleEntity? saved = null;
        repo.Setup(r => r.AddAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RoleEntity, CancellationToken>((r, _) => saved = r)
            .Returns(Task.CompletedTask);
        var sut = new CreateRoleCommandHandler(repo.Object);

        var result = await sut.HandleAsync(new CreateRoleCommand("  Operator ", " Desc ", "admin", "c1"));

        saved.Should().NotBeNull();
        saved!.Name.Should().Be("operator");
        saved.Description.Should().Be("Desc");
        saved.IsActive.Should().BeTrue();
        result.Name.Should().Be("operator");
        result.Status.Should().Be("Active");
        result.Id.Should().Be(saved.Id.Value);
        repo.Verify(r => r.AddAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRole_Existing_ReturnsRole()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", "d", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new GetRoleQueryHandler(repo.Object);

        var result = await sut.HandleAsync(new GetRoleQuery(role.Id));

        result.Id.Should().Be(role.Id.Value);
        result.Name.Should().Be("operator");
    }

    [Fact]
    public async Task GetRole_NotFound_Throws()
    {
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<RoleId>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        var sut = new GetRoleQueryHandler(repo.Object);

        var act = () => sut.HandleAsync(new GetRoleQuery(RoleId.New()));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task GetRoles_ReturnsAll()
    {
        var roles = new List<RoleEntity>
        {
            RoleEntity.Create(RoleId.New(), "admin", null, "sys"),
            RoleEntity.Create(RoleId.New(), "operator", null, "sys")
        };
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);
        var sut = new GetRolesQueryHandler(repo.Object);

        var result = await sut.HandleAsync(new GetRolesQuery());

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().BeEquivalentTo("admin", "operator");
    }

    [Fact]
    public async Task UpdateDescription_UpdatesAndPersists()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", "old", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new UpdateRoleDescriptionCommandHandler(repo.Object);

        var result = await sut.HandleAsync(new UpdateRoleDescriptionCommand(role.Id, "  new  ", "editor", "c1"));

        role.Description.Should().Be("new");
        result.Description.Should().Be("new");
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rename_SucceedsWhenNoStoredPermissionRefs()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new RenameRoleCommandHandler(repo.Object);

        var result = await sut.HandleAsync(new RenameRoleCommand(role.Id, "ops", "editor", "c1"));

        role.Name.Should().Be("ops");
        result.Name.Should().Be("ops");
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rename_FailsWhenStoredPermissionRefsExist()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.AddPermission("catalog.read", "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new RenameRoleCommandHandler(repo.Object);

        var act = () => sut.HandleAsync(new RenameRoleCommand(role.Id, "ops", "editor"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_RENAME_NOT_ALLOWED");
        repo.Verify(r => r.UpdateAsync(It.IsAny<RoleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deactivate_UpdatesStatusAndRetainsAssignments()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        user.AssignRole(role.Id, "c1");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new DeactivateRoleCommandHandler(repo.Object);

        var result = await sut.HandleAsync(new DeactivateRoleCommand(role.Id, "admin", "c2"));

        role.IsActive.Should().BeFalse();
        result.Status.Should().Be("Inactive");
        user.RoleIds.Should().Contain(role.Id.Value);
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_ReactivatesInactiveRole()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.Deactivate("admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new ActivateRoleCommandHandler(repo.Object);

        var result = await sut.HandleAsync(new ActivateRoleCommand(role.Id, "admin", "c1"));

        role.IsActive.Should().BeTrue();
        result.Status.Should().Be("Active");
        repo.Verify(r => r.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_AlreadyActive_Throws()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new ActivateRoleCommandHandler(repo.Object);

        var act = () => sut.HandleAsync(new ActivateRoleCommand(role.Id, "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_ALREADY_ACTIVE");
    }

    [Fact]
    public async Task Deactivate_AlreadyInactive_Throws()
    {
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.Deactivate("admin");
        var repo = new Mock<IRoleRepository>();
        repo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new DeactivateRoleCommandHandler(repo.Object);

        var act = () => sut.HandleAsync(new DeactivateRoleCommand(role.Id, "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_ALREADY_INACTIVE");
    }

    [Fact]
    public async Task AssignRole_ActiveRole_AssignsByRoleId()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignRoleCommandHandler(userRepo.Object, roleRepo.Object);

        var result = await sut.HandleAsync(new AssignRoleCommand(user.Id, role.Id, "admin", "c1"));

        user.RoleIds.Should().ContainSingle().Which.Should().Be(role.Id.Value);
        result.RoleId.Should().Be(role.Id.Value);
        result.RoleName.Should().Be("operator");
        userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRole_InactiveRole_ThrowsAndDoesNotAssign()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        role.Deactivate("admin");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var sut = new AssignRoleCommandHandler(userRepo.Object, roleRepo.Object);

        var act = () => sut.HandleAsync(new AssignRoleCommand(user.Id, role.Id, "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_INACTIVE");
        user.RoleIds.Should().BeEmpty();
        userRepo.Verify(r => r.UpdateAsync(It.IsAny<UserEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignRole_RoleNotFound_Throws()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(It.IsAny<RoleId>(), It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);
        var sut = new AssignRoleCommandHandler(userRepo.Object, roleRepo.Object);

        var act = () => sut.HandleAsync(new AssignRoleCommand(user.Id, RoleId.New(), "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_NOT_FOUND");
    }

    [Fact]
    public async Task RemoveRoleAssignment_RemovesByRoleId()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        user.AssignRole(role.Id, "c1");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new RemoveRoleCommandHandler(userRepo.Object, roleRepo.Object, dispatcher.Object);

        var result = await sut.HandleAsync(new RemoveRoleCommand(user.Id, role.Id, "admin", "c2"));

        user.RoleIds.Should().BeEmpty();
        result.RoleId.Should().Be(role.Id.Value);
        userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAssignment_NotAssigned_Throws()
    {
        var user = UserEntity.CreateLocal(UserId.New(), "alice", null, null, "sys");
        var role = RoleEntity.Create(RoleId.New(), "operator", null, "admin");
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var sut = new RemoveRoleCommandHandler(userRepo.Object, roleRepo.Object, dispatcher.Object);

        var act = () => sut.HandleAsync(new RemoveRoleCommand(user.Id, role.Id, "admin"));

        await act.Should().ThrowAsync<DomainException>().Where(e => e.ErrorCode == "ROLE_NOT_ASSIGNED");
    }
}
