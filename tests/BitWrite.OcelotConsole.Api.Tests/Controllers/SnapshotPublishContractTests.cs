using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BitWrite.OcelotConsole.Api.DTOs;
using BitWrite.OcelotConsole.Api.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BitWrite.OcelotConsole.Api.Tests;

/// <summary>
/// What a publication has to name about itself before it is allowed to run.
/// </summary>
/// <remarks>
/// The dashboard published with only an initiator, so every publication was
/// answered with a validation failure — "The Environment field is required" and
/// "The TargetGatewayIds field is required" — and nothing reached a gateway.
/// Both are required on purpose: the environment decides whose gateways are
/// being written to, and the target list decides which ones. Body and header
/// also have to agree about the environment, which the controller checks.
///
/// The gateway ids were declared as <c>int[]</c> while gateway ids are GUIDs,
/// so even a request that named everything else failed inside the handler with
/// "Invalid GatewayId format". The contract is now what the rest of the system
/// already speaks.
/// </remarks>
public class SnapshotPublishContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SnapshotPublishContractTests(WebApplicationFactory<Program> factory) =>
        _factory = factory;

    /// <summary>A GUID, the form every gateway id has. Only its shape matters here.</summary>
    private static readonly string GatewayId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    /// <summary>
    /// A client that names its environment on every request, as the dashboard does.
    /// </summary>
    /// <remarks>
    /// The controller itself refuses a publication whose request named no
    /// environment, so the header is set the same way a real client sets it.
    /// </remarks>
    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Environment", "development");
        return client;
    }

    [Fact]
    public async Task RefusesAPublicationThatNamesNoEnvironment()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/snapshots/4/publish",
            new { initiatedBy = "test", targetGatewayIds = new[] { GatewayId } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await ValidationErrors(response);
        errors.Should().Contain("Environment",
            "a publication that named no environment would reach one environment's " +
            "gateways while claiming to serve another");
    }

    [Fact]
    public async Task RefusesAPublicationThatNamesNoGateways()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/snapshots/4/publish",
            new
            {
                initiatedBy = "test",
                environment = "development",
                targetGatewayIds = Array.Empty<string>(),
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = await ValidationErrors(response);
        errors.Should().Contain("TargetGatewayIds",
            "an empty target list is not 'every gateway': it is a publication that " +
            "reaches nobody and still reports success");
    }

    [Fact]
    public void TheValidatorNamesTheEnvironmentAndTheGateways()
    {
        var validator = new SnapshotPublishRequestValidator();

        var result = validator.Validate(
            new SnapshotPublishRequest("test", string.Empty, Array.Empty<string>()));

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should().BeEquivalentTo(new[] { "Environment", "TargetGatewayIds" });
    }

    [Fact]
    public void BindsTheGatewayIdsAsTheStringsTheGatewayListHandsOut()
    {
        // Ids arrive as GUID strings from GET /api/v1/gateways, and they bind as
        // strings — the int[] they used to be declared as could never hold one.
        const string json =
            """{"initiatedBy":"test","environment":"development","targetGatewayIds":["3f2504e0-4f89-11d3-9a0c-0305e82c3301"]}""";

        var request = JsonSerializer.Deserialize<SnapshotPublishRequest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        request!.TargetGatewayIds.Should().BeEquivalentTo(new[] { GatewayId });
    }

    /// <summary>
    /// The field names a validation answer named, as the response lists them.
    /// </summary>
    /// <remarks>
    /// Read from the `errors` dictionary when the answer carries one, and from
    /// the message otherwise, so a change in envelope shape does not turn a
    /// passing contract check into a parse failure.
    /// </remarks>
    private static async Task<List<string>> ValidationErrors(HttpResponseMessage response)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        var names = new List<string>();
        if (json.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in errors.EnumerateObject())
            {
                names.Add(property.Name);
            }
        }
        else if (json.TryGetProperty("error", out var message) &&
                 message.ValueKind == JsonValueKind.String)
        {
            names.Add(message.GetString() ?? string.Empty);
        }

        return names;
    }
}
