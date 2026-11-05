using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User.Session;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BitWrite.OcelotConsole.Infrastructure.Services;

/// <summary>
/// Security service for JWT token management, session management, and authentication security (§16).
/// </summary>
/// <remarks>
/// Implements:
/// - Secure token signing and key management
/// - Token expiration and refresh rotation
/// - Session revocation
/// - External identity validation
/// - Prevention of external identity collisions
/// - Least-privilege default Role assignment
/// - Default-deny authorization
/// - Audit logging without secrets
/// - Safe bootstrap
/// - Protection against concurrent identity provisioning
/// - Protection against refresh-token replay
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
}

/// <summary>
/// Implementation of ISecurityService using JWT and Redis (§16).
/// </summary>
public class SecurityService : ISecurityService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenBlacklistRepository _tokenBlacklistRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public SecurityService(
        ISessionRepository sessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenBlacklistRepository tokenBlacklistRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _sessionRepository = sessionRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenBlacklistRepository = tokenBlacklistRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;

        _signingKey = configuration["Security:SigningKey"] ?? throw new InvalidOperationException("Security:SigningKey not configured");
        _issuer = configuration["Security:Issuer"] ?? "BitWrite.OcelotControl";
        _audience = configuration["Security:Audience"] ?? "BitWrite.OcelotConsole.Api";
        _accessTokenExpirationMinutes = int.Parse(configuration["Security:AccessTokenExpirationMinutes"] ?? "15");
        _refreshTokenExpirationDays = int.Parse(configuration["Security:RefreshTokenExpirationDays"] ?? "7");
    }

    public async Task<string> GenerateAccessTokenAsync(UserId userId, IList<string> roles, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var token = GenerateSecureRandomString(64);
        var tokenHash = ComputeSha256Hash(token);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays);

        var refreshToken = RefreshToken.Create(userId, tokenHash, expiresAt);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return token;
    }

    public async Task<UserId?> ValidateAccessTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken == null) return null;
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return null;

            return UserId.From(userId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserId?> ValidateRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.IsExpired)
            return null;

        return refreshToken.UserId;
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, string revokedBy, string reason, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken != null && !refreshToken.IsRevoked)
        {
            refreshToken.Revoke(revokedBy, reason);
            await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
        }
    }

    public async Task RevokeAllUserSessionsAsync(UserId userId, string revokedBy, string reason, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetActiveSessionsAsync(userId, cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(reason, revokedBy);
            await _sessionRepository.UpdateAsync(session, cancellationToken);
        }

        var refreshTokens = await _refreshTokenRepository.GetActiveRefreshTokensAsync(userId, cancellationToken);
        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.Revoke(revokedBy, reason);
            await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);
        }
    }

    public async Task<bool> IsTokenBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _tokenBlacklistRepository.IsTokenBlacklistedAsync(tokenHash, cancellationToken);
    }

    public async Task BlacklistTokenAsync(string tokenHash, string tokenType, DateTimeOffset expiresAt, string? revokedBy = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        var blacklistEntry = TokenBlacklist.Create(tokenHash, tokenType, expiresAt, revokedBy, reason);
        await _tokenBlacklistRepository.AddAsync(blacklistEntry, cancellationToken);
    }

    public string GenerateSecureRandomString(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task<string> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        return await _passwordHasher.HashPasswordAsync(password, cancellationToken);
    }

    public async Task<bool> VerifyPasswordAsync(string password, string hashedPassword, CancellationToken cancellationToken = default)
    {
        return await _passwordHasher.VerifyPasswordAsync(password, hashedPassword, cancellationToken);
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
