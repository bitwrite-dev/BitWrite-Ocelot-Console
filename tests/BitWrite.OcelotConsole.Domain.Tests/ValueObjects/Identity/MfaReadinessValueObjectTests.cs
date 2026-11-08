using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Xunit;

namespace BitWrite.OcelotConsole.Domain.Tests.ValueObjects.Identity;

public class MfaReadinessValueObjectTests
{
    [Fact]
    public void MfaFactorType_IncludesTotpAndRecoveryCode()
    {
        Enum.GetValues<MfaFactorType>()
            .Should().Contain([MfaFactorType.Totp, MfaFactorType.RecoveryCode]);
    }

    [Fact]
    public void MfaEnrollmentStatus_ModelsFutureEnrollmentLifecycle()
    {
        Enum.GetValues<MfaEnrollmentStatus>().Should().Contain([
            MfaEnrollmentStatus.NotEnrolled,
            MfaEnrollmentStatus.PendingVerification,
            MfaEnrollmentStatus.Enrolled,
            MfaEnrollmentStatus.Revoked
        ]);
    }

    [Fact]
    public void MfaChallengeStatus_ModelsChallengeOutcomes()
    {
        Enum.GetValues<MfaChallengeStatus>().Should().Contain([
            MfaChallengeStatus.NotRequired,
            MfaChallengeStatus.Required,
            MfaChallengeStatus.Satisfied,
            MfaChallengeStatus.Failed,
            MfaChallengeStatus.Expired
        ]);
    }
}
