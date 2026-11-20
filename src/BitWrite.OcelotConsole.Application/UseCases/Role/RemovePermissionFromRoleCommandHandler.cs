using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.Permissions;
using BitWrite.OcelotConsole.Domain.Events;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class RemovePermissionFromRoleCommandHandler
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

    private readonly IRoleRepository _roleRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public RemovePermissionFromRoleCommandHandler(
        IRoleRepository roleRepository,
        IDistributedLock distributedLock,
        IDomainEventDispatcher eventDispatcher)
    {
        _roleRepository = roleRepository;
        _distributedLock = distributedLock;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RemovePermissionFromRoleResponse> HandleAsync(
        RemovePermissionFromRoleCommand command,
        CancellationToken cancellationToken = default)
    {
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

            var normalizedCode = PermissionCode.Normalize(command.PermissionCode);
            role.RemovePermission(normalizedCode, command.InitiatedBy, command.CorrelationId);
            await _roleRepository.UpdateAsync(role, cancellationToken);
            await _eventDispatcher.DispatchAsync(role.DomainEvents, cancellationToken);
            role.ClearDomainEvents();
            await _eventDispatcher.DispatchAsync(new AuditRecorded(command.InitiatedBy, "RemovePermissionFromRole", "Role", role.Id.Value, "Success").WithCorrelationId(command.CorrelationId), cancellationToken);

            return new RemovePermissionFromRoleResponse(
                role.Id.Value,
                normalizedCode,
                command.InitiatedBy,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            await _distributedLock.ReleaseAsync($"role:{command.RoleId.Value}:permissions", lockResult.LockId!, cancellationToken);
        }
    }
}
