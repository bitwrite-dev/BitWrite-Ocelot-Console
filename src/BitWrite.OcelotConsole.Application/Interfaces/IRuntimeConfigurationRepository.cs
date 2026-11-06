using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces;

/// <summary>
/// Repository for runtime configuration stored in Redis.
/// Provides abstraction over Redis so the Application layer does not depend
/// directly on StackExchange.Redis.
/// </summary>
public interface IRuntimeConfigurationRepository
{
    /// <summary>
    /// Sets the current published version for an environment.
    /// </summary>
    Task SetCurrentVersionAsync(
        string environment,
        string version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current published version for an environment.
    /// </summary>
    Task<string?> GetCurrentVersionAsync(
        string environment,
        CancellationToken cancellationToken = default);
}
