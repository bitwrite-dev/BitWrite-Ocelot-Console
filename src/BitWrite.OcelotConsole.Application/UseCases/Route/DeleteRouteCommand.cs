using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Route;

public record DeleteRouteCommand(
    RouteId Id,
    string InitiatedBy = "",
    string CorrelationId = ""
);
