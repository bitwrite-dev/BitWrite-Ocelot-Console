using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Contracts.Mfa;

/// <summary>
/// Authentication step result that can represent either completion or an MFA challenge.
/// </summary>
/// <remarks>
/// T-16 readiness: v1 does not issue MFA challenges, but future login/token issuance flows
/// can return this shape without breaking the architecture.
/// </remarks>
public record MfaChallengeResult(
    MfaChallengeStatus Status,
    UserId? UserId = null,
    string? ChallengeId = null,
    IReadOnlyList<MfaFactorType>? AvailableFactors = null,
    DateTimeOffset? ExpiresAt = null,
    string? FailureReason = null)
{
    public static MfaChallengeResult NotRequired(UserId userId) => new(MfaChallengeStatus.NotRequired, userId);

    public static MfaChallengeResult Required(
        UserId userId,
        string challengeId,
        IReadOnlyList<MfaFactorType> availableFactors,
        DateTimeOffset expiresAt)
        => new(MfaChallengeStatus.Required, userId, challengeId, availableFactors, expiresAt);

    public static MfaChallengeResult Satisfied(UserId userId) => new(MfaChallengeStatus.Satisfied, userId);
}
