using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public record DisableRouteCommand(
    RouteId RouteId,
    string InitiatedBy = "",
    string CorrelationId = ""
);
