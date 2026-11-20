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

internal static class IamAudit
{
    public static Task EmitAsync(IDomainEventDispatcher dispatcher, DomainEvent source, string actor, string action, string resourceType, string resourceId, string result, CancellationToken ct)
        => dispatcher.DispatchAsync(new AuditRecorded(actor, action, resourceType, resourceId, result).WithCorrelationId(source.CorrelationId), ct);
}

public sealed class UserDisabledPropagationHandler : IDomainEventHandler<UserDisabled>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public UserDisabledPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(UserDisabled domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnUserDisabledAsync(domainEvent.UserId, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.DisabledBy, SecurityPropagationReasons.UserDisabled, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

public sealed class RoleDeactivatedPropagationHandler : IDomainEventHandler<RoleDeactivated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleDeactivatedPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleDeactivated domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleDeactivated, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.DeactivatedBy, SecurityPropagationReasons.RoleDeactivated, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class RoleActivatedPropagationHandler : IDomainEventHandler<RoleActivated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleActivatedPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleActivated domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleActivated, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.ActivatedBy, SecurityPropagationReasons.RoleActivated, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class RoleRenamedPropagationHandler : IDomainEventHandler<RoleRenamed>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleRenamedPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleRenamed domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleRenamed, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.UpdatedBy, SecurityPropagationReasons.RoleRenamed, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class RoleUpdatedPropagationHandler : IDomainEventHandler<RoleUpdated>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleUpdatedPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleUpdated domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.RoleUpdated, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.UpdatedBy, SecurityPropagationReasons.RoleUpdated, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class PermissionAddedToRolePropagationHandler : IDomainEventHandler<PermissionAddedToRole>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public PermissionAddedToRolePropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(PermissionAddedToRole domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.PermissionAdded, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.UpdatedBy, SecurityPropagationReasons.PermissionAdded, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class PermissionRemovedFromRolePropagationHandler : IDomainEventHandler<PermissionRemovedFromRole>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public PermissionRemovedFromRolePropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(PermissionRemovedFromRole domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnRoleChangedAsync(domainEvent.RoleId, SecurityPropagationReasons.PermissionRemoved, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.UpdatedBy, SecurityPropagationReasons.PermissionRemoved, "Role", domainEvent.RoleId.Value, "Success", cancellationToken);
    }
}

public sealed class RoleAssignedToUserPropagationHandler : IDomainEventHandler<RoleAssignedToUser>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleAssignedToUserPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleAssignedToUser domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnUserRoleAssignmentChangedAsync(domainEvent.UserId, RoleId.From(domainEvent.RoleId), SecurityPropagationReasons.RoleAssigned, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, "system", SecurityPropagationReasons.RoleAssigned, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

public sealed class RoleRemovedFromUserPropagationHandler : IDomainEventHandler<RoleRemovedFromUser>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public RoleRemovedFromUserPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(RoleRemovedFromUser domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnUserRoleAssignmentChangedAsync(domainEvent.UserId, RoleId.From(domainEvent.RoleId), SecurityPropagationReasons.RoleRemoved, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, "system", SecurityPropagationReasons.RoleRemoved, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

public sealed class LocalCredentialChangedPropagationHandler : IDomainEventHandler<LocalCredentialChanged>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public LocalCredentialChangedPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(LocalCredentialChanged domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialChanged, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.ChangedBy, SecurityPropagationReasons.CredentialChanged, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

public sealed class LocalCredentialResetPropagationHandler : IDomainEventHandler<LocalCredentialReset>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public LocalCredentialResetPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(LocalCredentialReset domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialReset, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, domainEvent.ResetBy, SecurityPropagationReasons.CredentialReset, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

public sealed class LocalCredentialLockedOutPropagationHandler : IDomainEventHandler<LocalCredentialLockedOut>
{
    private readonly IAuthorizationStatePropagationService _propagation;
    private readonly IDomainEventDispatcher _dispatcher;
    public LocalCredentialLockedOutPropagationHandler(IAuthorizationStatePropagationService propagation, IDomainEventDispatcher dispatcher)
    { _propagation = propagation; _dispatcher = dispatcher; }
    public async Task HandleAsync(LocalCredentialLockedOut domainEvent, CancellationToken cancellationToken = default)
    {
        await _propagation.OnCredentialOrSecurityStateChangedAsync(domainEvent.UserId, SecurityPropagationReasons.CredentialLockedOut, cancellationToken);
        await IamAudit.EmitAsync(_dispatcher, domainEvent, "system", SecurityPropagationReasons.CredentialLockedOut, "User", domainEvent.UserId.Value.ToString(), "Success", cancellationToken);
    }
}

