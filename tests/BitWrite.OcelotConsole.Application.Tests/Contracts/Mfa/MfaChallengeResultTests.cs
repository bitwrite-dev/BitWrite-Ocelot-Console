using BitWrite.OcelotConsole.Application.Contracts.Mfa;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;
using FluentAssertions;
using Xunit;

namespace BitWrite.OcelotConsole.Application.Tests.Contracts.Mfa;

public class MfaChallengeResultTests
{
    [Fact]
    public void NotRequired_RepresentsV1Behavior()
    {
        var userId = UserId.New();

        var result = MfaChallengeResult.NotRequired(userId);

        result.Status.Should().Be(MfaChallengeStatus.NotRequired);
        result.UserId.Should().Be(userId);
        result.ChallengeId.Should().BeNull();
        result.AvailableFactors.Should().BeNull();
    }

    [Fact]
    public void Required_CarriesChallengeMetadataWithoutSecrets()
    {
        var userId = UserId.New();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        var result = MfaChallengeResult.Required(userId, "challenge-1", [MfaFactorType.Totp, MfaFactorType.RecoveryCode], expiresAt);

        result.Status.Should().Be(MfaChallengeStatus.Required);
        result.UserId.Should().Be(userId);
        result.ChallengeId.Should().Be("challenge-1");
        result.AvailableFactors.Should().ContainInOrder(MfaFactorType.Totp, MfaFactorType.RecoveryCode);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void EnrollmentDescriptor_IsMetadataOnly()
    {
        var userId = UserId.New();
        var descriptor = new MfaEnrollmentDescriptor(
            EnrollmentId: "enroll-1",
            UserId: userId,
            FactorType: MfaFactorType.Totp,
            Status: MfaEnrollmentStatus.PendingVerification,
            CreatedAt: DateTimeOffset.UtcNow,
            Label: "Authenticator app");

        descriptor.EnrollmentId.Should().Be("enroll-1");
        descriptor.FactorType.Should().Be(MfaFactorType.Totp);
        descriptor.Status.Should().Be(MfaEnrollmentStatus.PendingVerification);
        descriptor.Label.Should().Be("Authenticator app");
    }
}
