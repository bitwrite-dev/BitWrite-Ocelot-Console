using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

/// <summary>
/// Get user by ID (§5.2).
/// </summary>
public record GetUserQuery(
    UserId Id
);
