using BitWrite.OcelotConsole.Application.Interfaces;

namespace BitWrite.OcelotConsole.Application.UseCases.Role;

public class GetRolesQueryHandler
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<RoleResponse>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return roles.Select(RoleMapping.ToResponse).ToList();
    }
}
