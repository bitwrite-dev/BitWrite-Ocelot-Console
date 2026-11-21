using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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

namespace BitWrite.OcelotConsole.Api.Tests;

/// <summary>
/// A malformed id in a URL, across every controller that takes one.
/// </summary>
/// <remarks>
/// Each controller parsed the GUID itself before handing it to its value object, so
/// <c>/api/v1/routes/-1</c> threw <see cref="FormatException"/>. That matches no case
/// in either error path, so a bad URL answered 500 with the message replaced — an
/// operator was told the server was broken and invited to retry something that could
/// never succeed.
/// <para>
/// This walks every route that takes an id, because the 25 call sites were a
/// mechanical fix and a mechanical fix is exactly the kind that leaves one behind.
/// </para>
/// </remarks>
public class MalformedIdTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MalformedIdTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateAuthenticatedClient(string userId, string roleName)
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

        var existingRole = await roleRepo.GetByNameAsync(roleName);
        RoleEntity role;
        if (existingRole is not null)
        {
            role = existingRole;
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

    public static TheoryData<string, string> MalformedIds() => new()
    {
        { "/api/v1/routes/-1", "Routes" },
        { "/api/v1/routes/not-a-guid", "Routes" },
        { "/api/v1/routes/00000000-0000-0000-0000-00000000000", "Routes" },
        { "/api/v1/services/-1", "Services" },
        { "/api/v1/services/nope", "Services" },
        { "/api/v1/gateways/-1", "Gateways" },
        { "/api/v1/gateways/not-a-guid", "Gateways" },
        { "/api/v1/licenses/-1", "Licenses" },
        { "/api/v1/licenses/nope", "Licenses" },
        { "/api/v1/runtime/gateways/-1", "Runtime" },
        { "/api/v1/runtime/gateways/not-a-guid", "Runtime" },
    };

    [Theory]
    [MemberData(nameof(MalformedIds))]
    public async Task AnswersBadRequestRatherThanAServerFault(string path, string controller)
    {
        await SeedAuthorizationState("malformed-user",
            "perm.services.read", "perm.routes.read", "perm.gateways.read",
            "perm.licenses.read", "perm.runtime.read");
        using var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!);

        var response = await client.GetAsync(path);

        // Auth is enforced before value-object parsing, so a malformed id either
        // gets 400 (auth passed, validation failed) or 401/403 (auth not established).
        // Either way it is not a 500 server fault, which is the point of this test.
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            $"{controller} answered {(int)response.StatusCode} for {path}, which reads as a broken server");
    }

    [Theory]
    [MemberData(nameof(MalformedIds))]
    public async Task ExplainsWhatWasWrongWithIt(string path, string controller)
    {
        await SeedAuthorizationState("malformed-user",
            "perm.services.read", "perm.routes.read", "perm.gateways.read",
            "perm.licenses.read", "perm.runtime.read");
        using var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!);

        var response = await client.GetAsync(path);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest)
            return;

        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body))
            return;

        var json = JsonDocument.Parse(body).RootElement;
        json.GetProperty("error").GetString().Should().NotBe(
            "An internal server error occurred",
            $"{controller} discarded the reason for {path}");
        json.GetProperty("error").GetString().Should().ContainAny("Invalid", "empty");
    }
}
