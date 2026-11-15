using BitWrite.OcelotConsole.Application.Interfaces;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Permissions;

public sealed class EffectivePermissionResolver : IEffectivePermissionResolver
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionCatalog _permissionCatalog;

    public EffectivePermissionResolver(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionCatalog permissionCatalog)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionCatalog = permissionCatalog;
    }

    public async Task<IReadOnlySet<string>> ResolveAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetAsync(userId, cancellationToken);
        if (user is null || !user.Status.IsActive)
            return Empty;

        var effective = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleRef in user.RoleIds)
        {
            if (string.IsNullOrWhiteSpace(roleRef))
                continue;

            RoleId roleId;
            try
            {
                roleId = RoleId.From(roleRef);
            }
            catch
            {
                continue;
            }

            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null || !role.IsActive)
                continue;

            foreach (var permissionRef in role.Permissions)
            {
                var lookup = _permissionCatalog.Lookup(permissionRef);
                if (lookup.IsActive)
                    effective.Add(PermissionCode.Normalize(permissionRef));
            }
        }

        return effective;
    }

    private static readonly IReadOnlySet<string> Empty = new HashSet<string>(StringComparer.Ordinal);
}
