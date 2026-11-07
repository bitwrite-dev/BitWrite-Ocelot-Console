using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IRoleRepository
{
    Task<RoleEntity?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);
    Task<RoleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<RoleEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoleEntity role, CancellationToken cancellationToken = default);
    Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default);
}
