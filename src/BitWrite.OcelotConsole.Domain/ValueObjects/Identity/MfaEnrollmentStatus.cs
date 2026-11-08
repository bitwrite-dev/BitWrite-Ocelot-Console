namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

/// <summary>
/// Enrollment lifecycle for an MFA factor (T-16 readiness; not implemented in v1).
/// </summary>
public enum MfaEnrollmentStatus
{
    NotEnrolled = 0,
    PendingVerification = 1,
    Enrolled = 2,
    Revoked = 3
}
