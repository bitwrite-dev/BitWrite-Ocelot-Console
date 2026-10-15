using BitWrite.OcelotConsole.Domain.Aggregates.RuntimeInstance;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IRuntimeInstanceRepository
{
    Task<RuntimeInstance?> GetAsync(GatewayId id, CancellationToken cancellationToken = default);
    Task<List<RuntimeInstance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RuntimeInstance runtimeInstance, CancellationToken cancellationToken = default);
    Task UpdateAsync(RuntimeInstance runtimeInstance, CancellationToken cancellationToken = default);
    Task DeleteAsync(GatewayId id, CancellationToken cancellationToken = default);
}
