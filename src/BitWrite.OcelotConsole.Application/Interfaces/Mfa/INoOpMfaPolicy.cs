using BitWrite.OcelotConsole.Application.Contracts.Mfa;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces.Mfa;

/// <summary>
/// Explicit v1 policy marker: MFA is architecture-ready but not enforced/implemented.
/// </summary>
public interface INoOpMfaPolicy
{
    Task<MfaChallengeResult> EvaluateAsync(UserId userId, CancellationToken cancellationToken = default);
}
