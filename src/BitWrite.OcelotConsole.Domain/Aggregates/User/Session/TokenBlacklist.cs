using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User.Session;

/// <summary>
/// TokenBlacklist entity - Manages revoked tokens (§8).
/// </summary>
/// <remarks>
/// Token blacklist tracks revoked access tokens and refresh tokens to prevent
/// replay attacks after revocation.
/// </summary>
public sealed class TokenBlacklist
{
    private readonly List<DomainEvent> _domainEvents = new();

    public TokenBlacklistId Id { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public string TokenType { get; private set; } = null!; // "AccessToken" or "RefreshToken"
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? RevokedBy { get; private set; }
    public string? Reason { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private TokenBlacklist() { }

    /// <summary>
    /// Creates a new blacklist entry (§8).
    /// </summary>
    public static TokenBlacklist Create(
        string tokenHash,
        string tokenType,
        DateTimeOffset expiresAt,
        string? revokedBy = null,
        string? reason = null,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash cannot be empty", "INVALID_TOKEN_HASH");

        if (string.IsNullOrWhiteSpace(tokenType))
            throw new DomainException("Token type cannot be empty", "INVALID_TOKEN_TYPE");

        var blacklistEntry = new TokenBlacklist
        {
            Id = TokenBlacklistId.New(),
            TokenHash = tokenHash,
            TokenType = tokenType,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            RevokedBy = revokedBy,
            Reason = reason
        };

        blacklistEntry.AddDomainEvent(new TokenBlacklisted(
            blacklistEntry.Id,
            tokenHash,
            tokenType,
            revokedBy,
            reason));

        return blacklistEntry;
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
