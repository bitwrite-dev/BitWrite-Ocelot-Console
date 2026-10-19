using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Plugin;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainPlugin = BitWrite.OcelotConsole.Domain.Aggregates.Plugin.Plugin;

namespace BitWrite.OcelotConsole.Application.UseCases.Plugin;

public class ListPluginsQueryHandler
{
    private readonly IPluginRepository _pluginRepository;

    public ListPluginsQueryHandler(IPluginRepository pluginRepository)
    {
        _pluginRepository = pluginRepository;
    }

    public async Task<PluginListResponse> HandleAsync(PluginListQuery query, CancellationToken cancellationToken = default)
    {
        var plugins = await _pluginRepository.GetAllAsync(cancellationToken);

        var totalCount = plugins.Count;
        var pagedPlugins = plugins
            .OrderByDescending(p => p.InstalledAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var response = new PluginListResponse(
            pagedPlugins.Select(MapToResponse).ToList(),
            totalCount,
            query.Page,
            query.PageSize
        );

        return response;
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
