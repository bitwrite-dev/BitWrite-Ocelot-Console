using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Repository for Session aggregate (§8).
/// </summary>
public interface ISessionRepository
{
    Task<Session?> GetAsync(SessionId id, CancellationToken cancellationToken = default);
    Task<List<Session>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<Session>> GetActiveSessionsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
    Task UpdateAsync(Session session, CancellationToken cancellationToken = default);
    Task DeleteAsync(SessionId id, CancellationToken cancellationToken = default);
}
