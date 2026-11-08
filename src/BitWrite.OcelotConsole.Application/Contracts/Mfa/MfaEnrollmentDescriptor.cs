using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Contracts.Mfa;

/// <summary>
/// Non-secret descriptor for an enrolled or pending MFA factor.
/// </summary>
/// <remarks>
/// T-16 readiness: secrets (TOTP seed, recovery-code plaintext/hashes) are intentionally excluded.
/// Future implementation must store secrets via dedicated secure infrastructure contracts.
/// </remarks>
public record MfaEnrollmentDescriptor(
    string EnrollmentId,
    UserId UserId,
    MfaFactorType FactorType,
    MfaEnrollmentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VerifiedAt = null,
    DateTimeOffset? RevokedAt = null,
    string? Label = null);
