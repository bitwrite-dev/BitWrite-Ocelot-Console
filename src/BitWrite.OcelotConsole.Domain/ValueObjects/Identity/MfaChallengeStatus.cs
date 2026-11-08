namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

/// <summary>
/// Outcome of an MFA challenge step (T-16 readiness).
/// </summary>
public enum MfaChallengeStatus
{
    NotRequired = 0,
    Required = 1,
    Satisfied = 2,
    Failed = 3,
    Expired = 4
}
