using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Aggregates.User;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public class JitProvisionCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalIdentityRepository _externalIdentityRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly JitProvisionOptions _options;

    public JitProvisionCommandHandler(
        IUserRepository userRepository,
        IExternalIdentityRepository externalIdentityRepository,
        IRoleRepository roleRepository,
        IDomainEventDispatcher eventDispatcher,
        JitProvisionOptions options)
    {
        _userRepository = userRepository;
        _externalIdentityRepository = externalIdentityRepository;
        _roleRepository = roleRepository;
        _eventDispatcher = eventDispatcher;
        _options = options ?? new JitProvisionOptions();
    }

    /// <summary>
    /// Provisions a new internal User for an externally authenticated identity (§5.2 JIT).
    /// </summary>
    public async Task<JitProvisionResponse> HandleAsync(
        JitProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check if external identity already exists (§7.4: identified by Issuer + Subject)
        var existingIdentity = await _externalIdentityRepository.GetByIssuerAndSubjectAsync(
            request.Issuer, request.Subject, cancellationToken);

        if (existingIdentity != null)
        {
            // Identity already mapped - return existing user info
            var existingUser = await _userRepository.GetAsync(existingIdentity.UserId, cancellationToken)
                ?? throw new DomainException(
                    $"External identity maps to non-existent user {existingIdentity.UserId}",
                    "EXTERNAL_IDENTITY_ORPHANED");

            return new JitProvisionResponse(
                existingUser.Id.Value,
                existingUser.Username,
                existingUser.UserType,
                existingUser.Status.Value,
                IsNewlyCreated: false,
                existingUser.RoleIds
            );
        }

        // 2. Resolve and validate configured restricted default role before creating any state.
        var defaultRole = await ResolveConfiguredDefaultRoleAsync(cancellationToken);

        // 3. Create new internal User (§5.2: automatic JIT provisioning)
        var userId = UserId.New();
        var username = string.IsNullOrWhiteSpace(request.Username)
            ? $"ext_{request.Issuer}_{request.Subject}"
            : request.Username;

        var user = User.CreateExternal(
            userId,
            username,
            request.DisplayName,
            request.Email,
            createdBy: "system",
            correlationId: Guid.NewGuid().ToString()
        );

        // 4. Assign restricted default Role by configured RoleId (§7.2)
        user.AssignRole(defaultRole.Id, Guid.NewGuid().ToString());

        await _userRepository.AddAsync(user, cancellationToken);

        // 5. Create External Identity association (§7.4)
        var externalIdentity = ExternalIdentity.Create(
            userId,
            request.Issuer,
            request.Subject,
            request.ProviderType,
            correlationId: Guid.NewGuid().ToString()
        );

        await _externalIdentityRepository.AddAsync(externalIdentity, cancellationToken);

        // 6. Dispatch domain events
        foreach (var domainEvent in user.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        user.ClearDomainEvents();

        foreach (var domainEvent in externalIdentity.DomainEvents)
        {
            await _eventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }
        externalIdentity.ClearDomainEvents();

        return new JitProvisionResponse(
            userId.Value,
            user.Username,
            user.UserType,
            user.Status.Value,
            IsNewlyCreated: true,
            user.RoleIds
        );
    }

    private async Task<BitWrite.OcelotConsole.Domain.Aggregates.Role.Role> ResolveConfiguredDefaultRoleAsync(CancellationToken cancellationToken)
    {
        var configured = _options.DefaultRoleId?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
            throw new DomainException("JIT default role is not configured", "JIT_DEFAULT_ROLE_NOT_CONFIGURED");

        if (!Guid.TryParse(configured, out _))
            throw new DomainException("Configured JIT default role identifier is invalid", "JIT_DEFAULT_ROLE_INVALID");

        var roleId = RoleId.From(configured);

        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null)
            throw new DomainException("Configured JIT default role was not found", "JIT_DEFAULT_ROLE_NOT_FOUND");

        if (!role.IsActive)
            throw new DomainException("Configured JIT default role is not active", "JIT_DEFAULT_ROLE_INACTIVE");

        return role;
    }
}
