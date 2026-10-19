using BitWrite.OcelotConsole.Domain.Aggregates.Route;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IRouteRepository
{
    Task<Route?> GetAsync(RouteId id, CancellationToken cancellationToken = default);
    Task<List<Route>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Route route, CancellationToken cancellationToken = default);
    Task UpdateAsync(Route route, CancellationToken cancellationToken = default);
    Task DeleteAsync(RouteId id, CancellationToken cancellationToken = default);
}
