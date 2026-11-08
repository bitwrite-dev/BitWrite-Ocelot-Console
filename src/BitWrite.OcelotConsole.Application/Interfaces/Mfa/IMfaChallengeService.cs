using BitWrite.OcelotConsole.Application.Contracts.Mfa;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces.Mfa;

/// <summary>
/// MFA challenge orchestration contract (T-16 readiness).
/// </summary>
/// <remarks>
/// Not implemented in v1. Future local login/JIT flows can request a challenge decision
/// before issuing BitWrite sessions/tokens, without redesigning authentication ownership.
/// </remarks>
public interface IMfaChallengeService
{
    Task<MfaChallengeResult> EvaluateAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<MfaChallengeResult> VerifyAsync(UserId userId, string challengeId, MfaFactorType factorType, string proof, CancellationToken cancellationToken = default);
}
