using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User.Session;

/// <summary>
/// Session Aggregate Root - Manages user sessions (§8).
/// </summary>
/// <remarks>
/// Session tracks active user sessions and provides mechanisms for
/// session revocation and refresh token lifecycle management.
/// </summary>
public sealed class Session
{
    private readonly List<DomainEvent> _domainEvents = new();

    public SessionId Id { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public string? RefreshTokenId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? LastAccessedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Session() { }

    /// <summary>
    /// Creates a new session (§8).
    /// </summary>
    public static Session Create(
        UserId userId,
        DateTimeOffset expiresAt)
    {
        if (userId == null)
            throw new DomainException("UserId cannot be empty", "INVALID_USER_ID");

        var session = new Session
        {
            Id = SessionId.New(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        session.AddDomainEvent(new SessionCreated(session.Id, userId, session.CreatedAt));
        return session;
    }

    /// <summary>
    /// Records successful authentication for the session (§9).
    /// </summary>
    public void RecordAuthentication(string refreshTokenId, DateTimeOffset expiresAt)
    {
        RefreshTokenId = refreshTokenId;
        ExpiresAt = expiresAt;
        LastAccessedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new SessionAuthenticated(Id, UserId, refreshTokenId));
    }

    /// <summary>
    /// Revokes the session (§8).
    /// </summary>
    public void Revoke(string reason, string revokedBy)
    {
        IsRevoked = true;
        AddDomainEvent(new SessionRevoked(Id, UserId, reason, revokedBy, DateTimeOffset.UtcNow));
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
