# IAM - User Management Task List

**Source:** BitWrite-User-Management-Architecture-v1.0.md, T-18 reconciled architecture docs (v1.1 / implementation spec v1.0), repository state on `develop@ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`  
**Status:** Planning reconciled (implementation backlog updated 2026-11-14)  
**Scope note:** v1 additive RBAC only. No Tenant/Multi-tenancy, no Scope/ScopeId/scoped assignment/resource-context authorization, no permission inheritance, no explicit deny, no external role mapping, no enforced MFA/TOTP, no mandatory external IdP. TOTP is architecture-ready only.

**Status:** Implementation aligned (IAM-01..16 complete; IAM-17 Documentation & Traceability Alignment in progress)
**Scope note:** v1 additive RBAC only. No Tenant/Multi-tenancy, no Scope/ScopeId/scoped assignment/resource-context authorization, no permission inheritance, no explicit deny, no external role mapping, no enforced MFA/TOTP, no mandatory external IdP. TOTP is architecture-ready only.

## Completion Status (IAM v1 Backlog)

| Issue | Title | Status | Notes |
|-------|-------|--------|-------|
| IAM-01 | Role Domain Core | ✅ Complete | Role lifecycle/Active-Inactive/rename invariant; no hard-delete; dispatch of role domain events |
| IAM-02 | RoleId Consistency & RoleName Uniqueness | ✅ Complete | RoleId-only assignments; normalized atomic RoleName index; migration/legacy handling |
| IAM-03 | Permission Catalog Infrastructure | ✅ Complete | Source-code SoT; `SourceCodePermissionCatalog`; `BitWritePermissionCatalogDefinitions.All` populated with approved codes |
| IAM-04 | Role-Permission Management & Idempotent Assignment | ✅ Complete | Catalog-validated assign/remove; idempotent reassignment no-op; per-role distributed lock; domain invariants preserved |
| IAM-05 | Role & User-Role Administration Use Cases | ✅ Complete | Create/get/list/update/rename/activate/deactivate role; reject inactive role assign; dispatch/clear domain events |
| IAM-06 | Effective Permission Resolver | ✅ Complete | Current-state resolver; side-effect-free; active user/roles; catalog-active only; deduped |
| IAM-07 | JWT Issuance: RoleName-Only Claims | ✅ Complete | RoleName claims only; 4-condition eligibility; no Effective Permission calc in issuance; `RoleClaimType` explicit |
| IAM-08 | Current-State Authorization + Default-Deny | ✅ Complete | Permission requirement handler; fail-closed; current-state checks; legacy role policies removed |
| IAM-09 | IAM & Security-Sensitive API Authz | ✅ Complete | `IdentityController`/`AuditController`/`BootstrapController`/`UsersController` protected; explicit `[AllowAnonymous]` exceptions only; no dangling policy refs |
| IAM-10a–d | Product API Authorization (Services/Routes/etc.) | ⏸ Gated | Infrastructure/scaffolding available; completion gated by P-GATE-2 (no invented mappings/codes) |
| IAM-11 | JIT Provisioning Default Role by RoleId | ✅ Complete | Configured active default RoleId; validation; safe failures; preserve issuer/subject linking |
| IAM-12 | Security-Triggered Revocation & State-Change Propagation | ✅ Complete | Event→propagation→revocation (#647)→invalidation/version signals; `DisableUser` aligned to central propagation |
| IAM-13 | Authz Freshness & Stale-Claim Mitigation | ⏸ Gated | Fail-closed freshness with `av` version baseline + IAM-12 signals; **cannot be Complete** until U-03 resolved |
| IAM-14 | IAM Audit Coverage & Security Event Logging | ✅ Complete | `AuditRecorded` emission for state-change and role-admin handlers; coverage verified per event matrix |
| IAM-15 | User Administration Use Cases | ✅ Complete | Disable (mandatory P1) complete/tested/integrated; Enable/Create/Update/List/Get implemented per approved scope |
| IAM-16 | Cross-Module E2E & Security Regression | ✅ Complete | IntegrationTests suite with bootstrap/authz/default-deny/audit E2E coverage |
| IAM-17 | Documentation & Traceability Alignment | 🔄 In Progress | This task — docs aligned with implemented state; unresolved gates marked |

## Implementation Gates (Unresolved — block completion but not task progress)

| Gate | Type | Status | Blocks |
|------|------|--------|--------|
| P-GATE-1 | Product decision | ✅ Resolved | Permission Catalog codes/definitions approved and in code (`BitWritePermissionCatalogDefinitions.All`) |
| P-GATE-2 | Product decision | Unresolved | Endpoint/use-case → Permission mappings (IAM-09 infra complete; IAM-10a–d completion blocked) |
| U-01 | Architectural approval + Security release gate | Unresolved | Historical RoleName reuse mitigation (tombstone/alternative); full IAM v1 security release blocked |
| U-02b | Product decision | Unresolved | Explicit admin removal of stored Retired permission references (retention/non-grant unchanged) |
| U-03 | Security release gate | Unresolved | Freshness mechanism, enforcement point, max stale-state window per case, cache/version consistency, failure behavior; IAM-13 cannot be Complete |


## Existing Issue Reconciliation (T-16–T-21 and related, evidence-based)

| Issue | Current | Evidence | Recommendation | Reason |
|-------|---------|----------|----------------|--------|
| #638 T-16 MFA Architecture Readiness | In Review (PR #665) | `MFA-Architecture-Readiness-T16.md`, MFA contracts | **Keep** (track review/merge only) | Architecture-ready deliverable present; must not expand into MFA enforcement (out of v1). |
| #639 T-17 Bootstrap & Initial Administrator | In Progress | bootstrap handler/controller/hosted service/options/lock marker | **Update** | Clarify acceptance: secure deployment-supplied secrets only, idempotent/multi-instance safety, no default creds, no secret leakage, tests/ops verification; keep scope limited. |
| #640 T-18 Permission Catalog Definition | Documentation reconciled (implementation pending) | `documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md`, implementation spec, `T18-Final-Diff-Review-Report.md`, superseded v1.0 notice | **Keep + Update** (do not reopen approved decisions) | T-18 design/docs done; split/track implementation as separate issues below; resolve minor doc alignment (RBAC audit coverage wording) if needed. |
| #641 T-19 External Authentication Plugin Contract | Todo | external identity/JIT primitives exist, no verified plugin contract abstraction beyond current flow | **Update/Split** | Must remain optional licensed plugin contract; BitWrite owns internal authorization; depends on stable identity/JIT/default-role policy decisions. |
| #642 T-20 Domain Model - User, LocalCredential, ExternalIdentity | Todo | aggregates exist but gaps remain | **Update (critical path)** | Refocus on finalized IAM domain model/invariants: Role Active/Inactive lifecycle, stable Role identity contract, assignment/lifecycle rules, no v1-excluded concepts. Do not silently approve RoleId-over-Name. |
| #643 T-21 User Administration Use Cases | Todo | partial role/user/session/credential pieces exist | **Split/Update** | Break into concrete use cases (role mgmt, role-permission mgmt, user-role mgmt, permission retrieval, credential/admin flows) with exact boundaries and dependencies. |
| #644 T-22 Credential Management Use Cases | Todo | `LocalCredential`, repos/services exist | **Update** | Verify existing flows vs gaps (change/reset/lockout/admin reset policies) against approved security model; avoid duplicating implemented primitives. |
| #645 T-23 External Identity Management Use Cases | Todo | JIT/external identity linking primitives exist | **Update** | Cover JIT restricted default-role policy, explicit linking rules, external identity admin operations per approved model. |
| #646 T-24 Account Security Use Cases | Todo | disable/enable/throttle/session revocation primitives partial | **Update** | Include verified `UserDisabled`→session revocation wiring, lockout/revocation semantics, deny access for inactive users. |
| #647 T-15 Session & Refresh Token Management | In Review (PR #664) | session/refresh domain, repos, handlers, controller, tests | **Keep** (review/merge) | Validate acceptance, security, and revocation-on-disable integration. |
| #648 T-26 Open Design Items Resolution | Todo | RoleId vs Name, bootstrap/JIT defaults, etc. | **Update** | Track only unresolved decisions with evidence; feed into T-20/T-21/T-23; do not convert proposals to approvals. |
| #649 T-25 Management API Token Model - JWT | Todo | JWT issuance/validation in `SecurityService` exists | **Update** | Reconcile implemented token model with approved IAM/authz requirements (claims, effective permissions/roles source, revocation, versioning concerns) without adding excluded scope. |
| #650 T-07 Security Requirements | Todo | partial implementations (hashing, throttle, lockout, bootstrap, token lifecycle pieces) | **Update/Split** | Decompose into verifiable security controls and regression coverage; mark already-satisfied controls as evidence-backed. |
| #625/#627 T-01/T-02 | Done (per backlog history) | domain/credential implementations present | **Keep** | Confirm closure criteria in GitHub; retain as dependency anchors. |
| #626/#629/#630/#631/#632/#633/#634/#635/#636/#637 T-03–T-14 | Mixed (several commits exist; backlog stale) | commits `2809edc..3447917` touch related areas; code gaps remain | **Update (per-issue audit required)** | Reconcile each issue’s acceptance against current code/tests before close; do not mark implemented without evidence. |

## Final GitHub Issue Backlog (prioritized, implementation-ready proposals)

> No issues created/updated/closed. `Existing Issue ID` is included only where verified in repo/docs/history; otherwise **New (proposed)**. Exact permission codes are **Unverified/proposed** until the official source-code catalog is defined and approved (T-18 implementation).

### P0 — Domain Foundation (Critical Path)

| Priority | Proposed title | Existing Issue ID | New/Update | Scope & responsibilities | Acceptance Criteria (summary) | Dependencies | Architectural constraints | DoD |
|---|---|---|---|---|---|---|---|---|
| P0 | Finalize IAM Domain Model (Role lifecycle, invariants, identity contracts) | #642 (T-20) | **Update** | Finalize User/Role/Permission-related domain concepts; add Role Active/Inactive lifecycle and invariants; define behavior for assigning inactive roles/users; clarify permission reference semantics; resolve open questions with evidence. | Role has explicit lifecycle; inactive roles cannot contribute to effective permissions; invariants tested; no v1-excluded concepts added; RoleId-vs-Name decision documented as approved or explicitly deferred with impact. | #625, #640 docs | Additive RBAC only; Role=assignment, Permission=authz unit; no scope/deny/inheritance/tenant. | Domain changes + tests + ADR/notes in issue; existing invariants preserved. |
| P0 | Normalize Role Identity & Assignment Contract | **New (proposed)** | **New** (or split from #642/#631/#643) | Inspect/fix use of `Role.Name` vs `RoleId` across assignment, persistence, retrieval, JIT, and APIs; define stable contract (proposal: `RoleId` pending explicit approval). | Single stable role identifier used consistently; `Assign/Remove/Get` and JIT resolve same identity; migration/compatibility strategy defined; tests cover mismatch cases. | #642 decision point | Do not silently approve RoleId; preserve data integrity and auditability. | Code/tests/docs updated; no ambiguous dual semantics. |
| P0 | Implement Official Permission Catalog (source-code SoT) | **New (proposed)** | **New** (implements #640 decisions) | Introduce product-owned catalog in source: codes, metadata, lifecycle (Active/Retired), assignability, lookup/validation contracts; Redis never SoT. | Catalog is authoritative in code; validation/lookup APIs/contracts exist; Active/Retired supported; unknown codes detectable; admins cannot mutate definitions at runtime. | #640 docs, #642 | Source-code SoT; catalog additions require code+release; do not promote speculative codes. | Catalog module + unit tests + reviewable metadata contract. |
| P0 | Role-Permission Domain Validation Integration (catalog-aware, no runtime definition edits) | **New (proposed)** | **New** | Integrate catalog validation into role-permission operations (assign/remove eligibility rules) without breaking `Role.AddPermission` duplicate invariant. | Only existing assignable (Active) catalog permissions can be newly assigned; retired/unknown rejected on assignment; domain invariant remains. | P0 Catalog, #642 | Retired cannot be newly assigned; preserve `PERMISSION_ALREADY_ASSIGNED`. | Tests for valid/invalid/retired/unknown/duplicate. |

### P1 — Role and Permission Management (Application)

| Priority | Proposed title | Existing Issue ID | New/Update | Scope & responsibilities | Acceptance Criteria (summary) | Dependencies | Architectural constraints | DoD |
|---|---|---|---|---|---|---|---|---|
| P1 | Implement Role Management Use Cases (CRUD + activate/deactivate) | #643 (split) | **Update/Split** | Create/update/activate/deactivate/delete roles per approved rules; enforce lifecycle and audit. | Use cases respect Role lifecycle; inactive role behavior enforced; audited; concurrency-safe where required. | #642, Role identity | Admins manage roles/assignments only, not catalog definitions. | Handlers + tests + API/contract as applicable. |
| P1 | Implement Role Permission Assignment/Removal Use Cases | #643 (split) | **Update/Split** | Assign/remove catalog permissions to/from roles; validate existence/assignability/lifecycle. | Active catalog permissions assignable; retired/unknown rejected; removals safe for historical refs; audited. | Catalog, #642 | Redis stores refs only; no runtime catalog mutation. | Use cases + tests. |
| P1 | Implement Permission Assignment Idempotency at Application Boundary | **New (proposed)** | **New** | Successful no-op when already assigned: check normalized membership before domain add; no duplicate event; no unnecessary persistence write; API returns success/no-change. | App returns success/no-change on duplicate; domain `PERMISSION_ALREADY_ASSIGNED` still thrown if bypassed; no extra write/event; concurrency handled. | Role-Permission assignment | **Do not weaken domain invariant.** | Unit/integration tests incl. concurrency/race. |
| P1 | Implement Role Permission Retrieval (codes + catalog metadata, safe historical refs) | #643 (split) | **Update/Split** | Retrieve assigned codes and resolve metadata via catalog; surface unknown/retired historical refs safely without granting access. | Response distinguishes resolved/retired/unknown as designed; no implicit replacement/mapping. | Catalog, resolver contract | Historical refs retained in Redis; never grant access. | Query/use case + tests. |
| P1 | Implement User Role Assignment with Lifecycle Validation | #631 (T-13) | **Update** | Assign/remove roles from users using finalized role identity; validate user/role existence and lifecycle; handle inactive role assignment per approved rule. | Consistent identity; correct errors; inactive/disabled rules enforced; audited; idempotency semantics defined where applicable. | #642, Role identity | Deny-by-default; no scoped assignment. | Handlers/tests aligned to contract. |
| P1 | Align JIT Provisioning with Restricted Default-Role Policy | #630/#645 (T-04/T-23) | **Update** | Replace placeholder `"restricted-user"` with configured/validated default role resolution per approved policy; safe failure if invalid/missing; preserve issuer+subject mapping rules. | JIT users receive only approved restricted default role; failures safe; no privilege escalation; audited. | Role identity, role mgmt, #642 | External authn != BitWrite authz; no external role mapping. | Use case/tests/config validation. |

### P2 — Permission Resolution and Authorization

| Priority | Proposed title | Existing Issue ID | New/Update | Scope & responsibilities | Acceptance Criteria (summary) | Dependencies | Architectural constraints | DoD |
|---|---|---|---|---|---|---|---|---|
| P2 | Implement Effective Permission Resolver | **New (proposed)** | **New** | Compute union of valid permissions from active roles of active users; exclude inactive roles, inactive users, retired/unknown permissions. | Deterministic union; deny-by-default inputs; inactive user => empty; retired excluded; tested. | Catalog, role lifecycle, user-role assignment | No inheritance, no deny rules, no scope/resource context. | Service + unit tests. |
| P2 | Implement Permission Authorization Service/Contract | **New (proposed)** | **New** | `HasPermission(user, permissionCode)` (or equivalent) abstraction for API/app enforcement. | Single contract used by policies/handlers; resolves via catalog+effective perms; unknown required permission => deny. | Effective resolver | Compatible with CQRS/custom handlers (no MediatR required). | Interface/impl + tests. |
| P2 | Implement Default-Deny Authorization Enforcement (API + Application) | #632 (T-14) | **Update/Split** | Ensure missing/unresolvable permission denies; enforce on complete execution paths including app handlers where applicable. | Protected operations deny without required permission; controller-only not sufficient when alternate paths exist. | Authz service | Deny-by-default, no implicit grants. | Tests for API and use-case paths. |
| P2 | Implement Permission-based Authorization Policies (ASP.NET Core integration) | **New (proposed)** | **New** | Replace/augment role-name policies with permission requirements; resolve existing `Operator` policy inconsistency (verified missing). | Named/requirement policies map to catalog permission codes; `Operator` resolved; legacy role policies migrated or explicitly contained. | Authz service, catalog codes | No new scopes/tenants; exact codes verified before enforcement. | Policy registration + tests. |

### P3 — Protect Product APIs and Use Cases

| Priority | Proposed title | Existing Issue ID | New/Update | Scope & responsibilities | Acceptance Criteria (summary) | Dependencies | Architectural constraints | DoD |
|---|---|---|---|---|---|---|---|---|
| P3 | Protect IAM Management APIs & Use Cases | #643/#646 (related) | **Update/Split** | Apply permission checks to identity/user/role/credential/session/bootstrap/audit admin surfaces as applicable. | Each endpoint/use case has defined required permission and enforcement evidence; anonymous/bootstrap exceptions explicitly justified. | P2 policies/service | Least privilege; bootstrap remains secure-by-default. | Tests (authorized/unauthorized/forbidden). |
| P3 | Protect Service Management APIs & Use Cases | **New (proposed)** | **New** | Enforce permissions on `ServicesController` and handlers. | Required permission(s) defined & enforced end-to-end. | P2 | Exact code(s) **Unverified** until catalog. | Integration tests. |
| P3 | Protect Route Management APIs & Use Cases | **New (proposed)** | **New** | Enforce on `RoutesController` and handlers. | Defined & enforced; alternate paths covered. | P2 | — | Tests. |
| P3 | Protect Global Configuration APIs & Use Cases | **New (proposed)** | **New** | Enforce on `GlobalConfigurationController` and handlers. | Defined & enforced. | P2 | — | Tests. |
| P3 | Protect Snapshot & Publication APIs & Use Cases | **New (proposed)** | **New** | Enforce on snapshot/publication controllers and handlers. | Defined & enforced. | P2 | — | Tests. |
| P3 | Protect Remaining Management APIs & Resolve Policy Inconsistencies | **New (proposed)** | **New** | Gateways, runtime, plugins, licenses, settings, audit read/write surfaces as applicable; fix `IdentityController` `Operator` policy gap. | No unintended anonymous management access; `Operator` defined or removed consistently. | P2 | Preserve necessary anonymous endpoints only where explicitly required. | Tests + audit of attributes. |

### P4 — Tests, Security, Completion

| Priority | Proposed title | Existing Issue ID | New/Update | Scope & responsibilities | Acceptance Criteria (summary) | Dependencies | Architectural constraints | DoD |
|---|---|---|---|---|---|---|---|---|
| P4 | IAM Domain & Application Tests (RBAC core) | **New (proposed)** | **New** | Cover domain invariants, role lifecycle, permission assignment idempotency, user-role rules, catalog validation, retired refs. | Required scenarios pass (incl. concurrency where relevant). | P0–P1 | Preserve domain invariants. | Automated tests in CI. |
| P4 | Permission Authorization Integration Tests | **New (proposed)** | **New** | API/app enforcement, default-deny, effective permissions, policy mapping. | Authorized/unauthorized/forbidden cases covered per protected surface. | P2–P3 | Deny-by-default. | Integration tests. |
| P4 | IAM Security Regression Tests | #650/#646 related | **Update/Split** | Regressions for disable→revocation, throttling, bootstrap safety, token/session invalidation, retired permission exclusion. | No privilege escalation; inactive users/roles grant nothing; secrets not leaked. | P1–P3, #639/#647 | No MFA enforcement added. | Security-focused test suite. |
| P4 | Bootstrap & Initial Administrator Hardening/Verification | #639 (T-17) | **Update** | Complete acceptance verification: config-only secrets, lock/marker idempotency, role assignment correctness, audit, no API secret intake. | Meets architecture/security requirements with evidence. | #642 role identity, role mgmt | No default credentials. | Tests/docs/runbook notes. |
| P4 | IAM Authorization Documentation Finalization | #640 follow-up / **New** | **Update/New** | Document final permission codes (once approved), enforcement map, operational guidance; align RBAC audit-coverage wording across T-18 docs (minor review item). | Docs match implemented behavior; codes traceable to catalog; no speculative codes documented as official. | P0–P3 | Source-code catalog remains SoT. | Reviewed docs. |

## Implementation Roadmap

### Recommended execution order (critical path)
1. **P0 / #642 (update):** Finalize IAM domain model & Role lifecycle; explicitly decide/defer Role identity (do not assume RoleId).
2. **P0 (new):** Normalize Role identity & assignment contract across domain/app/infra/JIT/retrieval.
3. **P0 (new):** Implement official source-code Permission Catalog (Active/Retired, metadata, lookup/validation).
4. **P0 (new):** Integrate catalog-aware role-permission validation (preserve domain duplicate invariant).
5. **P1:** Role management use cases → Role permission assign/remove → **Permission assignment idempotency (app no-op)** → Role permission retrieval → User role assignment alignment → JIT restricted default-role policy.
6. **P2:** Effective permission resolver → Permission authorization service → Default-deny enforcement → ASP.NET Core permission policies (resolve `Operator`).
7. **P3:** Protect IAM APIs/use cases first, then Service/Route/GlobalConfig/Snapshot/Publication/remaining management surfaces (can be parallelized by bounded context once P2 contracts are stable).
8. **P4:** Domain/app tests, authorization integration tests, security regressions, bootstrap verification, documentation finalization.

### Parallelizable work (after dependencies ready)
- P3 endpoint protection tracks per controller/domain area **after** permission policy/service contracts are stable.
- P4 test suites can be developed alongside P3 slices they cover (TDD/parallel PRs), but must not claim completion before enforcement exists.
- Documentation alignment (minor T-18 wording) can run in parallel with implementation planning.

### Blocking issues / milestones
- **Milestone M1 (Domain & Identity ready):** #642 updated decisions + Role identity normalization complete.
- **Milestone M2 (Catalog ready):** official catalog + validation contracts merged (source-code SoT).
- **Milestone M3 (RBAC core ready):** role/user-permission management + idempotency + effective resolver complete.
- **Milestone M4 (Enforcement ready):** permission authz service/policies/default-deny enforced in API and application paths.
- **Milestone M5 (Surface protected):** all management APIs/use cases protected with verified permission mapping and tests.
- **Milestone M6 (Release hardening):** security regressions/bootstrap verification/docs complete.

### Critical path
`#642 Domain/Lifecycle` → `Role Identity normalization` → `Permission Catalog` → `Role-Permission mgmt + idempotency` → `Effective Permission Resolver` → `Permission Authz/Default-Deny/Policies` → `API+UseCase enforcement` → `Integration/Security tests`.

## Risks and Open Questions (unresolved / must not be treated as approved)

| Item | Type | Status | Notes |
|------|------|--------|-------|
| Official permission code set | Catalog/product decision | ✅ Resolved | Codes defined in `BitWritePermissionCatalogDefinitions.All` |
| endpoint-to-permission mapping | Catalog/product decision | ⏸ Unresolved (P-GATE-2) | Mappings not approved — none invented |
| Throttling effectiveness/coverage | Security verification | ⏸ Independent | #628 — outside IAM v1 backlog scope; middleware/service present |


## Executive Summary

- **Current state:** All IAM v1 implementation issues IAM-01 through IAM-16 are complete on `develop` (merged). Foundational domain/identity/catalog contracts are implemented and tested: Role lifecycle (Active/Inactive, rename invariant), RoleId-only assignments, source-code Permission Catalog infra, catalog-validated role-permission management with idempotent reassignment, effective-permission resolver, RoleName-only JWT issuance with 4-condition eligibility, current-state authorization with default-deny/fail-closed, security-triggered revocation & state-change propagation (IAM-12 matrix via #647), user administration use cases (Disable mandatory P1 complete), IAM/security-sensitive API authorization protection, audit event coverage for state changes, and an E2E/security regression suite.
- **Implementation gates unresolved (block full security-release completion but not task progress):** P-GATE-2 (endpoint→permission mappings), U-01 (Historical RoleName reuse mitigation), U-02b (explicit admin removal of stored Retired references), U-03 (authz freshness mechanism/max stale-state window/cache-consistency/failure behavior). No endpoint→permission mappings invented or presented as approved.
- **Security release prerequisites:** U-01 and U-03 must be resolved, implemented, and tested before IAM-13 can be Complete and before full IAM v1 security release approval. Short JWT expiry alone is never sufficient.
