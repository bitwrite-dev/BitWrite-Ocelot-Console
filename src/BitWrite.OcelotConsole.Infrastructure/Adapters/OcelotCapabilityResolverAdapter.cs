using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements IOcelotCapabilityResolver using Domain.OcelotCapabilityResolver.
/// </summary>
public class OcelotCapabilityResolverAdapter : IOcelotCapabilityResolver
{
    private readonly OcelotCapabilityResolver _domainResolver;

    public OcelotCapabilityResolverAdapter(OcelotCapabilityResolver domainResolver)
    {
        _domainResolver = domainResolver;
    }

    public bool IsCapabilitySupported(string capabilityKey, OcelotVersion ocelotVersion)
    {
        return _domainResolver.IsCapabilitySupported(capabilityKey, ocelotVersion);
    }

    public IReadOnlyList<CapabilityKey> GetSupportedCapabilities(OcelotVersion ocelotVersion)
    {
        return _domainResolver.GetSupportedCapabilities(ocelotVersion);
    }

    public IReadOnlyList<CapabilityKey> ResolveEffectiveCapabilities(
        OcelotVersion ocelotVersion,
        IReadOnlyList<string> enabledPluginCapabilities)
    {
        return _domainResolver.ResolveEffectiveCapabilities(ocelotVersion, enabledPluginCapabilities);
    }
}
