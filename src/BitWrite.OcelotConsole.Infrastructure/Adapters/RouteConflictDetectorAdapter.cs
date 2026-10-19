using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Services;
using BitWrite.OcelotConsole.Domain.ValueObjects.Configuration;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Infrastructure.Adapters;

/// <summary>
/// Adapter that implements IRouteConflictDetector using Domain.RouteConflictDetector.
/// </summary>
public class RouteConflictDetectorAdapter : IRouteConflictDetector
{
    private readonly RouteConflictDetector _domainDetector;

    public RouteConflictDetectorAdapter(RouteConflictDetector domainDetector)
    {
        _domainDetector = domainDetector;
    }

    public IReadOnlyList<RouteKey> DetectConflicts(
        RouteKey newRouteKey,
        IEnumerable<(RouteId Id, RouteKey Key)> existingRouteKeys,
        RouteId? excludeRouteId = null)
    {
        return _domainDetector.DetectConflicts(newRouteKey, existingRouteKeys, excludeRouteId);
    }
}
