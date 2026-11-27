using BitWrite.OcelotConsole.Application.UseCases.Identity;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitWrite.OcelotConsole.Api.Controllers;

/// <summary>
/// User administration endpoints (IAM-15). Protected; endpoint-to-permission mapping gated (P-GATE-1/2).
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly CreateUserCommandHandler _createUser;
    private readonly GetUserQueryHandler _getUser;
    private readonly ListUsersQueryHandler _listUsers;
    private readonly UpdateUserCommandHandler _updateUser;
    private readonly EnableUserCommandHandler _enableUser;
    private readonly DisableUserCommandHandler _disableUser;
    private readonly AssignRoleCommandHandler _assignRole;
    private readonly RemoveRoleCommandHandler _removeRole;

    public UsersController(
        CreateUserCommandHandler createUser,
        GetUserQueryHandler getUser,
        ListUsersQueryHandler listUsers,
        UpdateUserCommandHandler updateUser,
        EnableUserCommandHandler enableUser,
        DisableUserCommandHandler disableUser,
        AssignRoleCommandHandler assignRole,
        RemoveRoleCommandHandler removeRole)
    {
        _createUser = createUser;
        _getUser = getUser;
        _listUsers = listUsers;
        _updateUser = updateUser;
        _enableUser = enableUser;
        _disableUser = disableUser;
        _assignRole = assignRole;
        _removeRole = removeRole;
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateUserCommand(request.Username, request.DisplayName, request.Email, request.Password, InitiatedBy, CorrelationId);
            return HandleResult(await _createUser.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet]
    public async Task<ActionResult<ListUsersResponse>> List(CancellationToken cancellationToken)
    {
        try
        {
            return HandleResult(await _listUsers.HandleAsync(new ListUsersQuery(), cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> Get(string id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getUser.HandleAsync(new GetUserQuery(UserId.From(id)), cancellationToken);
            if (result is null) return NotFound();
            return HandleResult(result);
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateUserCommand(UserId.From(id), request.DisplayName, request.Email, InitiatedBy, CorrelationId);
            return HandleResult(await _updateUser.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/enable")]
    public async Task<ActionResult<UserResponse>> Enable(string id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new EnableUserCommand(UserId.From(id), InitiatedBy, CorrelationId);
            return HandleResult(await _enableUser.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/disable")]
    public async Task<ActionResult<UserResponse>> Disable(string id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DisableUserCommand(UserId.From(id), InitiatedBy, CorrelationId);
            return HandleResult(await _disableUser.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpPost("{id}/roles")]
    public async Task<ActionResult<RoleAssignmentResponse>> AssignRole(string id, [FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new AssignRoleCommand(UserId.From(id), RoleId.From(request.RoleId), InitiatedBy, CorrelationId);
            return HandleResult(await _assignRole.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    [HttpDelete("{id}/roles/{roleId}")]
    public async Task<ActionResult<RoleAssignmentResponse>> RemoveRole(string id, string roleId, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RemoveRoleCommand(UserId.From(id), RoleId.From(roleId), InitiatedBy, CorrelationId);
            return HandleResult(await _removeRole.HandleAsync(command, cancellationToken));
        }
        catch (Exception ex) { return HandleError(ex); }
    }

    private string InitiatedBy =>
        User.Identity?.Name
        ?? User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
        ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
        ?? "unattributed";
}

public record CreateUserRequest(string Username, string? DisplayName, string? Email, string? Password);
public record UpdateUserRequest(string? DisplayName, string? Email);
public record AssignUserRoleRequest(string RoleId);
