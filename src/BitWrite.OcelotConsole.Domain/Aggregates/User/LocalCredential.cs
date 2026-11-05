using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Domain.Aggregates.User;

/// <summary>
/// LocalCredential Aggregate Root - Manages local authentication credentials (§6).
/// </summary>
/// <remarks>
/// Credential data is logically separated from general User profile data (§6).
/// Passwords are never stored in plaintext or recoverable form (§15).
/// </summary>
public sealed class LocalCredential
{
    private readonly List<DomainEvent> _domainEvents = new();

    public UserId UserId { get; private set; } = null!;
    public PasswordHash PasswordHash { get; private set; } = null!;
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedOutUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastChangedAt { get; private set; }
    public DateTimeOffset? LastAuthenticatedAt { get; private set; }

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private LocalCredential() { }

    /// <summary>
    /// Creates a new local credential with a hashed password (§6).
    /// </summary>
    public static LocalCredential Create(
        UserId userId,
        string plainTextPassword,
        PasswordHash passwordHash,
        string correlationId = "")
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new DomainException("Plain text password cannot be empty", "INVALID_PASSWORD");

        if (plainTextPassword.Length < 8)
            throw new DomainException("Password must be at least 8 characters", "PASSWORD_TOO_SHORT");

        var credential = new LocalCredential
        {
            UserId = userId,
            PasswordHash = passwordHash,
            FailedLoginAttempts = 0,
            LockedOutUntil = null,
            CreatedAt = DateTimeOffset.UtcNow,
            LastAuthenticatedAt = null
        };

        credential.AddDomainEvent(new LocalCredentialCreated(userId, correlationId));
        return credential;
    }

    /// <summary>
    /// Changes the password (§6).
    /// </summary>
    public void ChangePassword(
        PasswordHash newPasswordHash,
        string changedBy,
        string correlationId = "")
    {
        if (newPasswordHash == null)
            throw new DomainException("Password hash cannot be null", "INVALID_PASSWORD_HASH");

        PasswordHash = newPasswordHash;
        LastChangedAt = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;

        AddDomainEvent(new LocalCredentialChanged(UserId, changedBy, correlationId));
    }

    /// <summary>
    /// Records a successful authentication (§6).
    /// </summary>
    public void RecordSuccessfulAuthentication()
    {
        LastAuthenticatedAt = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;

        AddDomainEvent(new LocalCredentialAuthenticated(UserId));
    }

    /// <summary>
    /// Records a failed authentication attempt (§15).
    /// </summary>
    public void RecordFailedAuthentication(int maxFailedAttempts = 5, int lockoutMinutes = 30)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxFailedAttempts)
        {
            LockedOutUntil = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
        }

        AddDomainEvent(new LocalCredentialAuthenticationFailed(UserId, FailedLoginAttempts));
    }

    /// <summary>
    /// Resets the password after a lockout or forgotten password (§6).
    /// </summary>
    public void ResetPassword(
        PasswordHash newPasswordHash,
        string resetBy,
        string correlationId = "")
    {
        if (newPasswordHash == null)
            throw new DomainException("Password hash cannot be null", "INVALID_PASSWORD_HASH");

        PasswordHash = newPasswordHash;
        LastChangedAt = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;

        AddDomainEvent(new LocalCredentialReset(UserId, resetBy, correlationId));
    }

    /// <summary>
    /// Checks if the credential is locked out (§6).
    /// </summary>
    public bool IsLockedOut =>
        LockedOutUntil.HasValue && LockedOutUntil > DateTimeOffset.UtcNow;

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
