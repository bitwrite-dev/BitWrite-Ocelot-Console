using BitWrite.OcelotConsole.Domain.Aggregates.Gateway;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IGatewayRepository
{
    Task<Gateway?> GetAsync(GatewayId id, CancellationToken cancellationToken = default);
    Task<Gateway?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Gateway>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Gateway gateway, CancellationToken cancellationToken = default);
    Task UpdateAsync(Gateway gateway, CancellationToken cancellationToken = default);
    Task DeleteAsync(GatewayId id, CancellationToken cancellationToken = default);
}
