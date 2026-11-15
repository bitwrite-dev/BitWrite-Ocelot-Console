using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class GetRoleQueryHandler
{
    private readonly IRoleRepository _roleRepository;

    public GetRoleQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponse> HandleAsync(GetRoleQuery query, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(query.RoleId, cancellationToken)
            ?? throw new DomainException($"Role {query.RoleId} not found", "ROLE_NOT_FOUND");
        return RoleMapping.ToResponse(role);
    }
}
