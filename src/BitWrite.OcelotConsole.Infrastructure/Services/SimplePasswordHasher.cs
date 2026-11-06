using System.Security.Cryptography;
using System.Text;
using BitWrite.OcelotConsole.Application.Interfaces;

namespace BitWrite.OcelotConsole.Infrastructure.Security;

/// <summary>
/// Simple password hasher using SHA-256 with a random salt.
/// </summary>
/// <remarks>
/// This is a development implementation. Production should use BCrypt or Argon2.
/// </summary>
public class SimplePasswordHasher : IPasswordHasher
{
    public Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        var salt = GenerateSalt();
        var hash = ComputeHash(password, salt);
        return Task.FromResult($"{salt}:{hash}");
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        var parts = hashedPassword.Split(':');
        if (parts.Length != 2)
            return false;

        var salt = parts[0];
        var storedHash = parts[1];
        var computedHash = ComputeHash(password, salt);

        return computedHash == storedHash;
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string ComputeHash(string password, string salt)
    {
        var combined = password + salt;
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
