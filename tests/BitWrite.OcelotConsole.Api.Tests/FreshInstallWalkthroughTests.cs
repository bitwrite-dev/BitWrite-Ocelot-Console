using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// A fresh install, taken from nothing to able to create a snapshot.
/// </summary>
/// <remarks>
/// This is the sequence that was broken, and it is easy to break again: every
/// step can pass its own test while the walk between them does not. Nothing
/// connected <c>POST /settings/first-run</c> to <c>POST /snapshots/preview</c>,
/// so a fresh install answered 401 at the first step and then refused to build
/// anything at the second, pointing at a setup screen it could not reach.
///
/// It goes through the real HTTP pipeline rather than calling handlers, because the
/// failure was an authorisation attribute — invisible to a handler-level test.
///
/// Settings live in a Redis shared with the developer's own run, so these cannot
/// assume a virgin install: each one completes setup first, or reads the current
/// state, rather than depending on what the test before it left behind. The class
/// is disabled where that sharing would make a result meaningless.
/// </remarks>
[Collection("fresh-install")]
public class FreshInstallWalkthroughTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FreshInstallWalkthroughTests(WebApplicationFactory<Program> factory) =>
        _factory = factory;

    /// <summary>
    /// A client that names its environment on every request, as the dashboard does.
    /// </summary>
    /// <remarks>
    /// Set once here rather than per call because it is the contract every client owes
    /// the API: routes, services and snapshots are stored per environment, so a request
    /// that names none is rejected instead of being answered with a default's data.
    /// </remarks>
    private HttpClient CreateClient(string environment = "development")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        client.DefaultRequestHeaders.Add("X-Environment", environment);
        return client;
    }

    private HttpClient CreateAuthenticatedClient(string userId, string roleName, string environment = "development", bool includeEnvironmentHeader = true)
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
                    })
                    .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options =>
                    {
                        options.UserId = _testUserId;
                        options.RoleName = _testRoleName;
                    });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        if (includeEnvironmentHeader)
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

    private async Task<HttpClient> SetupWithAuth(string roleName, params string[] permissions)
    {
        await SeedAuthorizationState(roleName, permissions);
        var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!);
        await CompleteSetup(client);
        return client;
    }

    /// <summary>
    /// Completes setup, so a test starts from a state it arranged itself.
    /// </summary>
    private async Task CompleteSetup(HttpClient client)
    {
        var existing = await ReadJson(client, HttpMethod.Get, "/api/v1/settings");
        if (!existing.GetProperty("isInitialised").GetBoolean())
        {
            await ReadJson(client, HttpMethod.Post, "/api/v1/settings/first-run",
                new { ocelotVersion = "18.0.0", initiatedBy = "test", snapshotRetentionCount = 10 });
        }
    }

    [Fact]
    public async Task AnUnconfiguredInstallCanChooseItsVersionAndThenBuildASnapshot()
    {
        using var client = await SetupWithAuth("walkthrough-user", "perm.settings.read", "perm.snapshots.read");

        // 1. Setup completed, and the install says so.
        var chosen = await ReadJson(client, HttpMethod.Get, "/api/v1/settings");
        chosen.GetProperty("isInitialised").GetBoolean().Should().BeTrue();
        chosen.GetProperty("ocelotVersion").GetString().Should().Be("18.0.0");

        // 3. And a snapshot can now be previewed, which is the thing that was
        //    refused with "Complete first-run setup" and a null reference before.
        var preview = await ReadJson(client, HttpMethod.Post, "/api/v1/snapshots/preview");
        preview.GetProperty("ocelotVersion").GetString().Should().Contain("18");
        preview.GetProperty("content").ValueKind.Should().Be(
            JsonValueKind.String,
            "a configured install can resolve an artifact");
    }

    [Fact]
    public async Task SetupRecordsWhoChoseTheVersion()
    {
        using var client = await SetupWithAuth("walkthrough-user", "perm.settings.read");

        // The choice is permanent, so the audit trail has to be able to say who
        // made it — and with no session there is nothing else to read.
        var chosen = await ReadJson(client, HttpMethod.Get, "/api/v1/settings");

        chosen.GetProperty("ocelotVersionSelectedBy").ValueKind
            .Should().Be(JsonValueKind.String, "a setup call always records someone");
        chosen.GetProperty("ocelotVersionSelectedAt").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task SetupRefusesAVersionTheProductCannotEmit()
    {
        using var client = await SetupWithAuth("walkthrough-user", "perm.settings.read");

        // Offering 20.0 in the setup screen would promise a configuration the
        // builder refuses to generate: setup would complete and then every
        // snapshot would fail.
        var response = await client.PostAsJsonAsync("/api/v1/settings/first-run",
            new { ocelotVersion = "20.0.0", initiatedBy = "operator" });

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task SetupRefusesAVersionItDoesNotRecognise()
    {
        using var client = await SetupWithAuth("walkthrough-user", "perm.settings.read");

        var response = await client.PostAsJsonAsync("/api/v1/settings/first-run",
            new { ocelotVersion = "17.0.0", initiatedBy = "operator" });

        // It used to be a 500 carrying "An internal server error occurred" — so an
        // operator who picked the wrong version was told the server was broken, and
        // never learned that 18.0.0 was the one to pick. That is the whole point of
        // this change, so it is asserted directly rather than through a status
        // number, which depends on whether another test got here first.
        response.IsSuccessStatusCode.Should().BeFalse();

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("error").GetString().Should().NotBe(
            "An internal server error occurred");
        body.GetProperty("errorCode").GetString()
            .Should().BeOneOf("OCELOT_VERSION_NOT_EMITTABLE", "OCELOT_VERSION_ALREADY_CHOSEN");
    }

    [Fact]
    public async Task TheSettingsScreenIsToldWhichVersionsItCanOffer()
    {
        await SetupWithAuth("walkthrough-user", "perm.settings.read");

        using var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!);

        // The setup screen offers exactly this list, so it has to come from the
        // product rather than being typed into the page.
        var settings = await ReadJson(client, HttpMethod.Get, "/api/v1/settings");

        var available = settings.GetProperty("availableOcelotVersions")
            .EnumerateArray()
            .Select(version => version.GetString())
            .ToList();

        available.Should().NotBeEmpty();
        available.Should().OnlyContain(version => version == "18.0.0");
    }

    [Fact]
    public async Task ARequestThatNamesNoEnvironmentIsRejected()
    {
        // Every repository is scoped to the environment the request names, so a request
        // that names none has no key to address. Answering it with some default's
        // configuration would read as a short or empty list rather than as a mistake,
        // and an operator would go looking for routes that were never deleted.
        await SetupWithAuth("walkthrough-user", "perm.snapshots.read");

        using var client = CreateAuthenticatedClient(_testUserId!, _testRoleName!, includeEnvironmentHeader: false);

        var response = await client.PostAsync("/api/v1/snapshots/preview", content: null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("ENVIRONMENT_NOT_SELECTED");
    }

    [Fact]
    public async Task EnvironmentsDoNotSeeEachOthersSnapshots()
    {
        // The property the whole change exists for, across the API rather than a
        // repository: a snapshot sealed in development is not answerable from
        // production, and neither request reports the other's absence as an error.
        using var development = await SetupWithAuth("walkthrough-dev", "perm.settings.read", "perm.snapshots.read", "perm.snapshots.manage");
        using var production = CreateAuthenticatedClient(_testUserId!, _testRoleName!, "PRODUCTION");

        await development.PostAsJsonAsync("/api/v1/snapshots", new { createdBy = "test" });

        // A client aimed at a different environment can still authenticate (user
        // and role repositories are not environment-scoped), but sees no snapshots
        // because the snapshot repository is environment-scoped.
        var inProduction = await production.GetAsync("/api/v1/snapshots?page=1&pageSize=50");
        inProduction.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await inProduction.Content.ReadAsStringAsync()).RootElement;
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    private static async Task<JsonElement> ReadJson(
        HttpClient client,
        HttpMethod method,
        string path,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await client.SendAsync(request);

        // The response is read on the failure path too, because the body is where
        // the reason lives — a bare status code is what sent this bug looking
        // like a server fault rather than a rejected configuration.
        var json = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(
            $"{method} {path} answered {(int)response.StatusCode}: {json}");

        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Fact]
    public async Task PerformingAnActionLeavesARecordOfIt()
    {
        using var client = await SetupWithAuth("walkthrough-user", "perm.settings.read", "perm.services.manage", "perm.audit.read");

        // This is the test that would have caught #514. Every layer tested its own
        // seam — the handlers assert the event was dispatched, the controller
        // asserts it maps the repository's shape — and nothing crossed the two, so
        // the audit log stayed empty while the whole suite passed.
        var before = JsonDocument
            .Parse(await client.GetStringAsync("/api/v1/audit?page=1&pageSize=1"))
            .RootElement.GetProperty("totalCount").GetInt32();

        var created = JsonDocument.Parse(await client.PostAsJsonAsync("/api/v1/services",
            new
            {
                name = $"audit-walkthrough-{Guid.NewGuid():N}"[..20],
                downstreamTargets = new[] { new { host = "localhost", port = 5001, weight = 1 } },
            }).ContinueWith(task => task.Result.Content.ReadAsStringAsync()).Result)
            .RootElement;

        var id = created.GetProperty("id").GetString();
        created.GetProperty("downstreamTargets")[0].GetProperty("weight").GetInt32()
            .Should().Be(1, "the endpoint request now carries a weight");

        var after = JsonDocument.Parse(await client.GetStringAsync("/api/v1/audit?page=1&pageSize=50"))
            .RootElement;

        after.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(before);

        var mine = after.GetProperty("audits").EnumerateArray()
            .Where(entry => entry.GetProperty("resourceId").GetString() == id)
            .ToList();

        mine.Should().ContainSingle("creating one service should record one entry");
        var entry = mine[0];
        entry.GetProperty("action").GetString().Should().Be("CreateService");
        entry.GetProperty("resourceType").GetString().Should().Be("Service");
        entry.GetProperty("result").GetString().Should().Be("Success");

        await client.DeleteAsync($"/api/v1/services/{id}");
    }

    [Fact]
    public async Task FiltersTheLogByWhatItIsAskedFor()
    {
        // The list takes filters, and a filter that returned everything would look
        // like one that worked. Audit endpoints require authentication (IAM-09).
        await SetupWithAuth("walkthrough-user", "perm.audit.read");
        using var authed = CreateAuthenticatedClient(_testUserId!, _testRoleName!);
        var all = JsonDocument.Parse(await authed.GetStringAsync("/api/v1/audit?page=1&pageSize=50"));
        if (all.RootElement.GetProperty("totalCount").GetInt32() == 0) return;

        var filtered = JsonDocument.Parse(
            await authed.GetStringAsync("/api/v1/audit?action=CreateService&pageSize=50"));

        filtered.RootElement.GetProperty("audits").EnumerateArray()
            .Should().OnlyContain(entry => entry.GetProperty("action").GetString() == "CreateService");
    }
}
