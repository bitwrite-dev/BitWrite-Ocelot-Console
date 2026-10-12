using DomainService = BitWrite.OcelotControl.Domain.Aggregates.Service.Service;

namespace BitWrite.OcelotControl.Application.UseCases.Service;

/// <summary>
/// The single place a service aggregate is projected onto the response shape.
/// </summary>
/// <remarks>
/// Four handlers carried their own copy of this mapping, which is the drift the
/// route response mapper was extracted to fix: adding a field meant editing every
/// copy, and a missed copy reports older state for one endpoint and newer state
/// for another. Extracted before this field was needed for the same reason.
/// </remarks>
public static class ServiceResponseMapper
{
    public static ServiceResponse Map(DomainService service) =>
        new(
            service.Id,
            service.Name,
            service.Description,
            service.Endpoints.Select(e => new ServiceEndpoint(e.Host, e.Port, e.Weight, e.IsActive)).ToList(),
            service.CreatedAt,
            service.UpdatedAt,
            service.IsDraft);
}
