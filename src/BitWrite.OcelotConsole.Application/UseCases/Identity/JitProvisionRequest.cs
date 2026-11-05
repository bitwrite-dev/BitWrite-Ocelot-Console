using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.UseCases.Identity;

public record JitProvisionRequest(
    string Issuer,
    string Subject,
    string ProviderType,
    string? Username = null,
    string? DisplayName = null,
    string? Email = null
);

public record JitProvisionResponse(
    Guid UserId,
    string Username,
    string UserType,
    string Status,
    bool IsNewlyCreated,
    IReadOnlyList<string> AssignedRoles
);
