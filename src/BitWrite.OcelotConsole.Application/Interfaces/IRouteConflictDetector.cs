using BitWrite.OcelotConsole.Domain.Aggregates.Route;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IRouteConflictDetector
{
    IReadOnlyList<RouteKey> DetectConflicts(
        RouteKey newRouteKey,
        IEnumerable<(RouteId Id, RouteKey Key)> existingRouteKeys,
        RouteId? excludeRouteId = null);
}
