using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Plugin;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainPlugin = BitWrite.OcelotConsole.Domain.Aggregates.Plugin.Plugin;

namespace BitWrite.OcelotConsole.Application.UseCases.Plugin;

public class DisablePluginCommandHandler
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DisablePluginCommandHandler(
        IPluginRepository pluginRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _pluginRepository = pluginRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<PluginResponse?> HandleAsync(DisablePluginCommand command, CancellationToken cancellationToken = default)
    {
        var plugin = await _pluginRepository.GetAsync(command.Id, cancellationToken);
        if (plugin == null)
            return null;

        // Disable the plugin
        plugin.Disable();

        // Persist
        await _pluginRepository.UpdateAsync(plugin, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in plugin.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        plugin.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "DisablePlugin",
            "Plugin",
            plugin.Id.Value,
            "Success"
        );
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return MapToResponse(plugin);
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
