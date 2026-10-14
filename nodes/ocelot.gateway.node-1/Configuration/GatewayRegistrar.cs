using System.Net;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ocelot.gateway.node1.Configuration;

/// <summary>
/// Registers this node with the control plane, and keeps the registration alive.
/// </summary>
/// <remarks>
/// Why this exists at all: a publication names the gateways it is delivered to, and
/// the control plane refuses one that names none. A node nobody registered cannot be
/// published to, however many containers are running — the fleet would have no
/// members and publishing would be permanently refused.
///
/// The assigned id is remembered in Redis, keyed by the node's name, because the
/// register endpoint mints a gateway per call. A node that re-registered on every
/// restart would leave a graveyard of dead gateways behind it and the dashboard
/// would list containers that no longer exist. The name is the identity, and the
/// id is what it points at. If the gateway is deleted from the dashboard, the
/// heartbeat's 404 forgets the id and the node registers again under its name.
///
/// Everything here is best-effort and none of it gates startup. A node whose control
/// plane is unreachable still serves the configuration it already has — the same
/// refuse-and-keep rule the configuration subscriber follows — and the next tick
/// tries again. A registration problem is a logging problem, not a routing one.
/// </remarks>
public sealed class GatewayRegistrar : BackgroundService
{
    /// <summary>How often the node reports that it is still here.</summary>
    private const int DefaultHeartbeatSeconds = 30;

    private readonly IControlPlaneClient _controlPlane;
    private readonly IDatabase _database;
    private readonly PublishedEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GatewayRegistrar> _logger;

    public GatewayRegistrar(
        IControlPlaneClient controlPlane,
        IConnectionMultiplexer redis,
        PublishedEnvironment environment,
        IConfiguration configuration,
        ILogger<GatewayRegistrar> logger)
    {
        _controlPlane = controlPlane;
        _database = redis.GetDatabase();
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var controlPlaneUrl = _configuration[$"{PublishedEnvironment.SectionName}:ControlPlaneUrl"];

        if (string.IsNullOrWhiteSpace(controlPlaneUrl))
        {
            // Not an error: a node pointed at a Redis it shares with the control
            // plane still takes publications, it simply cannot be addressed by
            // name. Saying so beats failing to start over a setting the operator
            // may not need.
            _logger.LogWarning(
                "No control plane URL is configured, so this node will not register and cannot be published to. Set {Section}:ControlPlaneUrl.",
                PublishedEnvironment.SectionName);
            return;
        }

        var interval = TimeSpan.FromSeconds(HeartbeatSeconds());

        _logger.LogInformation(
            "Node {Node} serving environment {Environment} will register with {ControlPlane}",
            NodeName(), _environment.KeySegment, controlPlaneUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BeatAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // The gateway was deleted from the dashboard: the remembered id now
                // points at nothing, so it is forgotten and the next beat registers
                // again under the same name.
                _logger.LogWarning(
                    ex,
                    "The control plane no longer knows this gateway; registering again under {Node}",
                    NodeName());
                await ForgetRegistrationAsync(NodeName());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not register or report to the control plane; the node keeps serving and will retry");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// One registration-if-needed plus one heartbeat.
    /// </summary>
    /// <remarks>
    /// The loop is the timing and this is the work, separated so a test can beat
    /// twice without waiting for the interval — the property worth asserting is
    /// that the second beat does not register again, which is a property of the
    /// work and not of the delay.
    /// </remarks>
    internal async Task BeatAsync(CancellationToken cancellationToken)
    {
        var nodeName = NodeName();

        var gatewayId = await EnsureRegisteredAsync(nodeName, cancellationToken);

        await _controlPlane.ReportStatusAsync(gatewayId, "Active", cancellationToken);
    }

    /// <summary>
    /// The node's gateway id, registering it if this is the first time.
    /// </summary>
    private async Task<string> EnsureRegisteredAsync(string nodeName, CancellationToken cancellationToken)
    {
        var remembered = await _database.StringGetAsync(RegistrationKey(nodeName));

        if (!remembered.IsNullOrEmpty)
            return remembered.ToString();

        var gatewayId = await _controlPlane.RegisterAsync(
            nodeName,
            $"Registered by the node itself, serving {_environment.KeySegment}",
            cancellationToken);

        await _database.StringSetAsync(RegistrationKey(nodeName), gatewayId);

        _logger.LogInformation(
            "Registered with the control plane as {Node} ({GatewayId}) in environment {Environment}",
            nodeName, gatewayId, _environment.KeySegment);

        return gatewayId;
    }

    /// <summary>
    /// Drops the remembered id, so the next beat registers again.
    /// </summary>
    internal Task ForgetRegistrationAsync(string nodeName) =>
        _database.KeyDeleteAsync(RegistrationKey(nodeName));

    /// <summary>
    /// Where the node's assigned id is remembered.
    /// </summary>
    /// <remarks>
    /// Scoped by environment like everything else the node keys on: the same node
    /// name serving two environments is two gateways, and one shared id would
    /// register the node in one environment while it heartbeats from another.
    /// </remarks>
    private string RegistrationKey(string nodeName) =>
        $"ocelot:gateway:registration:{_environment.KeySegment}:{nodeName}";

    /// <summary>
    /// What this node is called, which is also the name it registers under.
    /// </summary>
    /// <remarks>
    /// The configured name if there is one, and the assembly name otherwise — a node
    /// is built from this project and the folder it was built from is the node's
    /// name, so three nodes are three folders and each registers as itself with no
    /// settings at all. The environment variable spelling is for a container started
    /// with one line, the same reason the environment setting has one.
    /// </remarks>
    internal string NodeName()
    {
        var configured = _configuration[$"{PublishedEnvironment.SectionName}:NodeName"]
            ?? _configuration["GATEWAY_NODENAME"];

        return string.IsNullOrWhiteSpace(configured)
            ? Assembly.GetExecutingAssembly().GetName().Name ?? "ocelot.gateway.node"
            : configured.Trim();
    }

    private int HeartbeatSeconds()
    {
        var configured = _configuration[$"{PublishedEnvironment.SectionName}:HeartbeatSeconds"];

        return int.TryParse(configured, out var seconds) && seconds > 0
            ? seconds
            : DefaultHeartbeatSeconds;
    }
}
