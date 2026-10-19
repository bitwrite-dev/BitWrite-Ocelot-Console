using BitWrite.OcelotConsole.Domain.Aggregates.GlobalConfiguration;
using BitWrite.OcelotConsole.Domain.Aggregates.Route;
using BitWrite.OcelotConsole.Domain.Aggregates.Service;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using GlobalConfig = BitWrite.OcelotConsole.Domain.Services.GlobalConfiguration;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IConfigurationBuilder
{
    OcelotConfiguration BuildConfiguration(
        IReadOnlyList<RouteConfiguration> routes,
        GlobalConfig globalConfig,
        OcelotVersion ocelotVersion);

    ConfigurationHash CalculateConfigurationHash(OcelotConfiguration configuration);
}
