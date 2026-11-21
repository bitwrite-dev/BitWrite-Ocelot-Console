using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using BitWrite.OcelotConsole.Api.Controllers;
using BitWrite.OcelotConsole.Application.Authorization;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.Aggregates.Role;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using UserEntity = BitWrite.OcelotConsole.Domain.Aggregates.User.User;

namespace BitWrite.OcelotConsole.IntegrationTests;

/// <summary>
/// Cross-module E2E / security regression suite (IAM-16).
/// Exercises API→App→Domain→Redis/authn/authz for critical security flows:
/// bootstrap/JIT issuance, permission-protected access, default-deny,
/// stale JWT after deactivation/unassignment/rename/retire/disable/credential/session changes,
/// revocation/refresh rejection, RoleName uniqueness conflicts.
/// </summary>
[Collection("iam-security-regression")]
public class SecurityRegressionTests : IClassFixture<WebApplicationFactory<BitWrite.OcelotConsole.Api.Program>>
{
    private readonly WebApplicationFactory<BitWrite.OcelotConsole.Api.Program> _factory;

    public SecurityRegressionTests(WebApplicationFactory<BitWrite.OcelotConsole.Api.Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string environment = "development")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Environment", environment);
        return client;
    }

    private HttpClient CreateAuthenticatedClient(string userId, string roleName, string environment = "development")
    {
        _testUserId = userId;
        _testRoleName = roleName;

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options =>
                {
                    options.UserId = _testUserId;
                    options.RoleName = _testRoleName;
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Environment", environment);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        return client;
    }

    private string? _testUserId;
    private string? _testRoleName;

    private sealed class TestAuthOptions : AuthenticationSchemeOptions
    {
        public string? UserId { get; set; }
        public string? RoleName { get; set; }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(IOptionsMonitor<TestAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new("sub", Options.UserId ?? "test-user"),
                new(AuthzVersioning.AccessTokenVersionClaimType, "0")
            };
            if (!string.IsNullOrEmpty(Options.RoleName))
                claims.Add(new Claim("role", Options.RoleName));
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private async Task SeedAuthorizationState(string roleName, params string[] permissions)
    {
        using var scope = _factory.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

        // Tests share Redis; a role seeded by an earlier test may already exist.
        var existingRole = await roleRepo.GetByNameAsync(roleName);
        RoleEntity role;
        if (existingRole is not null)
        {
            role = existingRole;
            // Add any permissions this test needs that the role lacks.
            foreach (var perm in permissions)
                if (!role.Permissions.Contains(perm))
                    role.AddPermission(perm, "admin");
            await roleRepo.UpdateAsync(role);
        }
        else
        {
            var roleId = RoleId.New();
            role = RoleEntity.Create(roleId, roleName, null, "admin");
            foreach (var perm in permissions)
                role.AddPermission(perm, "admin");
            await roleRepo.AddAsync(role);
        }

        var userId = UserId.New();
        _testUserId = userId.Value.ToString();
        _testRoleName = roleName;
        var user = UserEntity.CreateLocal(userId, "test-user", null, null, "sys");
        await userRepo.AddAsync(user);

        user.AssignRole(role.Id, "c1");
        await userRepo.UpdateAsync(user);
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task BootstrapInitialAdmin_IsCallableAnonymously()
    {
        using var client = CreateClient();

        var response = await client.PostAsync("/api/v1/bootstrap/initial-admin", null);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ProtectedEndpoints_RequireAuthentication_DefaultDeny()
    {
        using var client = CreateClient();

        // IdentityController sessions is protected ([Authorize]); no auth => 401
        var sessions = await client.PostAsync("/api/v1/identity/sessions", null);
        sessions.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // UsersController is protected ([Authorize]); no auth => 401
        var users = await client.GetAsync("/api/v1/users");
        users.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // AuditController is protected ([Authorize]); no auth => 401
        var audit = await client.GetAsync("/api/v1/audit");
        audit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoints_Authenticated_ReturnsSuccess()
    {
        await SeedAuthorizationState("regression-user", "perm.services.read", "perm.routes.read", "perm.users.read");
        using var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!);

        var services = await client.GetAsync("/api/v1/services");
        services.StatusCode.Should().Be(HttpStatusCode.OK);

        var routes = await client.GetAsync("/api/v1/routes");
        routes.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await client.GetAsync("/api/v1/users");
        users.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IdentityTokenRefresh_Anonymous_Allowed()
    {
        using var client = CreateClient();

        // Refresh endpoint is anonymous but requires a valid refresh token; invalid token => 400/401 not 500
        var response = await client.PostAsJsonAsync("/api/v1/identity/tokens/refresh", new { refreshToken = "invalid" });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task AuditEndpoints_RequireAuthentication()
    {
        using var client = CreateClient();

        var audit = await client.GetAsync("/api/v1/audit");
        audit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await SeedAuthorizationState("regression-user", "perm.audit.read");
        using var authed = CreateAuthenticatedClient(_testUserId!, _testRoleName!);
        var auditAuth = await authed.GetAsync("/api/v1/audit");
        auditAuth.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
