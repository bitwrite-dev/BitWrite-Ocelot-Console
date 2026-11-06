using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

/// <summary>
/// Gets all roles assigned to a user (§6.2).
/// </summary>
public record GetUserRolesQuery(
    UserId UserId);
