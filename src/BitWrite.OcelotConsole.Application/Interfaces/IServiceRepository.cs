using BitWrite.OcelotConsole.Domain.Aggregates.Service;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetAsync(ServiceId id, CancellationToken cancellationToken = default);
    Task<List<Service>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task DeleteAsync(ServiceId id, CancellationToken cancellationToken = default);
}
