using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Service for password hashing and verification (§6).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure algorithm (e.g., BCrypt).
    /// </summary>
    Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a password against its hash.
    /// </summary>
    Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default);
}
