using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

public interface IExternalIdentityRepository
{
    Task<ExternalIdentity?> GetByIssuerAndSubjectAsync(string issuer, string subject, CancellationToken cancellationToken = default);
    Task<ExternalIdentity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExternalIdentity>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task AddAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExternalIdentity externalIdentity, CancellationToken cancellationToken = default);
}
