using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Gateway;
using BitWrite.OcelotConsole.Domain.Aggregates.RuntimeInstance;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainGateway = BitWrite.OcelotConsole.Domain.Aggregates.Gateway.Gateway;

namespace BitWrite.OcelotConsole.Application.UseCases.Gateway;

public class RegisterGatewayCommandHandler
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IRuntimeInstanceRepository _runtimeInstanceRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RegisterGatewayCommandHandler(
        IGatewayRepository gatewayRepository,
        IRuntimeInstanceRepository runtimeInstanceRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _gatewayRepository = gatewayRepository;
        _runtimeInstanceRepository = runtimeInstanceRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<GatewayResponse> HandleAsync(RegisterGatewayCommand command, CancellationToken cancellationToken = default)
    {
        // Guard: refuse to create a duplicate gateway under the same name.
        // A gateway is identified by its name, not just its id — a node that
        // re-registers under the same name should point at the same record,
        // not mint a new gateway every time it restarts.
        var existing = await _gatewayRepository.GetByNameAsync(command.Name, cancellationToken);
        if (existing is not null)
            return MapToResponse(existing);

        // Create Gateway aggregate
        var gateway = DomainGateway.Register(command.Name, command.Description);

        // Persist
        await _gatewayRepository.AddAsync(gateway, cancellationToken);

        // Create the matching RuntimeInstance so the Overview page can see it.
        // A newly registered gateway starts with no reported version yet.
        var runtimeInstance = RuntimeInstance.Register(
            gateway.Id,
            new[] { "ocelot-gateway" });

        await _runtimeInstanceRepository.AddAsync(runtimeInstance, cancellationToken);

        // Dispatch domain events from both aggregates
        foreach (var domainEvent in gateway.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        gateway.ClearDomainEvents();

        foreach (var domainEvent in runtimeInstance.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        runtimeInstance.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "RegisterGateway",
            "Gateway",
            gateway.Id.Value.ToString(),
            "Success"
        );
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return MapToResponse(gateway);
    }

    private static GatewayResponse MapToResponse(DomainGateway gateway)
    {
        return new GatewayResponse(
            gateway.Id,
            gateway.Name,
            gateway.Description,
            gateway.Status,
            gateway.CreatedAt,
            gateway.UpdatedAt,
            gateway.LastHeartbeat
        );
    }
}
