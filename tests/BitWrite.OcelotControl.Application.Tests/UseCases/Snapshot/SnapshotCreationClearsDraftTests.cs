using BitWrite.OcelotControl.Application.Interfaces;
using BitWrite.OcelotControl.Application.UseCases.Snapshot;
using BitWrite.OcelotControl.Domain.Services;
using BitWrite.OcelotControl.Domain.ValueObjects.Configuration;
using BitWrite.OcelotControl.Domain.ValueObjects.Identity;
using BitWrite.OcelotControl.Domain.ValueObjects.Status;
using FluentAssertions;
using Moq;
using Xunit;
using DomainSystemSettings = BitWrite.OcelotControl.Domain.Aggregates.SystemSettings.SystemSettings;
using GlobalConfigAggregate = BitWrite.OcelotControl.Domain.Aggregates.GlobalConfiguration.GlobalConfiguration;
using GlobalConfigParam = BitWrite.OcelotControl.Domain.Services.GlobalConfiguration;
using HttpMethodValue = BitWrite.OcelotControl.Domain.ValueObjects.Configuration.HttpMethod;
using RouteAggregate = BitWrite.OcelotControl.Domain.Aggregates.Route.Route;
using RouteIdValue = BitWrite.OcelotControl.Domain.ValueObjects.Identity.RouteId;
using ServiceAggregate = BitWrite.OcelotControl.Domain.Aggregates.Service.Service;
using ServiceIdValue = BitWrite.OcelotControl.Domain.ValueObjects.Identity.ServiceId;
using SnapshotAggregate = BitWrite.OcelotControl.Domain.Aggregates.Snapshot.Snapshot;
using ValidationError = BitWrite.OcelotControl.Domain.Services.ValidationError;

namespace BitWrite.OcelotControl.Application.Tests.UseCases.Snapshot;

/// <summary>
/// The pending-capture flag's one invariant: sealing a snapshot captures
/// everything, so nothing is draft afterwards, and an edit re-flags exactly the
/// entity that moved.
///
/// This is what lets the create page answer "is there anything to capture?" —
/// and without the clearing step, every route, service and the settings would
/// stay draft forever after the first snapshot, so the page would offer a re-seal
/// of an unchanged configuration indefinitely.
/// </summary>
public class SnapshotCreationClearsDraftTests
{
    private static RouteAggregate ARoute(ServiceIdValue serviceId, string path) =>
        RouteAggregate.Create(
            HttpMethodValue.Get,
            UpstreamPath.From(path),
            serviceId,
            new List<DownstreamTarget> { DownstreamTarget.Create("http", "localhost", 5001) });

    [Fact]
    public async Task CreatingASnapshot_ClearsTheFlagOnEveryRouteServiceAndSetting()
    {
        var route = ARoute(ServiceIdValue.New(), "/api/orders");
        var service = ServiceAggregate.Create("orders");
        var globalConfig = GlobalConfigAggregate.Create();

        var (handler, routes, services, globalConfigs, snapshots) = BuildHandler(route, service, globalConfig);

        await handler.HandleAsync(new CreateSnapshotCommand("admin"));

        // The aggregates are mutated in place, so what the repositories were
        // handed afterwards is what the next read will return.
        routes.Verify(r => r.UpdateAsync(
            It.Is<RouteAggregate>(x => !x.IsDraft), It.IsAny<CancellationToken>()), Times.Once);
        services.Verify(r => r.UpdateAsync(
            It.Is<ServiceAggregate>(x => !x.IsDraft), It.IsAny<CancellationToken>()), Times.Once);
        globalConfigs.Verify(r => r.UpdateAsync(
            It.Is<GlobalConfigAggregate>(x => !x.IsDraft), It.IsAny<CancellationToken>()), Times.Once);

        // And the snapshot itself was stored, so the clearing is a consequence of
        // a real capture rather than of reaching the handler.
        PersistedSnapshot(snapshots).Version.Value.Should().Be(7);
    }

    [Fact]
    public async Task ClearingTheFlag_DoesNotPretendTheStateWasEdited()
    {
        // A stamp here would make the captured entity look modified by the act of
        // sealing it, and anything reading "modified at" beside the snapshot's
        // creation time would then disagree with the flag.
        var route = ARoute(ServiceIdValue.New(), "/api/orders");
        var updatedAt = route.UpdatedAt;

        var (handler, _, _, _, _) = BuildHandler(route, ServiceAggregate.Create("orders"), GlobalConfigAggregate.Create());

        await handler.HandleAsync(new CreateSnapshotCommand("admin"));

        route.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public async Task AFailedSeal_LeavesTheStateStillPendingCapture()
    {
        // Clearing before the snapshot is stored would claim a capture that does
        // not exist: the operator would be told there is nothing new to capture
        // while no snapshot carried the current configuration.
        var route = ARoute(ServiceIdValue.New(), "/api/orders");
        var service = ServiceAggregate.Create("orders");
        var globalConfig = GlobalConfigAggregate.Create();

        var (handler, routes, services, globalConfigs, snapshots) =
            BuildHandler(route, service, globalConfig, snapshotsFail: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new CreateSnapshotCommand("admin")));

        routes.Verify(r => r.UpdateAsync(It.IsAny<RouteAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        services.Verify(r => r.UpdateAsync(It.IsAny<ServiceAggregate>(), It.IsAny<CancellationToken>()), Times.Never);
        globalConfigs.Verify(
            r => r.UpdateAsync(It.IsAny<GlobalConfigAggregate>(), It.IsAny<CancellationToken>()), Times.Never);

        // Still flagged, because nothing captured it.
        route.IsDraft.Should().BeTrue();
    }

    /// <summary>
    /// Builds the handler over the state to seal, with the collaborators the
    /// create flow needs faked at the Application layer's own seams.
    /// </summary>
    /// <param name="snapshotsFail">
    /// Makes the snapshot repository refuse to store, which is how a seal fails.
    /// </param>
    private static (
        CreateSnapshotCommandHandler Handler,
        Mock<IRouteRepository> Routes,
        Mock<IServiceRepository> Services,
        Mock<IGlobalConfigurationRepository> GlobalConfigs,
        Mock<ISnapshotRepository> Snapshots)
        BuildHandler(
            RouteAggregate route,
            ServiceAggregate service,
            GlobalConfigAggregate globalConfig,
            bool snapshotsFail = false)
    {
        var snapshots = new Mock<ISnapshotRepository>();
        if (snapshotsFail)
        {
            snapshots.Setup(r => r.AddAsync(It.IsAny<SnapshotAggregate>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("storage refused the snapshot"));
        }

        var routes = new Mock<IRouteRepository>();
        routes.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RouteAggregate> { route });

        var services = new Mock<IServiceRepository>();
        services.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ServiceAggregate> { service });

        var globalConfigs = new Mock<IGlobalConfigurationRepository>();
        globalConfigs.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(globalConfig);

        var canonicalizer = new Mock<IConfigurationCanonicalizer>();
        canonicalizer.Setup(c => c.CanonicalizeJson(It.IsAny<OcelotConfiguration>()))
            .Returns<OcelotConfiguration>(c => new ConfigurationCanonicalizer().CanonicalizeJson(c));

        var integrityVerifier = new Mock<ISnapshotIntegrityVerifier>();
        integrityVerifier.Setup(v => v.ComputeHash(It.IsAny<string>()))
            .Returns<string>(content => new SnapshotIntegrityVerifier(new ConfigurationCanonicalizer()).ComputeHash(content));

        var builder = new Mock<IConfigurationBuilder>();
        builder.Setup(b => b.BuildConfiguration(
                It.IsAny<IReadOnlyList<RouteConfiguration>>(),
                It.IsAny<GlobalConfigParam>(),
                It.IsAny<OcelotVersion>()))
            .Returns(new OcelotConfiguration
            {
                GlobalConfiguration = new OcelotGlobalConfiguration
                {
                    BaseUrl = "http://localhost:5000",
                    RequestIdKey = "X-Request-Id",
                },
                Routes = new List<OcelotRouteConfiguration>
                {
                    new()
                    {
                        UpstreamPathTemplate = "/api/orders",
                        UpstreamHttpMethod = new[] { "GET" },
                        DownstreamHostAndPorts = new List<OcelotHostAndPort>
                        {
                            new() { Host = "localhost", Port = 5001 },
                        },
                    },
                },
            });

        var conflicts = new Mock<IRouteConflictDetector>();
        conflicts.Setup(d => d.DetectConflicts(
                It.IsAny<RouteKey>(),
                It.IsAny<IEnumerable<(RouteId Id, RouteKey Key)>>(),
                It.IsAny<RouteIdValue?>()))
            .Returns(Array.Empty<RouteKey>());

        var consistency = new Mock<IConfigurationConsistencyValidator>();
        consistency.Setup(v => v.ValidateServiceReferences(
                It.IsAny<IEnumerable<ServiceIdValue>>(),
                It.IsAny<IEnumerable<ServiceIdValue>>()))
            .Returns(Array.Empty<ValidationError>());
        consistency.Setup(v => v.ValidateDownstreamTargets(It.IsAny<IEnumerable<DownstreamTarget>>()))
            .Returns(Array.Empty<ValidationError>());
        consistency.Setup(v => v.ValidateGlobalConfiguration(
                It.IsAny<OcelotVersion>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Array.Empty<ValidationError>());

        var capabilities = new Mock<IOcelotCapabilityResolver>();
        capabilities.Setup(r => r.GetSupportedCapabilities(It.IsAny<OcelotVersion>()))
            .Returns(Array.Empty<CapabilityKey>());

        var allocator = new Mock<ISnapshotVersionAllocator>();
        allocator.Setup(a => a.AllocateNext()).Returns(SnapshotVersion.From(7));

        var systemSettings = new Mock<ISystemSettingsRepository>();
        systemSettings.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DomainSystemSettings.Reconstitute(
                OcelotVersion.V18_0,
                DateTimeOffset.UtcNow,
                "test",
                30,
                90,
                0,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        var handler = new CreateSnapshotCommandHandler(
            globalConfigs.Object,
            routes.Object,
            services.Object,
            snapshots.Object,
            builder.Object,
            conflicts.Object,
            consistency.Object,
            capabilities.Object,
            canonicalizer.Object,
            systemSettings.Object,
            integrityVerifier.Object,
            allocator.Object,
            new Mock<IDomainEventDispatcher>().Object);

        return (handler, routes, services, globalConfigs, snapshots);
    }

    private static SnapshotAggregate PersistedSnapshot(Mock<ISnapshotRepository> snapshots) =>
        (SnapshotAggregate)snapshots.Invocations
            .Single(i => i.Method.Name == nameof(ISnapshotRepository.AddAsync))
            .Arguments[0]!;
}
