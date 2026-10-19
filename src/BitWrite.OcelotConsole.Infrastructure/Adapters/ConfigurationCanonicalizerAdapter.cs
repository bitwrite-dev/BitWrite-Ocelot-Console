using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements IConfigurationCanonicalizer using Domain.ConfigurationCanonicalizer.
/// </summary>
public class ConfigurationCanonicalizerAdapter : IConfigurationCanonicalizer
{
    private readonly ConfigurationCanonicalizer _domainCanonicalizer;

    public ConfigurationCanonicalizerAdapter(ConfigurationCanonicalizer domainCanonicalizer)
    {
        _domainCanonicalizer = domainCanonicalizer;
    }

    public string Canonicalize(OcelotConfiguration configuration)
    {
        return _domainCanonicalizer.Canonicalize(configuration);
    }

    public string CanonicalizeJson(OcelotConfiguration configuration)
    {
        return _domainCanonicalizer.CanonicalizeJson(configuration);
    }
}
