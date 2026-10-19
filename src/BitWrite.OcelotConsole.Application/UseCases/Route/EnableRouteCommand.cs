using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public record EnableRouteCommand(
    RouteId RouteId,
    string InitiatedBy = "",
    string CorrelationId = ""
);
