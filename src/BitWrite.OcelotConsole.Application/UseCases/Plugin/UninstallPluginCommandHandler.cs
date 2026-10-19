using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Plugin;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainPlugin = BitWrite.OcelotConsole.Domain.Aggregates.Plugin.Plugin;

namespace BitWrite.OcelotConsole.Application.UseCases.Plugin;

public class UninstallPluginCommandHandler
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UninstallPluginCommandHandler(
        IPluginRepository pluginRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _pluginRepository = pluginRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(UninstallPluginCommand command, CancellationToken cancellationToken = default)
    {
        var plugin = await _pluginRepository.GetAsync(command.Id, cancellationToken);
        if (plugin == null)
            return false;

        // Uninstall the plugin
        plugin.Uninstall();

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
            "UninstallPlugin",
            "Plugin",
            plugin.Id.Value,
            "Success"
        );
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return true;
    }
}
