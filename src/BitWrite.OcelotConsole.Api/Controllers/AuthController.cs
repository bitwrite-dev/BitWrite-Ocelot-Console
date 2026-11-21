using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitWrite.OcelotConsole.Api.Controllers;

/// <summary>
/// Authentication endpoints: login, logout.
///
/// These are the only unauthenticated entry points into the control plane.
/// Everything else requires a bearer token issued here.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly ILocalCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityService _securityService;

    public AuthController(
        IUserRepository userRepository,
        ILocalCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        ISecurityService securityService)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _securityService = securityService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || !user.Status.IsActive)
        {
            return Unauthorized(new
            {
                correlationId = CorrelationId,
                error = "Invalid username or password",
                type = "InvalidCredentials",
            });
        }

        var credential = await _credentialRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (credential is null)
        {
            return Unauthorized(new
            {
                correlationId = CorrelationId,
                error = "No local credential for this user",
                type = "NoCredential",
            });
        }

        if (credential.PasswordHash is null)
        {
            return Unauthorized(new
            {
                correlationId = CorrelationId,
                error = "Credential is corrupted",
                type = "CorruptedCredential",
            });
        }

        if (credential.IsLockedOut)
        {
            return Unauthorized(new
            {
                correlationId = CorrelationId,
                error = "Account is locked out",
                type = "AccountLocked",
            });
        }

        var passwordValid = await _passwordHasher.VerifyPasswordAsync(
            request.Password, credential.PasswordHash.Value, cancellationToken);
        if (!passwordValid)
        {
            credential.RecordFailedAuthentication();
            await _credentialRepository.UpdateAsync(credential, cancellationToken);
            return Unauthorized(new
            {
                correlationId = CorrelationId,
                error = "Invalid username or password",
                type = "InvalidCredentials",
            });
        }

        credential.RecordSuccessfulAuthentication();
        await _credentialRepository.UpdateAsync(credential, cancellationToken);

        var accessToken = await _securityService.GenerateAccessTokenAsync(user.Id, cancellationToken: cancellationToken);
        var (rawRefreshToken, _) = await _securityService.IssueRefreshTokenAsync(user.Id, cancellationToken);

        return Ok(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            ExpiresIn: 15 * 60, // 15 minutes in seconds
            UserId: user.Id.Value.ToString(),
            Username: user.Username,
            DisplayName: user.DisplayName,
            Email: user.Email));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = UserId.From(User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!.Value);
        await _securityService.RevokeAllUserSessionsAsync(userId, "user-logout", "User logged out", cancellationToken);
        return Ok();
    }
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string UserId,
    string Username,
    string? DisplayName,
    string? Email);
