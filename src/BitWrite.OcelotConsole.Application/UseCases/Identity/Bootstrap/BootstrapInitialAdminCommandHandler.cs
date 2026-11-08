using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using RoleEntity = BitWrite.OcelotConsole.Domain.Aggregates.Role.Role;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity.Bootstrap;

/// <summary>
/// Safely creates the first initial administrator when IAM bootstrap has not completed.
/// </summary>
/// <remarks>
/// Bootstrap credentials are deployment-supplied (secure configuration/secret mechanism), never hardcoded.
/// The operation is protected for multi-instance startup by a distributed lock and a completion marker.
/// Secrets are never written to audit records, logs, or API responses.
/// </remarks>
public class BootstrapInitialAdminCommandHandler
{
    private const string BootstrapActor = "bootstrap";
    private const string DefaultAdminRoleName = "admin";
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(2);
    private const string BootstrapCompletedKey = "ocelot:iam:v1:bootstrap:completed";

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILocalCredentialRepository _credentialRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAtomicOperationService _atomic;
    private readonly IDistributedLock _lock;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly BootstrapInitialAdminOptions _options;

    public BootstrapInitialAdminCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILocalCredentialRepository credentialRepository,
        IPasswordHasher passwordHasher,
        IAtomicOperationService atomic,
        IDistributedLock distributedLock,
        IDomainEventDispatcher eventDispatcher,
        BootstrapInitialAdminOptions options)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _credentialRepository = credentialRepository;
        _passwordHasher = passwordHasher;
        _atomic = atomic;
        _lock = distributedLock;
        _eventDispatcher = eventDispatcher;
        _options = options;
    }

    public async Task<BootstrapInitialAdminResponse> HandleAsync(BootstrapInitialAdminCommand command, CancellationToken cancellationToken = default)
    {
        var lockResult = await _lock.AcquireAsync("iam:bootstrap", LockExpiry, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200), cancellationToken);
        if (!lockResult.Success || string.IsNullOrWhiteSpace(lockResult.LockId))
        {
            return new BootstrapInitialAdminResponse(false, false, "Bootstrap is in progress on another instance");
        }

        try
        {
            if (await IsCompletedAsync(cancellationToken))
            {
                return new BootstrapInitialAdminResponse(true, false, "Initial administrator bootstrap already completed");
            }

            // Safe-by-default: do nothing unless explicitly enabled with deployment-supplied secrets.
            if (!_options.Enabled)
            {
                await MarkCompletedAsync("disabled", cancellationToken);
                await AuditAsync(command, "Skipped", cancellationToken);
                return new BootstrapInitialAdminResponse(true, false, "Bootstrap disabled; no initial administrator created");
            }

            ValidateOptions(_options);

            var username = _options.Username!.Trim();
            var roleName = NormalizeRoleName(_options.RoleName);

            var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (existing is not null)
            {
                await EnsureAdminRoleAssignedAsync(existing, roleName, command.CorrelationId, cancellationToken);
                await EnsureCredentialExistsAsync(existing.Id, _options.Password!, cancellationToken);
                await MarkCompletedAsync(existing.Id.Value.ToString(), cancellationToken);
                await AuditAsync(command, "CompletedExisting", cancellationToken);
                return new BootstrapInitialAdminResponse(true, false, "Bootstrap completed using existing configured administrator user", existing.Id.Value, existing.Username);
            }

            var adminRole = await EnsureAdminRoleAsync(roleName, command.CorrelationId, cancellationToken);
            var userId = UserId.New();
            var user = User.CreateLocal(userId, username, _options.DisplayName, _options.Email, BootstrapActor, command.CorrelationId);
            user.AssignRole(adminRole.Name, command.CorrelationId);

            var hash = PasswordHash.From(await _passwordHasher.HashPasswordAsync(_options.Password!, cancellationToken), "PBKDF2-SHA256", 10000);
            var credential = LocalCredential.Create(userId, _options.Password!, hash, command.CorrelationId, mustChangePassword: true);

            await _userRepository.AddAsync(user, cancellationToken);
            await _credentialRepository.AddAsync(credential, cancellationToken);
            await MarkCompletedAsync(userId.Value.ToString(), cancellationToken);

            foreach (var domainEvent in user.DomainEvents.Concat(credential.DomainEvents))
            {
                await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
            user.ClearDomainEvents();
            credential.ClearDomainEvents();

            await AuditAsync(command, "Success", cancellationToken);
            return new BootstrapInitialAdminResponse(true, true, "Initial administrator bootstrap completed", userId.Value, username);
        }
        finally
        {
            await _lock.ReleaseAsync("iam:bootstrap", lockResult.LockId!, cancellationToken);
        }
    }

    private static void ValidateOptions(BootstrapInitialAdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Username))
            throw new DomainException("Bootstrap username must be supplied via secure configuration", "BOOTSTRAP_USERNAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(options.Password))
            throw new DomainException("Bootstrap password must be supplied via secure configuration", "BOOTSTRAP_PASSWORD_REQUIRED");
        if (options.Password.Length < 12)
            throw new DomainException("Bootstrap password must be at least 12 characters", "BOOTSTRAP_PASSWORD_TOO_WEAK");
        // Reject obviously unsafe/common bootstrap defaults without embedding a broad password blacklist.
        var p = options.Password;
        if (string.Equals(p, "administrator", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "changeme123!", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p, "Password123!", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Bootstrap password is too weak for initial administrator setup", "BOOTSTRAP_PASSWORD_TOO_WEAK");
    }

    private async Task<bool> IsCompletedAsync(CancellationToken cancellationToken)
    {
        var marker = await _atomic.GetAsync(BootstrapCompletedKey, cancellationToken);
        return !string.IsNullOrWhiteSpace(marker);
    }

    private async Task MarkCompletedAsync(string marker, CancellationToken cancellationToken)
    {
        await _atomic.SetIfNotExistsAsync(BootstrapCompletedKey, $"{DateTimeOffset.UtcNow:O}|{marker}", cancellationToken: cancellationToken);
    }

    private async Task<RoleEntity> EnsureAdminRoleAsync(string roleName, string correlationId, CancellationToken cancellationToken)
    {
        var existing = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (existing is not null)
            return existing;

        var role = RoleEntity.Create(RoleId.From(roleName), roleName, "Initial administrator bootstrap role", BootstrapActor, correlationId);
        await _roleRepository.AddAsync(role, cancellationToken);
        foreach (var domainEvent in role.DomainEvents)
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        role.ClearDomainEvents();
        return role;
    }

    private async Task EnsureAdminRoleAssignedAsync(User user, string roleName, string correlationId, CancellationToken cancellationToken)
    {
        var role = await EnsureAdminRoleAsync(roleName, correlationId, cancellationToken);
        if (user.RoleIds.Any(r => string.Equals(r, role.Name, StringComparison.OrdinalIgnoreCase)))
            return;
        user.AssignRole(role.Name, correlationId);
        await _userRepository.UpdateAsync(user, cancellationToken);
        foreach (var domainEvent in user.DomainEvents)
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        user.ClearDomainEvents();
    }

    private async Task EnsureCredentialExistsAsync(UserId userId, string password, CancellationToken cancellationToken)
    {
        var existing = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
            return;
        var hash = PasswordHash.From(await _passwordHasher.HashPasswordAsync(password, cancellationToken), "PBKDF2-SHA256", 10000);
        var credential = LocalCredential.Create(userId, password, hash, mustChangePassword: true);
        await _credentialRepository.AddAsync(credential, cancellationToken);
        credential.ClearDomainEvents();
    }

    private static string NormalizeRoleName(string? roleName)
    {
        var name = string.IsNullOrWhiteSpace(roleName) ? DefaultAdminRoleName : roleName.Trim().ToLowerInvariant();
        if (name.Length < 3)
            throw new DomainException("Bootstrap role name is invalid", "BOOTSTRAP_ROLE_INVALID");
        return name;
    }

    private Task AuditAsync(BootstrapInitialAdminCommand command, string result, CancellationToken cancellationToken)
    {
        // Never include passwords/secrets/tokens/hashes.
        return _eventDispatcher.DispatchAsync(
            new AuditRecorded(
                string.IsNullOrWhiteSpace(command.InitiatedBy) ? BootstrapActor : command.InitiatedBy,
                "BootstrapInitialAdmin",
                "User",
                "bootstrap",
                result).WithCorrelationId(command.CorrelationId),
            cancellationToken);
    }
}
