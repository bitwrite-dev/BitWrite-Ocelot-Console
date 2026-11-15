using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Permissions;

public interface IEffectivePermissionResolver
{
    Task<IReadOnlySet<string>> ResolveAsync(UserId userId, CancellationToken cancellationToken = default);
}
