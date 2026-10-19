using System.Net.Http.Json;
using System.Text.Json;

namespace ocelot.gateway.node1.Configuration;

/// <summary>
/// The two calls a node makes to the control plane: register itself, and report
/// that it is still here.
/// </summary>
/// <remarks>
/// An interface rather than an <see cref="HttpClient"/> used directly, so a test can
/// host the registrar against a fake and assert on what the node would send — the
/// same reason the configuration subscriber takes a writer it can be handed.
/// </remarks>
public interface IControlPlaneClient
{
    /// <summary>
    /// Registers the node, and returns the gateway id the control plane assigned it.
    /// </summary>
    Task<string> RegisterAsync(
        string nodeName,
        string? description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds an existing gateway by name, or null if none exists.
    /// Used so a restarting node reuses its existing registration instead of
    /// minting a new gateway id every time Redis is flushed.
    /// </summary>
    Task<string?> FindGatewayByNameAsync(
        string nodeName,
        CancellationToken cancellationToken);

    /// <summary>Reports that the node is still serving.</summary>
    Task ReportStatusAsync(string gatewayId, string status, CancellationToken cancellationToken);
}

/// <summary>
/// The control plane's HTTP surface, as a node uses it.
/// </summary>
/// <remarks>
/// Every request names the environment the node serves, because that is what scopes a
/// gateway to one environment's data: a node registered without it would be listed
/// under whichever environment the control plane defaulted to, and could be published
/// to from another one. The name comes from the same setting the Redis keys use, so
/// neither can be right while the other is wrong.
/// </remarks>
public sealed class HttpControlPlaneClient : IControlPlaneClient
{
    private readonly HttpClient _http;
    private readonly PublishedEnvironment _environment;

    public HttpControlPlaneClient(HttpClient http, PublishedEnvironment environment)
    {
        _http = http;
        _environment = environment;
    }

    public async Task<string> RegisterAsync(
        string nodeName,
        string? description,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/gateways")
        {
            Content = JsonContent.Create(new { name = nodeName, description }),
        };
        request.Headers.Add("X-Environment", _environment.KeySegment);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

        // The id is the only thing taken from the answer. The name was ours, and the
        // status and timestamps are the control plane's business — a node that echoed
        // any of them back would be asserting something it does not know.
        var id = document.RootElement.TryGetProperty("id", out var idElement)
            ? idElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException(
                "The control plane answered a registration without a gateway id.");

        return id;
    }

    public async Task ReportStatusAsync(
        string gatewayId,
        string status,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch, $"api/v1/gateways/{Uri.EscapeDataString(gatewayId)}/status")
        {
            Content = JsonContent.Create(new { status }),
        };
        request.Headers.Add("X-Environment", _environment.KeySegment);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> FindGatewayByNameAsync(
        string nodeName,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/gateways");
        request.Headers.Add("X-Environment", _environment.KeySegment);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

        // The response is { gateways: [...], totalCount, page, pageSize }
        // Find the first gateway whose name matches nodeName.
        var root = document.RootElement;
        if (!root.TryGetProperty("gateways", out var gateways))
            return null;

        foreach (var gateway in gateways.EnumerateArray())
        {
            if (gateway.TryGetProperty("name", out var nameElement) &&
                string.Equals(nameElement.GetString(), nodeName, StringComparison.OrdinalIgnoreCase))
            {
                if (gateway.TryGetProperty("id", out var idElement))
                    return idElement.GetString();
                break;
            }
        }

        return null;
    }
}
