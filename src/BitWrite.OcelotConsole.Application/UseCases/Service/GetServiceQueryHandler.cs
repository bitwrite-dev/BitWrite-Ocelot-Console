using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Service;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainService = BitWrite.OcelotConsole.Domain.Aggregates.Service.Service;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public class GetServiceQueryHandler
{
    private readonly IServiceRepository _serviceRepository;

    public GetServiceQueryHandler(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<ServiceResponse?> HandleAsync(GetServiceQuery query, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepository.GetAsync(query.Id, cancellationToken);
        return service != null ? MapToResponse(service) : null;
    }

    private static ServiceResponse MapToResponse(DomainService service) =>
        ServiceResponseMapper.Map(service);
}
