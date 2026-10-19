using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Service;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainService = BitWrite.OcelotConsole.Domain.Aggregates.Service.Service;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public class CreateServiceCommandHandler
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateServiceCommandHandler(
        IServiceRepository serviceRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _serviceRepository = serviceRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<ServiceResponse> HandleAsync(CreateServiceCommand command, CancellationToken cancellationToken = default)
    {
        // Create Service aggregate
        var service = DomainService.Create(command.Name, command.Description);

        // Add downstream targets if provided
        if (command.DownstreamTargets != null && command.DownstreamTargets.Any())
        {
            foreach (var target in command.DownstreamTargets)
            {
                service.AddHost(target.Host, target.Port, target.Weight);
            }
        }

        // Persist
        await _serviceRepository.AddAsync(service, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in service.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        service.ClearDomainEvents();

        // Dispatch audit event
        var auditEvent = new AuditRecorded(
            command.InitiatedBy,
            "CreateService",
            "Service",
            service.Id.Value.ToString(),
            "Success"
        );
        await _eventDispatcher.DispatchAsync(auditEvent, cancellationToken);

        return MapToResponse(service);
    }

    private static ServiceResponse MapToResponse(DomainService service) =>
        ServiceResponseMapper.Map(service);
}
