using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Persistence contract for local credentials (§6). Infrastructure implements using IAM namespace.
/// </summary>
public interface ILocalCredentialRepository
{
    Task<LocalCredential?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(LocalCredential credential, CancellationToken cancellationToken = default);
    Task UpdateAsync(LocalCredential credential, CancellationToken cancellationToken = default);
}
