using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Security service for authentication and authorization operations (§16).
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Generates a JWT access token for the specified user (§7.1).
    /// </summary>
    Task<string> GenerateAccessTokenAsync(UserId userId, IList<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a refresh token (§8).
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and persists a refresh token, returning both the raw token and stored entity.
    /// </summary>
    Task<(string RawToken, RefreshToken Entity)> IssueRefreshTokenAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token and returns the user ID if valid (§7).
    /// </summary>
    Task<UserId?> ValidateAccessTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns the user ID if valid (§8).
    /// </summary>
    Task<UserId?> ValidateRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token (§8).
    /// </summary>
    Task RevokeRefreshTokenAsync(string tokenHash, string revokedBy, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a user (§5.4).
    /// </summary>
    Task RevokeAllUserSessionsAsync(UserId userId, string revokedBy, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a token is blacklisted (§8).
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Blacklists a token (§8).
    /// </summary>
    Task BlacklistTokenAsync(string tokenHash, string tokenType, DateTimeOffset expiresAt, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a cryptographically secure random string (§16).
    /// </summary>
    string GenerateSecureRandomString(int length = 32);

    /// <summary>
    /// Hashes a password using BCrypt (§6).
    /// </summary>
    Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a password against a hash (§6).
    /// </summary>
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes SHA256 hash of input string.
    /// </summary>
    string ComputeSha256Hash(string input);
}
