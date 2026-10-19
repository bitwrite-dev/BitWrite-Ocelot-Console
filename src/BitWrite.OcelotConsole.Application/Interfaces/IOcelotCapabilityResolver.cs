using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Status;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IOcelotCapabilityResolver
{
    bool IsCapabilitySupported(string capabilityKey, OcelotVersion ocelotVersion);
    IReadOnlyList<CapabilityKey> GetSupportedCapabilities(OcelotVersion ocelotVersion);
    IReadOnlyList<CapabilityKey> ResolveEffectiveCapabilities(
        OcelotVersion ocelotVersion,
        IReadOnlyList<string> enabledPluginCapabilities);
}
