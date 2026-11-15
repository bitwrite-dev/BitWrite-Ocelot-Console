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
        var changed = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleRef in user.RoleIds.ToList())
        {
            if (string.IsNullOrWhiteSpace(roleRef))
                continue;

            var resolved = await ResolveRoleAsync(roleRef, cancellationToken);
            if (resolved is null)
                continue;

            var (role, resolvedId, isLegacyRef) = resolved.Value;
            if (!seen.Add(resolvedId.Value))
                continue;

            roles.Add(new RoleAssignmentResponse(
                user.Id.Value,
                role.Id.Value,
                role.Name,
                "System",
                DateTimeOffset.MinValue));

            if (isLegacyRef)
            {
                try
                {
                    if (user.RoleIds.Contains(roleRef))
                        user.RemoveRole(RoleId.From(roleRef));
                }
                catch
                {
                    // best-effort cleanup of unmappable/legacy ref shape
                }

                if (!user.RoleIds.Contains(resolvedId.Value))
                {
                    user.AssignRole(resolvedId);
                    changed = true;
                }
                else
                {
                    changed = true;
                }
            }
        }

        if (changed)
            await _userRepository.UpdateAsync(user, cancellationToken);

        return roles;
    }

    private async Task<(Domain.Aggregates.Role.Role Role, RoleId ResolvedId, bool IsLegacyRef)?> ResolveRoleAsync(
        string roleRef,
        CancellationToken cancellationToken)
    {
        // Try canonical RoleId first.
        try
        {
            var asId = RoleId.From(roleRef);
            var byId = await _roleRepository.GetByIdAsync(asId, cancellationToken);
            if (byId is not null)
                return (byId, asId, false);
        }
        catch
        {
            // not a valid RoleId form; fall through to name lookup
        }

        var mappedId = await _roleRepository.GetIdByNameAsync(roleRef, cancellationToken);
        if (mappedId is null)
            return null;

        var role = await _roleRepository.GetByIdAsync(mappedId, cancellationToken);
        if (role is null)
            return null;

        return (role, mappedId, true);
    }
}
