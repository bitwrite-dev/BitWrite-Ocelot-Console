using BitWrite.OcelotConsole.Application.Contracts.Mfa;
using BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

namespace BitWrite.OcelotConsole.Application.Interfaces.Mfa;

/// <summary>
/// Persistence/readiness contract for MFA enrollment metadata (T-16).
/// </summary>
/// <remarks>
/// Exposes metadata only; TOTP secrets and recovery-code material must be handled by
/// future dedicated secure contracts/infrastructure implementations.
/// </remarks>
public interface IMfaEnrollmentStore
{
    Task<IReadOnlyList<MfaEnrollmentDescriptor>> GetEnrollmentsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<MfaEnrollmentDescriptor?> GetEnrollmentAsync(UserId userId, string enrollmentId, CancellationToken cancellationToken = default);
}
