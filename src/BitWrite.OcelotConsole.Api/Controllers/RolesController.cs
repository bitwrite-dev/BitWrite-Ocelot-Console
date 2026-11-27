using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitWrite.OcelotConsole.Api.Controllers;

/// <summary>
/// Role administration endpoints (IAM-05). Permission mapping follows P-GATE-2.
/// </summary>
[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RolesController : BaseApiController
{
    private readonly CreateRoleCommandHandler _createRole;
    private readonly GetRoleQueryHandler _getRole;
    private readonly GetRolesQueryHandler _getRoles;
    private readonly UpdateRoleDescriptionCommandHandler _updateDescription;
    private readonly RenameRoleCommandHandler _renameRole;
    private readonly ActivateRoleCommandHandler _activateRole;
    private readonly DeactivateRoleCommandHandler _deactivateRole;
    private readonly AssignPermissionToRoleCommandHandler _assignPermission;
    private readonly RemovePermissionFromRoleCommandHandler _removePermission;

    public RolesController(
        CreateRoleCommandHandler createRole,
        GetRoleQueryHandler getRole,
        GetRolesQueryHandler getRoles,
        UpdateRoleDescriptionCommandHandler updateDescription,
        RenameRoleCommandHandler renameRole,
        ActivateRoleCommandHandler activateRole,
        DeactivateRoleCommandHandler deactivateRole,
        AssignPermissionToRoleCommandHandler assignPermission,
        RemovePermissionFromRoleCommandHandler removePermission)
    {
        _createRole = createRole;
        _getRole = getRole;
        _getRoles = getRoles;
        _updateDescription = updateDescription;
        _renameRole = renameRole;
        _activateRole = activateRole;
        _deactivateRole = deactivateRole;
        _assignPermission = assignPermission;
        _removePermission = removePermission;
    }

    [HttpGet]
    [Authorize(Policy = "perm:roles.read")]
    public async Task<ActionResult<List<RoleResponse>>> List(CancellationToken cancellationToken)
    {
        try
        {
            var roles = await _getRoles.HandleAsync(new GetRolesQuery(), cancellationToken);
            return HandleResult(roles);
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "perm:roles.read")]
    public async Task<ActionResult<RoleResponse>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var role = await _getRole.HandleAsync(new GetRoleQuery(RoleId.From(id)), cancellationToken);
            return HandleResult(role);
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<RoleResponse>> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateRoleCommand(request.Name, request.Description, InitiatedBy, CorrelationId);
            return HandleResult(await _createRole.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<RoleResponse>> Update(string id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var roleId = RoleId.From(id);
            var role = await _getRole.HandleAsync(new GetRoleQuery(roleId), cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Name) && !string.Equals(role.Name, request.Name, StringComparison.Ordinal))
            {
                role = await _renameRole.HandleAsync(
                    new RenameRoleCommand(roleId, request.Name, InitiatedBy, CorrelationId),
                    cancellationToken);
            }

            if (request.Description is not null && !string.Equals(role.Description, request.Description, StringComparison.Ordinal))
            {
                role = await _updateDescription.HandleAsync(
                    new UpdateRoleDescriptionCommand(roleId, request.Description, InitiatedBy, CorrelationId),
                    cancellationToken);
            }

            return HandleResult(role);
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/activate")]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<RoleResponse>> Activate(string id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ActivateRoleCommand(RoleId.From(id), InitiatedBy, CorrelationId);
            return HandleResult(await _activateRole.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<RoleResponse>> Deactivate(string id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeactivateRoleCommand(RoleId.From(id), InitiatedBy, CorrelationId);
            return HandleResult(await _deactivateRole.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/permissions")]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<AssignPermissionToRoleResponse>> AssignPermission(string id, [FromBody] RolePermissionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new AssignPermissionToRoleCommand(RoleId.From(id), request.PermissionCode, InitiatedBy, CorrelationId);
            return HandleResult(await _assignPermission.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpDelete("{id}/permissions/{permissionCode}")]
    [Authorize(Policy = "perm:roles.manage")]
    public async Task<ActionResult<RemovePermissionFromRoleResponse>> RemovePermission(string id, string permissionCode, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RemovePermissionFromRoleCommand(RoleId.From(id), permissionCode, InitiatedBy, CorrelationId);
            return HandleResult(await _removePermission.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    private string InitiatedBy =>
        User.Identity?.Name
        ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
        ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
        ?? "unattributed";
}

public record CreateRoleRequest(string Name, string? Description);
public record UpdateRoleRequest(string? Name, string? Description);
public record RolePermissionRequest(string PermissionCode);
