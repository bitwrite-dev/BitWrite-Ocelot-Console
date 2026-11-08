namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

/// <summary>
/// MFA factor types anticipated by IAM architecture (T-16).
/// </summary>
/// <remarks>
/// TOTP is the primary readiness target, but the model must not block future factor types.
/// </remarks>
public enum MfaFactorType
{
    Totp = 1,
    RecoveryCode = 2
}
