using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Service;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using DomainService = BitWrite.OcelotConsole.Domain.Aggregates.Service.Service;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public class ListServicesQueryHandler
{
    private readonly IServiceRepository _serviceRepository;

    public ListServicesQueryHandler(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<ServiceListResponse> HandleAsync(ListServicesQuery query, CancellationToken cancellationToken = default)
    {
        var services = await _serviceRepository.GetAllAsync(cancellationToken);

        var totalCount = services.Count;
        var pagedServices = services
            .OrderBy(s => s.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var response = new ServiceListResponse(
            pagedServices.Select(MapToResponse).ToList(),
            totalCount,
            query.Page,
            query.PageSize
        );

        return response;
    }

    private static ServiceResponse MapToResponse(DomainService service) =>
        ServiceResponseMapper.Map(service);
}
