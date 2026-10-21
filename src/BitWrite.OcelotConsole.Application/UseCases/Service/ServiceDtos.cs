using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Service;

public record ServiceResponse(
    ServiceId Id,
    string Name,
    string? Description,
    IReadOnlyList<ServiceEndpoint> Endpoints,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDraft
);

public record ServiceEndpoint(
    string Host,
    int Port,
    int Weight,
    bool IsActive
);

public record ServiceListResponse(
    IReadOnlyList<ServiceResponse> Services,
    int TotalCount,
    int Page,
    int PageSize
);

public record GetServiceQuery(
    ServiceId Id
);

public record ListServicesQuery(
    int Page = 1,
    int PageSize = 20
);

public record GetServiceRoutesQuery(
    ServiceId Id
);

public record ServiceRoutesResponse(
    IReadOnlyList<RouteSummary> Routes
);

public record RouteSummary(
    RouteId Id,
    string Key,
    string Method,
    string UpstreamPath,
    bool IsEnabled,
    bool IsDraft
);
