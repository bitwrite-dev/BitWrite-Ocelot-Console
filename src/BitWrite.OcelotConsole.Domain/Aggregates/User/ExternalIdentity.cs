using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User;

/// <summary>
/// ExternalIdentity - Linked external identity provider account (§7.4).
/// </summary>
/// <remarks>
/// Identified by issuer + subject combination (unique constraint).
/// Every external authentication maps to one internal BitWrite User (§4.3).
/// </summary>
public sealed class ExternalIdentity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public Guid Id { get; private set; }
    public UserId UserId { get; private set; } = null!;
    public string Issuer { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public string ProviderType { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAuthenticatedAt { get; private set; }

    private ExternalIdentity() { }

    public static ExternalIdentity Create(
        UserId userId,
        string issuer,
        string subject,
        string providerType,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new DomainException("Issuer cannot be empty", "INVALID_ISSUER");

        if (string.IsNullOrWhiteSpace(subject))
            throw new DomainException("Subject cannot be empty", "INVALID_SUBJECT");

        var externalIdentity = new ExternalIdentity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Issuer = issuer.Trim(),
            Subject = subject.Trim(),
            ProviderType = providerType.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastAuthenticatedAt = null
        };

        externalIdentity.AddDomainEvent(new ExternalIdentityLinked(
            userId, externalIdentity.Id, correlationId));

        return externalIdentity;
    }

    public void UpdateLastAuthenticatedAt(string correlationId = "")
    {
        LastAuthenticatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ExternalIdentityAuthenticated(UserId, Id, correlationId));
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
