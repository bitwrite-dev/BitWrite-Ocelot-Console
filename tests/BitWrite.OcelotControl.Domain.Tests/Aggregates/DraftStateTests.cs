using BitWrite.OcelotControl.Domain.Aggregates.GlobalConfiguration;
using BitWrite.OcelotControl.Domain.Aggregates.Route;
using BitWrite.OcelotControl.Domain.Aggregates.Service;
using BitWrite.OcelotControl.Domain.Events;
using BitWrite.OcelotControl.Domain.ValueObjects.Configuration;
using BitWrite.OcelotControl.Domain.ValueObjects.FeatureConfig;
using BitWrite.OcelotControl.Domain.ValueObjects.Identity;
using FluentAssertions;
using HttpMethod = BitWrite.OcelotControl.Domain.ValueObjects.Configuration.HttpMethod;

namespace BitWrite.OcelotControl.Domain.Tests.Aggregates;

/// <summary>
/// The pending-capture flag's lifecycle on each aggregate that a snapshot
/// captures: created uncaptured, changed uncaptured, captured on request.
///
/// The flag is what the create page reads to say how much is still pending, so
/// the two ways it can be wrong are both expensive. Left set after a capture and
/// the page offers a re-seal of an unchanged configuration forever; left unset by
/// a change and the page says there is nothing new when there is.
/// </summary>
public class DraftStateTests
{
    private static Route ARoute() =>
        Route.Create(
            HttpMethod.Get,
            UpstreamPath.From("/api/orders"),
            ServiceId.New(),
            new List<DownstreamTarget> { DownstreamTarget.Create("http", "localhost", 5001) });

    [Fact]
    public void EverythingStartsUncaptured()
    {
        // A route nobody has sealed yet is pending by definition; starting
        // captured would hide the very first change from the create page.
        ARoute().IsDraft.Should().BeTrue();
        Service.Create("orders").IsDraft.Should().BeTrue();
        GlobalConfiguration.Create().IsDraft.Should().BeTrue();
    }

    [Fact]
    public void AChangeFlagsTheEntityThatChanged()
    {
        var route = ARoute();
        var service = Service.Create("orders");
        var settings = GlobalConfiguration.Create();

        route.MarkAsCaptured();
        service.MarkAsCaptured();
        settings.MarkAsCaptured();

        route.UpdateKey("orders");
        service.UpdateName("orders-renamed");
        settings.SetBaseUrl("http://gateway:5000");

        route.IsDraft.Should().BeTrue();
        service.IsDraft.Should().BeTrue();
        settings.IsDraft.Should().BeTrue();
    }

    [Fact]
    public void DisablingOrEnablingRouteCountsAsAChange()
    {
        // Enable/Disable changes the sealed configuration, so a snapshot taken
        // while a route is disabled differs from one taken while it is not.
        var route = ARoute();
        route.MarkAsCaptured();

        route.Disable();

        route.IsDraft.Should().BeTrue();
    }

    [Fact]
    public void EveryFeatureAssignmentCountsAsAChange()
    {
        // The feature blocks are part of the captured configuration, so setting
        // one is a change like any other.
        var route = ARoute();
        route.MarkAsCaptured();

        route.SetAuthentication(AuthenticationOptions.Create("Bearer"));

        route.IsDraft.Should().BeTrue();
    }

    [Fact]
    public void HostEditsCountAsAChange()
    {
        var service = Service.Create("orders");
        service.AddHost("localhost", 5001);
        service.MarkAsCaptured();

        service.UpdateHostWeight("localhost", 5001, 3);

        service.IsDraft.Should().BeTrue();
    }

    [Fact]
    public void ClearingTheFlagDoesNotClaimTheContentWasEdited()
    {
        // The stamp records when the content last moved. Clearing the flag says
        // the content was taken, which is not the same event, and a stamp here
        // would make a captured entity look modified by the act of capturing it.
        var route = ARoute();
        var service = Service.Create("orders");
        var settings = GlobalConfiguration.Create();
        var routeUpdatedAt = route.UpdatedAt;
        var serviceUpdatedAt = service.UpdatedAt;
        var settingsUpdatedAt = settings.UpdatedAt;

        route.MarkAsCaptured();
        service.MarkAsCaptured();
        settings.MarkAsCaptured();

        route.IsDraft.Should().BeFalse();
        service.IsDraft.Should().BeFalse();
        settings.IsDraft.Should().BeFalse();

        route.UpdatedAt.Should().Be(routeUpdatedAt);
        service.UpdatedAt.Should().Be(serviceUpdatedAt);
        settings.UpdatedAt.Should().Be(settingsUpdatedAt);
    }

    [Fact]
    public void ClearingTheFlagRaisesNoEvent()
    {
        // The capture is already recorded by the snapshot's own SnapshotCreated
        // event; one per route as well would flood the audit trail with restatements.
        var route = ARoute();

        route.MarkAsCaptured();

        route.DomainEvents.Should().HaveCount(1);
        route.DomainEvents.Single().Should().BeOfType<RouteCreated>();
    }
}
