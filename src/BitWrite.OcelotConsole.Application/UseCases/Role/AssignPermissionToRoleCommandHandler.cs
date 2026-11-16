using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class AssignPermissionToRoleCommandHandler
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IDistributedLock _distributedLock;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public AssignPermissionToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionCatalog permissionCatalog,
        IDistributedLock distributedLock,
        IDomainEventDispatcher eventDispatcher)
    {
        _roleRepository = roleRepository;
        _permissionCatalog = permissionCatalog;
        _distributedLock = distributedLock;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<AssignPermissionToRoleResponse> HandleAsync(
        AssignPermissionToRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = PermissionCode.Normalize(command.PermissionCode);
        var lookup = _permissionCatalog.Lookup(normalizedCode);

        if (lookup.IsUnknown)
            throw new DomainException($"Permission '{normalizedCode}' is not defined in catalog", "PERMISSION_NOT_FOUND");

        if (lookup.IsRetired || !lookup.IsAssignable)
            throw new DomainException($"Permission '{normalizedCode}' is not assignable", "PERMISSION_NOT_ASSIGNABLE");

        var lockResult = await _distributedLock.AcquireAsync(
            $"role:{command.RoleId.Value}:permissions",
            LockExpiry,
            waitTimeout: TimeSpan.FromSeconds(10),
            retryInterval: TimeSpan.FromMilliseconds(100),
            cancellationToken);

        if (!lockResult.Success || string.IsNullOrWhiteSpace(lockResult.LockId))
            throw new DomainException("Failed to acquire role permission lock", "ROLE_PERMISSION_LOCK_FAILED");

        try
        {
            var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
                ?? throw new DomainException($"Role {command.RoleId} not found", "ROLE_NOT_FOUND");

            if (role.Permissions.Contains(normalizedCode))
            {
                return new AssignPermissionToRoleResponse(
                    role.Id.Value,
                    normalizedCode,
                    Assigned: false,
                    AlreadyAssigned: true,
                    command.InitiatedBy,
                    DateTimeOffset.UtcNow);
            }

            role.AddPermission(normalizedCode, command.InitiatedBy, command.CorrelationId);
            await _roleRepository.UpdateAsync(role, cancellationToken);
            await _eventDispatcher.DispatchAsync(role.DomainEvents, cancellationToken);
            role.ClearDomainEvents();

            return new AssignPermissionToRoleResponse(
                role.Id.Value,
                normalizedCode,
                Assigned: true,
                AlreadyAssigned: false,
                command.InitiatedBy,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            await _distributedLock.ReleaseAsync($"role:{command.RoleId.Value}:permissions", lockResult.LockId!, cancellationToken);
        }
    }
}
