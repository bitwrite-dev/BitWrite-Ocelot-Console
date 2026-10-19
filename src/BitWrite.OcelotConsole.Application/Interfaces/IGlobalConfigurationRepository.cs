using BitWrite.OcelotConsole.Domain.Aggregates.GlobalConfiguration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IGlobalConfigurationRepository
{
    Task<GlobalConfiguration> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(GlobalConfiguration globalConfiguration, CancellationToken cancellationToken = default);
    Task UpdateAsync(GlobalConfiguration globalConfiguration, CancellationToken cancellationToken = default);
}
