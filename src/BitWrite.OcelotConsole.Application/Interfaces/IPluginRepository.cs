using BitWrite.OcelotConsole.Domain.Aggregates.Plugin;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IPluginRepository
{
    Task<Plugin?> GetAsync(PluginId id, CancellationToken cancellationToken = default);
    Task<List<Plugin>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Plugin plugin, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default);
    Task DeleteAsync(PluginId id, CancellationToken cancellationToken = default);
}
