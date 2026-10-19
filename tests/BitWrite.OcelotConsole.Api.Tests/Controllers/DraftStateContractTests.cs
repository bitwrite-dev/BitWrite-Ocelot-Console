using System.Text.Json;
using BitWrite.OcelotConsole.Api.DTOs;
using FluentAssertions;
using Xunit;

namespace BitWrite.OcelotConsole.Api.Tests.Controllers;

/// <summary>
/// The pending-capture flag on the three endpoints that report management state.
///
/// The dashboard has read `isDraft` since the create page's draft counts were
/// added, and the field did not exist on any response — so every read was
/// `undefined`, the counts were always zero, and nothing failed: not the build,
/// not the tests, not the browser. These pin the wire name to the three
/// endpoints that carry it.
/// </summary>
public class DraftStateContractTests
{
    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web);

    private static string ToJson<T>(T response) => JsonSerializer.Serialize(response, WireOptions);

    [Fact]
    public void RoutesReportTheFlag()
    {
        var json = ToJson(new RouteResponse(
            "00000000-0000-0000-0000-000000000001",
            "GET:/api/orders",
            "GET",
            "/api/orders",
            null,
            "00000000-0000-0000-0000-000000000002",
            true,
            new List<DownstreamTargetResponse>(),
            // The nine option blocks a route can carry, none of them set here:
            // the flag is the field under test, so the route is otherwise bare.
            null, null, null, null, null, null, null, null, null,
            0, false, null, null, null, null, false,
            new List<string>(), null, null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            true));

        var root = JsonDocument.Parse(json).RootElement;

        root.TryGetProperty("isDraft", out _).Should().BeTrue("the create page counts draft routes");
        root.GetProperty("isDraft").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ServicesReportTheFlag()
    {
        var json = ToJson(new ServiceResponse(
            "00000000-0000-0000-0000-000000000001",
            "orders",
            null,
            new List<ServiceEndpointResponse>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            false));

        var root = JsonDocument.Parse(json).RootElement;

        root.TryGetProperty("isDraft", out _).Should().BeTrue("the create page counts draft services");
        root.GetProperty("isDraft").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GlobalConfigurationReportsTheFlag()
    {
        var json = ToJson(new GlobalConfigurationResponse(
            "00000000-0000-0000-0000-000000000001",
            "http://localhost:5000",
            "X-Request-Id",
            "http",
            90000,
            null, null, null, null,
            DateTimeOffset.UtcNow,
            true));

        var root = JsonDocument.Parse(json).RootElement;

        // The settings are captured too, so the create page counts them among
        // what is pending.
        root.TryGetProperty("isDraft", out _).Should().BeTrue("the create page counts draft settings");
        root.GetProperty("isDraft").GetBoolean().Should().BeTrue();
    }
}
