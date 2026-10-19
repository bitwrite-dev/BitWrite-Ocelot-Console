using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Plugin;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainPlugin = BitWrite.OcelotConsole.Domain.Aggregates.Plugin.Plugin;

namespace BitWrite.OcelotConsole.Application.UseCases.Plugin;

public class GetPluginQueryHandler
{
    private readonly IPluginRepository _pluginRepository;

    public GetPluginQueryHandler(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<PluginResponse?> HandleAsync(GetPluginQuery query, CancellationToken cancellationToken = default)
    {
        var plugin = await _pluginRepository.GetAsync(query.Id, cancellationToken);
        return plugin != null ? MapToResponse(plugin) : null;
    }

    private static PluginResponse MapToResponse(DomainPlugin plugin)
    {
        return new PluginResponse(
            plugin.Id,
            plugin.Name,
            plugin.Version,
            plugin.Description,
            plugin.Scope,
            plugin.IsEnabled,
            plugin.InstalledAt,
            plugin.LastUpdated,
            plugin.LastEnabled,
            plugin.LastDisabled,
            plugin.GetCapabilityKeys()
        );
    }
}
