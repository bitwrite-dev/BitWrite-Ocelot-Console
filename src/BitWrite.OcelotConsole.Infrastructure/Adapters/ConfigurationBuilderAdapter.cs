using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using GlobalConfig = BitWrite.OcelotConsole.Domain.Services.GlobalConfiguration;
using HttpMethod = BitWrite.OcelotConsole.Domain.ValueObjects.Configuration.HttpMethod;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements IConfigurationBuilder using Domain.ConfigurationBuilder.
/// </summary>
public class ConfigurationBuilderAdapter : IConfigurationBuilder
{
    private readonly ConfigurationBuilder _domainBuilder;

    public ConfigurationBuilderAdapter(ConfigurationBuilder domainBuilder)
    {
        _domainBuilder = domainBuilder;
    }

    public OcelotConfiguration BuildConfiguration(
        IReadOnlyList<RouteConfiguration> routes,
        GlobalConfig globalConfig,
        OcelotVersion ocelotVersion)
    {
        return _domainBuilder.BuildConfiguration(routes, globalConfig, ocelotVersion);
    }

    public ConfigurationHash CalculateConfigurationHash(OcelotConfiguration configuration)
    {
        return _domainBuilder.CalculateConfigurationHash(configuration);
    }
}
