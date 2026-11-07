using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Moq;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.Tests.UseCases.Identity;

public class SessionRefreshTokenManagementTests
{
    [Fact]
    public async Task RotateRefreshToken_WhenNoActiveSession_AddsNewSession()
    {
        var userId = UserId.New();
        var oldToken = CreateRefreshToken(userId, "old-hash");
        var newToken = CreateRefreshToken(userId, "new-hash");

        var security = new Mock<ISecurityService>();
        security.Setup(s => s.ComputeSha256Hash("raw-old")).Returns("old-hash");
        security.Setup(s => s.ValidateRefreshTokenAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(userId);
        security.Setup(s => s.IssueRefreshTokenAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(("raw-new", newToken));

        var refreshRepo = new Mock<IRefreshTokenRepository>();
        refreshRepo.Setup(r => r.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(oldToken);

        Session? addedSession = null;
        var sessionRepo = new Mock<ISessionRepository>();
        sessionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session>());
        sessionRepo.Setup(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()))
            .Callback<Session, CancellationToken>((s, _) => addedSession = s)
            .Returns(Task.CompletedTask);

        var sut = new RotateRefreshTokenCommandHandler(security.Object, refreshRepo.Object, sessionRepo.Object);

        var result = await sut.HandleAsync(new RotateRefreshTokenCommand("raw-old", "corr"));

        result.NewRefreshToken.Should().Be("raw-new");
        result.RefreshTokenExpiresAt.Should().Be(newToken.ExpiresAt);
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenId.Should().Be(newToken.Id.Value.ToString());
        addedSession.Should().NotBeNull();
        addedSession!.UserId.Should().Be(userId);
        addedSession.RefreshTokenId.Should().Be(newToken.Id.Value.ToString());
        addedSession.IsRevoked.Should().BeFalse();
        sessionRepo.Verify(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.UpdateAsync(oldToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAccessToken_RotatesRefreshTokenAndResolvesRoles()
    {
        var userId = UserId.New();
        var oldToken = CreateRefreshToken(userId, "old-hash");
        var newToken = CreateRefreshToken(userId, "new-hash");
        var session = Session.Create(userId, DateTimeOffset.UtcNow.AddHours(1));
        var user = CreateUser(userId, "admin");

        var security = new Mock<ISecurityService>();
        security.Setup(s => s.ComputeSha256Hash("raw-old")).Returns("old-hash");
        security.Setup(s => s.ValidateRefreshTokenAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(userId);
        security.Setup(s => s.IssueRefreshTokenAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(("raw-new", newToken));
        security.Setup(s => s.GenerateAccessTokenAsync(userId, It.IsAny<IList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        var refreshRepo = new Mock<IRefreshTokenRepository>();
        refreshRepo.Setup(r => r.GetByTokenHashAsync("old-hash", It.IsAny<CancellationToken>())).ReturnsAsync(oldToken);

        var sessionRepo = new Mock<ISessionRepository>();
        sessionRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Session> { session });

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(r => r.GetByNameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((RoleEntity?)null);

        var sut = new RefreshAccessTokenCommandHandler(security.Object, refreshRepo.Object, sessionRepo.Object, userRepo.Object, roleRepo.Object);

        var result = await sut.HandleAsync(new RefreshAccessTokenCommand("raw-old", "corr"));

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-new");
        result.RefreshTokenExpiresAt.Should().Be(newToken.ExpiresAt);
        oldToken.IsRevoked.Should().BeTrue();
        session.RefreshTokenId.Should().Be(newToken.Id.Value.ToString());
        security.Verify(s => s.GenerateAccessTokenAsync(userId, It.Is<IList<string>>(r => r.Contains("admin")), It.IsAny<CancellationToken>()), Times.Once);
        refreshRepo.Verify(r => r.UpdateAsync(oldToken, It.IsAny<CancellationToken>()), Times.Once);
        sessionRepo.Verify(r => r.UpdateAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        sessionRepo.Verify(r => r.AddAsync(It.IsAny<Session>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeRefreshToken_BlacklistsUsingTokenExpiry()
    {
        var userId = UserId.New();
        var token = CreateRefreshToken(userId, "hash");

        var security = new Mock<ISecurityService>();
        security.Setup(s => s.ComputeSha256Hash("raw")).Returns("hash");

        var refreshRepo = new Mock<IRefreshTokenRepository>();
        refreshRepo.Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var sut = new RevokeRefreshTokenCommandHandler(security.Object, refreshRepo.Object);

        var result = await sut.HandleAsync(new RevokeRefreshTokenCommand("raw", "user-1", "logout", "corr"));

        result.Success.Should().BeTrue();
        security.Verify(s => s.RevokeRefreshTokenAsync("hash", "user-1", "logout", It.IsAny<CancellationToken>()), Times.Once);
        security.Verify(s => s.BlacklistTokenAsync("hash", "refresh", token.ExpiresAt, "user-1", "logout", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshToken_WhenAlreadyRevoked_DoesNotBlacklistAgain()
    {
        var userId = UserId.New();
        var token = CreateRefreshToken(userId, "hash");
        token.Revoke("admin", "already");

        var security = new Mock<ISecurityService>();
        security.Setup(s => s.ComputeSha256Hash("raw")).Returns("hash");

        var refreshRepo = new Mock<IRefreshTokenRepository>();
        refreshRepo.Setup(r => r.GetByTokenHashAsync("hash", It.IsAny<CancellationToken>())).ReturnsAsync(token);

        var sut = new RevokeRefreshTokenCommandHandler(security.Object, refreshRepo.Object);

        var result = await sut.HandleAsync(new RevokeRefreshTokenCommand("raw", "user-1", "logout", "corr"));

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already revoked");
        security.Verify(s => s.RevokeRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        security.Verify(s => s.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RefreshToken CreateRefreshToken(UserId userId, string tokenHash) =>
        RefreshToken.Create(userId, tokenHash, DateTimeOffset.UtcNow.AddDays(7));

    private static User CreateUser(UserId userId, string roleName)
    {
        var user = User.CreateLocal(
            userId,
            $"user-{userId.Value:N}",
            "Test User",
            null,
            "test");
        user.AssignRole(roleName, "test");
        user.ClearDomainEvents();
        return user;
    }
}
