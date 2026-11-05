using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User.Session;

/// <summary>
/// RefreshToken entity - Manages refresh token lifecycle (§8).
/// </summary>
/// <remarks>
/// Refresh tokens are used to obtain new access tokens without requiring
/// re-authentication. They are stored as hashes and support rotation.
/// </summary>
public sealed class RefreshToken
{
    private readonly List<DomainEvent> _domainEvents = new();

    public RefreshTokenId Id { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? RevokedBy { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByTokenId { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private RefreshToken() { }

    /// <summary>
    /// Creates a new refresh token (§8).
    /// </summary>
    public static RefreshToken Create(
        UserId userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash cannot be empty", "INVALID_TOKEN_HASH");

        var refreshToken = new RefreshToken
        {
            Id = RefreshTokenId.New(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        refreshToken.AddDomainEvent(new RefreshTokenCreated(refreshToken.Id, userId, refreshToken.ExpiresAt, correlationId));
        return refreshToken;
    }

    /// <summary>
    /// Revokes the refresh token (§8).
    /// </summary>
    public void Revoke(string revokedBy, string reason, string correlationId = "")
    {
        if (IsRevoked)
            throw new DomainException($"Refresh token {Id} is already revoked", "TOKEN_ALREADY_REVOKED");

        IsRevoked = true;
        RevokedBy = revokedBy;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason;

        AddDomainEvent(new RefreshTokenRevoked(Id, UserId, revokedBy, reason));
    }

    /// <summary>
    /// Marks the token as replaced by a new token during rotation (§8).
    /// </summary>
    public void MarkAsReplaced(string replacedByTokenId, string correlationId = "")
    {
        if (IsRevoked)
            throw new DomainException($"Refresh token {Id} is already revoked", "TOKEN_ALREADY_REVOKED");

        IsRevoked = true;
        RevokedBy = "TokenRotation";
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = "Token rotated";
        ReplacedByTokenId = replacedByTokenId;

        AddDomainEvent(new RefreshTokenRotated(Id, UserId, replacedByTokenId));
    }

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsExpired => ExpiresAt < DateTimeOffset.UtcNow;

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
