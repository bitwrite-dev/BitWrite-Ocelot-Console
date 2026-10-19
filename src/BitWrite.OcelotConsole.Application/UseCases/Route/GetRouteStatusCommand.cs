using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public record GetRouteStatusCommand(
    RouteId RouteId
);

public record RouteStatusResponse(
    RouteId Id,
    string Key,
    bool IsEnabled,
    DateTimeOffset LastUpdatedAt
);
