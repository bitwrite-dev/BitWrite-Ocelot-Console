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
    private readonly IDomainEventDispatcher _eventDispatcher;

    public JitProvisionCommandHandler(
        IUserRepository userRepository,
        IExternalIdentityRepository externalIdentityRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _externalIdentityRepository = externalIdentityRepository;
        _eventDispatcher = eventDispatcher;
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

        // 2. Create new internal User (§5.2: automatic JIT provisioning)
        var userId = UserId.New();
        var username = string.IsNullOrWhiteSpace(request.Username)
            ? $"ext_{request.Issuer}_{request.Subject}"
            : request.Username;

        var user = User.CreateExternal(
            userId,
            username,
            request.DisplayName,
            request.Email,
            createdBy: "system", // System-initiated for JIT
            correlationId: Guid.NewGuid().ToString()
        );

        await _userRepository.AddAsync(user, cancellationToken);

        // 3. Create External Identity association (§7.4)
        var externalIdentity = ExternalIdentity.Create(
            userId,
            request.Issuer,
            request.Subject,
            request.ProviderType,
            correlationId: Guid.NewGuid().ToString()
        );

        await _externalIdentityRepository.AddAsync(externalIdentity, cancellationToken);

        // 4. Assign restricted default Role (§7.2: JIT gets restricted default Role)
        // Note: Role assignment logic is in T-13. Here we record the intent.
        // The default Role ID should come from configuration.
        var defaultRoleId = "restricted-user"; // Placeholder - configured in system settings
        user.AssignRole(defaultRoleId, Guid.NewGuid().ToString());

        await _userRepository.UpdateAsync(user, cancellationToken);

        // 5. Dispatch domain events
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
}
