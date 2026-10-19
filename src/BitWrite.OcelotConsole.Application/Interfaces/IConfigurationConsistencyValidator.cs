using BitWrite.OcelotConsole.Domain.Aggregates.Route;
using BitWrite.OcelotConsole.Domain.Aggregates.Service;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;
using ValidationError = BitWrite.OcelotConsole.Domain.Services.ValidationError;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IConfigurationConsistencyValidator
{
    IReadOnlyList<ValidationError> ValidateServiceReferences(
        IEnumerable<ServiceId> routeServiceIds,
        IEnumerable<ServiceId> existingServiceIds);

    IReadOnlyList<ValidationError> ValidateDownstreamTargets(
        IEnumerable<DownstreamTarget> targets);

    IReadOnlyList<ValidationError> ValidateRouteConfiguration(
        RouteId routeId,
        ServiceId serviceId,
        UpstreamPath upstreamPath,
        IReadOnlyList<DownstreamTarget> downstreamTargets);

    IReadOnlyList<ValidationError> ValidateGlobalConfiguration(
        OcelotVersion ocelotVersion,
        IEnumerable<string> features);
}
