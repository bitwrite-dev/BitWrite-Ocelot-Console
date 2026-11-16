using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Infrastructure.EventHandlers;

internal static class SecurityPropagationReasons
{
    public const string UserDisabled = "UserDisabled";
    public const string RoleDeactivated = "RoleDeactivated";
    public const string RoleActivated = "RoleActivated";
    public const string RoleRenamed = "RoleRenamed";
    public const string RoleUpdated = "RoleUpdated";
    public const string RoleAssigned = "RoleAssignedToUser";
    public const string RoleRemoved = "RoleRemovedFromUser";
    public const string PermissionAdded = "PermissionAddedToRole";
    public const string PermissionRemoved = "PermissionRemovedFromRole";
    public const string CredentialChanged = "LocalCredentialChanged";
    public const string CredentialReset = "LocalCredentialReset";
    public const string CredentialLockedOut = "LocalCredentialLockedOut";
}

public sealed class UserDisabledPropagationHandler : IDomainEventHandler<UserDisabled>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public UserDisabledPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(UserDisabled domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnUserDisabledAsync(domainEvent.UserId, cancellationToken);
}

public sealed class RoleDeactivatedPropagationHandler : IDomainEventHandler<RoleDeactivated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleDeactivatedPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleDeactivated domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleDeactivated, cancellationToken);
}

public sealed class RoleActivatedPropagationHandler : IDomainEventHandler<RoleActivated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleActivatedPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleActivated domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleActivated, cancellationToken);
}

public sealed class RoleRenamedPropagationHandler : IDomainEventHandler<RoleRenamed>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleRenamedPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleRenamed domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleRenamed, cancellationToken);
}

public sealed class RoleUpdatedPropagationHandler : IDomainEventHandler<RoleUpdated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleUpdatedPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleUpdated domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleUpdated, cancellationToken);
}

public sealed class PermissionAddedToRolePropagationHandler : IDomainEventHandler<PermissionAddedToRole>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public PermissionAddedToRolePropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(PermissionAddedToRole domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.PermissionAdded, cancellationToken);
}

public sealed class PermissionRemovedFromRolePropagationHandler : IDomainEventHandler<PermissionRemovedFromRole>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public PermissionRemovedFromRolePropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(PermissionRemovedFromRole domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.PermissionRemoved, cancellationToken);
}

public sealed class RoleAssignedToUserPropagationHandler : IDomainEventHandler<RoleAssignedToUser>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleAssignedToUserPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleAssignedToUser domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnUserRoleAssignmentChangedAsync(domainEvent.UserId, RoleId.From(domainEvent.RoleId), SecurityPropagationReasons.RoleAssigned, cancellationToken);
}

public sealed class RoleRemovedFromUserPropagationHandler : IDomainEventHandler<RoleRemovedFromUser>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public RoleRemovedFromUserPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(RoleRemovedFromUser domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnUserRoleAssignmentChangedAsync(domainEvent.UserId, RoleId.From(domainEvent.RoleId), SecurityPropagationReasons.RoleRemoved, cancellationToken);
}

public sealed class LocalCredentialChangedPropagationHandler : IDomainEventHandler<LocalCredentialChanged>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public LocalCredentialChangedPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(LocalCredentialChanged domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialChanged, cancellationToken);
}

public sealed class LocalCredentialResetPropagationHandler : IDomainEventHandler<LocalCredentialReset>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public LocalCredentialResetPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(LocalCredentialReset domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialReset, cancellationToken);
}

public sealed class LocalCredentialLockedOutPropagationHandler : IDomainEventHandler<LocalCredentialLockedOut>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    public LocalCredentialLockedOutPropagationHandler(IAuthorizationStatePropagationService propagation) => _propagation = propagation;
    public Task HandleAsync(LocalCredentialLockedOut domainEvent, CancellationToken cancellationToken = default) =>
        _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialLockedOut, cancellationToken);
}
