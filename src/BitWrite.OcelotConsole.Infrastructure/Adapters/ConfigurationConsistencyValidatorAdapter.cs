using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using ValidationError = BitWrite.OcelotConsole.Domain.Services.ValidationError;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements IConfigurationConsistencyValidator using Domain.ConfigurationConsistencyValidator.
/// </summary>
public class ConfigurationConsistencyValidatorAdapter : IConfigurationConsistencyValidator
{
    private readonly ConfigurationConsistencyValidator _domainValidator;

    public ConfigurationConsistencyValidatorAdapter(ConfigurationConsistencyValidator domainValidator)
    {
        _domainValidator = domainValidator;
    }

    public IReadOnlyList<ValidationError> ValidateServiceReferences(
        IEnumerable<ServiceId> routeServiceIds,
        IEnumerable<ServiceId> existingServiceIds)
    {
        return _domainValidator.ValidateServiceReferences(routeServiceIds, existingServiceIds);
    }

    public IReadOnlyList<ValidationError> ValidateDownstreamTargets(
        IEnumerable<DownstreamTarget> targets)
    {
        return _domainValidator.ValidateDownstreamTargets(targets);
    }

    public IReadOnlyList<ValidationError> ValidateRouteConfiguration(
        RouteId routeId,
        ServiceId serviceId,
        UpstreamPath upstreamPath,
        IReadOnlyList<DownstreamTarget> downstreamTargets)
    {
        return _domainValidator.ValidateRouteConfiguration(routeId, serviceId, upstreamPath, downstreamTargets);
    }

    public IReadOnlyList<ValidationError> ValidateGlobalConfiguration(
        OcelotVersion ocelotVersion,
        IEnumerable<string> features)
    {
        return _domainValidator.ValidateGlobalConfiguration(ocelotVersion, features);
    }
}
