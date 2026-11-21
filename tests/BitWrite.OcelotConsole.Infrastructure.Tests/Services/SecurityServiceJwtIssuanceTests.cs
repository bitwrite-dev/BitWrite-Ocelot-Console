using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BitWrite.OcelotConsole.Application.Authorization;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Infrastructure.Tests.Services;

public class SecurityServiceJwtIssuanceTests
{
    [Fact]
    public async Task GenerateAccessToken_ActiveUserActiveAssignedRoleWithActivePermission_EmitsRoleNameClaimOnly()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");

        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);

        var jwt = Read(token);
        jwt.Claims.Where(c => c.Type == SecurityService.RoleClaimType).Select(c => c.Value)
            .Should().ContainSingle().Which.Should().Be("operator");
        jwt.Claims.Should().NotContain(c => c.Type == "permission" || c.Type == "permissions" || c.Type == "roleId" || c.Type == "roleIds");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.Value.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        jwt.Claims.Should().Contain(c => c.Type == AuthzVersioning.AccessTokenVersionClaimType && c.Value == "0");
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("test-audience");
    }

    [Fact]
    public async Task GenerateAccessToken_DisabledUser_EmitsNoRoleClaims()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");
        user.Disable("admin", "t");

        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        Read(token).Claims.Where(c => c.Type == SecurityService.RoleClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAccessToken_InactiveRoleOrUnassignedRole_NotEmitted()
    {
        var user = ActiveUser();
        var inactive = ActiveRole("inactive-role", "perm.read");
        inactive.Deactivate("admin");
        var unassigned = ActiveRole("other-role", "perm.read");
        user.AssignRole(inactive.Id, "t");

        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, inactive, unassigned);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        Read(token).Claims.Where(c => c.Type == SecurityService.RoleClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAccessToken_RoleWithOnlyRetiredOrUnknownPermissions_NotEmitted()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.retired", "perm.unknown");
        user.AssignRole(role.Id, "t");

        var catalog = new SourceCodePermissionCatalog(new[] { new PermissionDefinition("perm.retired", "R", null, null, PermissionStatus.Retired, true) });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        Read(token).Claims.Where(c => c.Type == SecurityService.RoleClaimType).Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAccessToken_RoleWithMixedPermissions_EmittedWhenAtLeastOneActiveCatalogPermission()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.retired", "perm.unknown", "perm.active");
        user.AssignRole(role.Id, "t");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            new PermissionDefinition("perm.retired", "R", null, null, PermissionStatus.Retired, true),
            ActivePerm("perm.active")
        });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        Read(token).Claims.Where(c => c.Type == SecurityService.RoleClaimType).Select(c => c.Value)
            .Should().ContainSingle().Which.Should().Be("operator");
    }

    [Fact]
    public async Task GenerateAccessToken_MultiRoleEligibility_EmitsOneClaimPerEligibleRoleNameOnly()
    {
        var user = ActiveUser();
        var eligibleA = ActiveRole("role-a", "perm.a");
        var eligibleB = ActiveRole("role-b", "perm.b");
        var ineligible = ActiveRole("role-c", "perm.retired");
        var inactive = ActiveRole("role-d", "perm.a");
        inactive.Deactivate("admin");

        user.AssignRole(eligibleA.Id, "t");
        user.AssignRole(eligibleB.Id, "t");
        user.AssignRole(ineligible.Id, "t");
        user.AssignRole(inactive.Id, "t");

        var catalog = new SourceCodePermissionCatalog(new[]
        {
            ActivePerm("perm.a"),
            ActivePerm("perm.b"),
            new PermissionDefinition("perm.retired", "R", null, null, PermissionStatus.Retired, true)
        });
        var sut = CreateSut(user, catalog, eligibleA, eligibleB, ineligible, inactive);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        var roleClaims = Read(token).Claims.Where(c => c.Type == SecurityService.RoleClaimType).Select(c => c.Value).ToList();

        roleClaims.Should().BeEquivalentTo(new[] { "role-a", "role-b" });
        roleClaims.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GenerateAccessToken_ClaimCountIndependentOfPermissionCount()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", Enumerable.Range(1, 50).Select(i => $"perm.{i}").ToArray());
        user.AssignRole(role.Id, "t");

        var defs = Enumerable.Range(1, 50).Select(i => ActivePerm($"perm.{i}")).ToArray();
        var catalog = new SourceCodePermissionCatalog(defs);
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);
        var claims = Read(token).Claims.ToList();

        claims.Count(c => c.Type == SecurityService.RoleClaimType).Should().Be(1);
        claims.Should().NotContain(c => c.Type.StartsWith("perm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateAccessToken_RoleClaimTypeExplicitAndValidatedConsistently()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");
        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);

        SecurityService.RoleClaimType.Should().Be("role");
        var jwt = Read(token);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "operator");
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear();
        var principal = handler.ValidateToken(token, ValidationParameters(), out _);
        principal.IsInRole("operator").Should().BeTrue();
        principal.FindAll("role").Select(c => c.Value).Should().ContainSingle().Which.Should().Be("operator");
    }

    private static SecurityService CreateSut(User user, IPermissionCatalog catalog, params RoleEntity[] roles)
        => CreateSut(user, catalog, "0", roles);

    private static SecurityService CreateSut(User user, IPermissionCatalog catalog, string authzVersion, params RoleEntity[] roles)
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var roleRepo = new Mock<IRoleRepository>();
        foreach (var role in roles)
            roleRepo.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        var atomic = new Mock<IAtomicOperationService>();
        atomic.Setup(a => a.GetAsync(AuthzVersioning.UserVersionKey(user.Id), It.IsAny<CancellationToken>())).ReturnsAsync(authzVersion);

        return new SecurityService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IRefreshTokenRepository>(),
            Mock.Of<ITokenBlacklistRepository>(),
            userRepo.Object,
            roleRepo.Object,
            catalog,
            Mock.Of<IPasswordHasher>(),
            atomic.Object,
            TestConfiguration());
    }

    private static IConfiguration TestConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:SigningKey"] = "unit-test-signing-key-0123456789-abcdef",
            ["Security:Issuer"] = "test-issuer",
            ["Security:Audience"] = "test-audience",
            ["Security:AccessTokenExpirationMinutes"] = "15",
            ["Security:RefreshTokenExpirationDays"] = "7"
        }).Build();

    private static TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "test-issuer",
        ValidAudience = "test-audience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("unit-test-signing-key-0123456789-abcdef")),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = SecurityService.RoleClaimType
    };

    private static JwtSecurityToken Read(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);
    private static User ActiveUser() => User.CreateLocal(UserId.New(), "alice", "Alice", "alice@example.com", "sys");
    private static PermissionDefinition ActivePerm(string code) => new(code, code, null, null, PermissionStatus.Active, true);

    private static RoleEntity ActiveRole(string name, params string[] permissions)
    {
        var role = RoleEntity.Create(RoleId.New(), name, null, "sys");
        foreach (var p in permissions)
            role.AddPermission(p, "sys");
        return role;
    }

    [Fact]
    public async Task GenerateAccessToken_EmbedsCurrentAuthzVersionBaseline()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");
        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, "7", role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);

        Read(token).Claims.Should().Contain(c => c.Type == AuthzVersioning.AccessTokenVersionClaimType && c.Value == "7");
    }

    [Fact]
    public async Task GenerateAccessToken_WithSessionIdAndAuthMethod_EmitsSidAndAuthMethodClaims()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");
        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id, "session-123", "password");

        var jwt = Read(token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sid && c.Value == "session-123");
        jwt.Claims.Should().Contain(c => c.Type == "auth_method" && c.Value == "password");
    }

    [Fact]
    public async Task GenerateAccessToken_WithoutSessionIdAndAuthMethod_DoesNotEmitSidOrAuthMethod()
    {
        var user = ActiveUser();
        var role = ActiveRole("operator", "perm.read");
        user.AssignRole(role.Id, "t");
        var catalog = new SourceCodePermissionCatalog(new[] { ActivePerm("perm.read") });
        var sut = CreateSut(user, catalog, role);

        var token = await sut.GenerateAccessTokenAsync(user.Id);

        var jwt = Read(token);
        jwt.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Sid);
        jwt.Claims.Should().NotContain(c => c.Type == "auth_method");
    }

}
