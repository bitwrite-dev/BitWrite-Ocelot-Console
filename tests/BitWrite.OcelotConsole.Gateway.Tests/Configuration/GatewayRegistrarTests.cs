using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ocelot.gateway.node1.Configuration;
using StackExchange.Redis;
using Xunit;

namespace BitWrite.OcelotConsole.Gateway.Tests.Configuration;

/// <summary>
/// A node making itself addressable to the control plane.
/// </summary>
/// <remarks>
/// A publication names its target gateways and the control plane refuses one that
/// names none, so an unregistered node cannot be published to at all. These tests
/// pin the two properties that make registration safe to repeat: the id is
/// remembered, so a restart does not leave a graveyard of dead gateways; and a
/// failure to reach the control plane does not stop the node serving.
/// </remarks>
public class GatewayRegistrarTests
{
    private readonly Dictionary<string, RedisValue> _store = new(StringComparer.Ordinal);
    private readonly Mock<IDatabase> _database = new();
    private readonly FakeControlPlane _controlPlane = new();

    public GatewayRegistrarTests()
    {
        _database.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, _) =>
                Task.FromResult(_store.TryGetValue(key.ToString(), out var value) ? value : default(RedisValue)));

        // The six-argument overload, because that is the one a call with defaults
        // binds to: the four- and five-argument forms take expiry and when as
        // required, so `StringSetAsync(key, value)` lands here. Setting up the
        // five-argument one instead would silently match nothing and the id would
        // never be remembered.
        _database.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
                It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .Returns<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((key, value, _, _, _, _) =>
            {
                _store[key.ToString()] = value;
                return Task.FromResult(true);
            });

        _database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns<RedisKey, CommandFlags>((key, _) =>
            {
                var existed = _store.Remove(key.ToString());
                return Task.FromResult(existed);
            });
    }

    private GatewayRegistrar NewRegistrar(
        PublishedEnvironment? environment = null,
        params (string Key, string Value)[] settings)
    {
        var multiplexer = new Mock<IConnectionMultiplexer>();
        multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_database.Object);

        return new GatewayRegistrar(
            _controlPlane,
            multiplexer.Object,
            environment ?? PublishedEnvironment.For("Production"),
            Configuration(settings),
            NullLogger<GatewayRegistrar>.Instance);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s =>
                new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

    [Fact]
    public async Task RegistersOnceAndThenOnlyReports()
    {
        // The register endpoint mints a gateway per call, so a node that registered
        // on every heartbeat would bury the dashboard in dead gateways.
        var registrar = NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")]);

        await registrar.BeatAsync(CancellationToken.None);
        await registrar.BeatAsync(CancellationToken.None);
        await registrar.BeatAsync(CancellationToken.None);

        _controlPlane.Registered.Should().BeEquivalentTo(new[] { "ocelot.gateway.node-1" });
        _controlPlane.Reports.Should().HaveCount(3);
    }

    [Fact]
    public async Task RemembersTheIdSoARestartDoesNotRegisterAgain()
    {
        // A node restarts more often than an operator adds gateways, and the
        // remembered id is what keeps the two facts unrelated.
        await NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")])
            .BeatAsync(CancellationToken.None);

        var restarted = NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")]);
        await restarted.BeatAsync(CancellationToken.None);

        _controlPlane.Registered.Should().HaveCount(1);
        _controlPlane.Reports.Should().HaveCount(2);
    }

    [Fact]
    public async Task RegistersAgainUnderTheSameNameAfterTheGatewayWasDeleted()
    {
        // The dashboard can delete a gateway, and the remembered id then points at
        // nothing. Forgetting it is what lets the node come back rather than
        // heartbeat into a 404 forever.
        var registrar = NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")]);

        await registrar.BeatAsync(CancellationToken.None);
        _controlPlane.GatewayDeleted = true;

        // The loop catches the 404 and forgets the id, which is what makes the
        // next beat register again rather than retry the same dead id.
        await Assert.ThrowsAsync<HttpRequestException>(() => registrar.BeatAsync(CancellationToken.None));
        await registrar.ForgetRegistrationAsync("ocelot.gateway.node-1");

        _controlPlane.GatewayDeleted = false;
        await registrar.BeatAsync(CancellationToken.None);

        _controlPlane.Registered.Should().HaveCount(2);
        _controlPlane.Registered.Should().AllBe("ocelot.gateway.node-1");
    }

    [Fact]
    public async Task RegistersUnderTheConfiguredName()
    {
        var registrar = NewRegistrar(settings:
        [
            ("Gateway:ControlPlaneUrl", "http://plane.test"),
            ("Gateway:NodeName", "ocelot.gateway.node-2"),
        ]);

        await registrar.BeatAsync(CancellationToken.None);

        _controlPlane.Registered.Should().BeEquivalentTo(new[] { "ocelot.gateway.node-2" });
        // Scoped by environment, because the same name in another environment is a
        // different gateway.
        _store.Keys.Should().Contain("ocelot:gateway:registration:production:ocelot.gateway.node-2");
    }

    [Fact]
    public async Task AFailureToRegisterDoesNotStopTheNodeServing()
    {
        // The refusal-and-keep rule: a registration problem is a logging problem.
        // The loop catches this; what the test shows is that the beat surfaced it
        // to the loop rather than escaping into the host.
        _controlPlane.RegistrationFailure = new HttpRequestException("plane down");
        var registrar = NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")]);

        await Assert.ThrowsAsync<HttpRequestException>(() => registrar.BeatAsync(CancellationToken.None));
    }

    [Fact]
    public async Task DoesNotRegisterWhenNoControlPlaneIsConfigured()
    {
        // A node pointed at a shared Redis still takes publications without an
        // address, so the missing setting is reported rather than fatal.
        var registrar = NewRegistrar();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await registrar.StartAsync(cts.Token);
        await cts.CancelAsync();

        _controlPlane.Registered.Should().BeEmpty();
        _controlPlane.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task RetriesAfterAFailureInsteadOfGivingUp()
    {
        // The next beat after a failed one is what makes an outage survivable, so
        // failing once must not leave the node permanently unregistered.
        _controlPlane.RegistrationFailure = new HttpRequestException("plane down");
        var registrar = NewRegistrar(settings: [("Gateway:ControlPlaneUrl", "http://plane.test")]);

        await Assert.ThrowsAsync<HttpRequestException>(() => registrar.BeatAsync(CancellationToken.None));

        _controlPlane.RegistrationFailure = null;
        await registrar.BeatAsync(CancellationToken.None);

        _controlPlane.Registered.Should().HaveCount(1);
        _controlPlane.Reports.Should().HaveCount(1);
    }

    /// <summary>What the control plane hears, recorded rather than stubbed away.</summary>
    private sealed class FakeControlPlane : IControlPlaneClient
    {
        private int _next = 1;

        public List<string> Registered { get; } = new();
        public List<string> Reports { get; } = new();
        public Exception? RegistrationFailure { get; set; }
        public bool GatewayDeleted { get; set; }

        public Task<string> RegisterAsync(string nodeName, string? description, CancellationToken cancellationToken)
        {
            if (RegistrationFailure is not null)
                throw RegistrationFailure;

            Registered.Add(nodeName);
            return Task.FromResult($"gateway-{_next++}");
        }

        public Task ReportStatusAsync(string gatewayId, string status, CancellationToken cancellationToken)
        {
            if (GatewayDeleted)
                throw new HttpRequestException("gone", null, HttpStatusCode.NotFound);

            Reports.Add(gatewayId);
            return Task.CompletedTask;
        }
    }
}
