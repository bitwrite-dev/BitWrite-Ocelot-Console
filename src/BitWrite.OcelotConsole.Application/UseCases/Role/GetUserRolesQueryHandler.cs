using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Application.UseCases.Role;
using BitWrite.OcelotConsole.Domain.Exceptions;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class GetUserRolesQueryHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public GetUserRolesQueryHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleAssignmentResponse>> HandleAsync(GetUserRolesQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(query.UserId, cancellationToken);
        if (user == null)
            throw new DomainException($"User {query.UserId} not found", "USER_NOT_FOUND");

        var roles = new List<RoleAssignmentResponse>();

        foreach (var roleIdString in user.RoleIds)
        {
            var roleId = RoleId.From(roleIdString);
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role != null)
            {
                roles.Add(new RoleAssignmentResponse(
                    user.Id.Value,
                    role.Id.Value,
                    role.Name,
                    "System",
                    DateTimeOffset.MinValue));
            }
        }

        return roles;
    }
}
