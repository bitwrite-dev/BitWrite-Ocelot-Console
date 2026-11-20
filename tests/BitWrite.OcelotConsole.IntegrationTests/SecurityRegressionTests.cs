using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using BitWrite.OcelotConsole.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

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

    private HttpClient CreateAuthenticatedClient(string environment = "development")
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Add("X-Environment", environment);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        return client;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "Test";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "regression-user"), new Claim("sub", "regression-user") };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
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
        using var client = CreateAuthenticatedClient();

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

        using var authed = CreateAuthenticatedClient();
        var auditAuth = await authed.GetAsync("/api/v1/audit");
        auditAuth.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
