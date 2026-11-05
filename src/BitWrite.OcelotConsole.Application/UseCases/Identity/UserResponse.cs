using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <param name="LastAuthenticatedAt">
/// When the user last successfully authenticated, or null if never.
/// </param>
public record UserResponse(
    Guid Id,
    string Username,
    string? DisplayName,
    string? Email,
    string Status,
    string UserType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null,
    DateTimeOffset? LastAuthenticatedAt = null,
    IReadOnlyList<string>? RoleIds = null
);
