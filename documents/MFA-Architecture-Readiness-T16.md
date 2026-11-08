# MFA Architecture Readiness (T-16 / #638)

**Status:** Implemented as architecture readiness (TOTP-ready). **MFA/TOTP is not implemented in v1.**

## Purpose

Ensure BitWrite IAM can add MFA (initially TOTP) without redesigning authentication, session issuance, token issuance, or user management.

## v1 Boundary

- No TOTP enrollment, verification, recovery-code issuance, or MFA enforcement in v1.
- No secrets (TOTP seeds/shared secrets, recovery-code plaintext, recovery-code hashes) are introduced by T-16.
- Existing local/external authentication ownership remains unchanged: external IdPs authenticate identities; BitWrite owns internal users, authorization, and session/token lifecycle.

## Readiness Contracts Added

### Domain value objects

- `MfaFactorType` (`Totp`, `RecoveryCode`) — extensible factor taxonomy.
- `MfaEnrollmentStatus` — enrollment lifecycle states for future enrollment/verification/revocation flows.
- `MfaChallengeStatus` — challenge outcomes for future step-up/challenge flows.

### Application contracts

- `MfaChallengeResult` — allows future auth flows to return `NotRequired` (v1 behavior) or `Required/Satisfied/Failed/Expired` without breaking call sites.
- `MfaEnrollmentDescriptor` — metadata-only descriptor; intentionally excludes factor secrets and recovery material.
- `IMfaChallengeService` — future challenge evaluation/verification boundary.
- `IMfaEnrollmentStore` — future enrollment metadata read boundary.
- `INoOpMfaPolicy` — explicit v1 policy marker.

## Extension Points (No v1 Behavior Change)

Future MFA work can plug in at these points:

1. **Local credential verification flow** (post password verification, pre session/token issuance): call `IMfaChallengeService.EvaluateAsync`; if `Required`, return challenge instead of issuing access/refresh tokens.
2. **External/JIT authentication completion**: same challenge boundary before BitWrite session/token issuance.
3. **Refresh/session flows**: remain valid for v1; future step-up policies can add challenge decisions where product requirements demand it.
4. **Enrollment/verification/recovery**: add dedicated secure Application/Infrastructure contracts for secret provisioning, verification, recovery-code generation, and audited recovery actions. Do not place TOTP secrets in `MfaEnrollmentDescriptor` or domain events.

## Security/Audit Constraints for Future Implementation

- Never log or audit TOTP secrets, OTP codes, or recovery-code plaintext.
- Store secrets only through dedicated secure infrastructure (encrypted/protected at rest, least privilege).
- Recovery-code verification must use one-time, non-recoverable validation (hashed/protected material).
- MFA challenge attempts must be throttled and auditable (without secrets).
- Enrollment, verification, disable/recovery, and factor revocation are sensitive admin/self-service actions and must be audited.

## Compatibility

- No existing API behavior changed by T-16.
- No Redis schema/data migration required by T-16.
- Existing handlers/controllers may adopt `MfaChallengeResult`/`IMfaChallengeService` in future tasks without reworking identity ownership.
