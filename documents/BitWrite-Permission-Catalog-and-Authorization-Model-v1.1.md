# BitWrite Permission Catalog and Authorization Model

**Document ID:** BW-IAM-T18  
**Version:** 1.1  
**Status:** Approved architecture contract  
**Last Updated:** 2026-11-14  
**Work Item:** T-18 — Permission Catalog and Authorization Model

---

## 1. Purpose

This document defines the v1 authorization contract for BitWrite Ocelot Console (the Control Plane), including:

- Ownership and lifecycle of the official Permission Catalog.
- The relationship between Users, Roles, and Permissions.
- Permission assignment and effective-permission calculation.
- Authorization evaluation and enforcement requirements.
- Retired Permission behavior.
- Idempotent Permission assignment behavior.
- Audit and implementation boundaries.

This is an architecture and documentation contract. It does not authorize implementation changes as part of T-18.

## 2. Normative language and decision status

The terms **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** express requirements.

Decision labels:

- **Approved:** An explicit architecture decision.
- **Proposed:** A design recommendation that still requires approval.
- **Unverified:** A claim or implementation detail that has not been independently verified against the current source code.
- **Out of scope:** Explicitly excluded from v1 or from T-18.

The requirements marked Approved are authoritative for v1 unless a later architecture decision explicitly supersedes them.

## 3. v1 authorization model

### 3.1 RBAC model

BitWrite v1 uses product-managed, additive Role-Based Access Control (RBAC).

The model is:

`User → assigned Roles → Role Permission codes → effective Permissions`

A User may have one or more Roles. A Role may contain multiple Permission codes. A User's effective permissions are the union of the runtime-effective permissions of the User's active assigned Roles.

The authorization system MUST apply all of the following:

1. The internal BitWrite User MUST be active.
2. Only active Roles assigned to that User contribute permissions.
3. Only Permission codes present in the official product Permission Catalog are valid.
4. Only catalog Permissions that are runtime-effective contribute to the effective set.
5. A protected operation is allowed only when its required Permission is present in the effective set.
6. Any missing, unknown, invalid, or unresolved authorization condition MUST result in denial.

### 3.2 Explicit v1 exclusions

The v1 authorization model has **no Scope concept**.

The following are excluded:

- Global, Service, Environment, or Gateway scopes.
- `Scope`, `ScopeId`, scoped Role assignments, or scoped Permission grants.
- Resource-context authorization (for example, permission grants limited to a particular Service or Route).
- Permission inheritance.
- Explicit deny rules or deny-overrides.
- External IdP role/group-to-BitWrite-role mapping.
- Tenant-based authorization.

Authorization is based on the User's active Roles and the Permissions granted to those Roles, without a resource scope. Domain and business validation remain separate from authorization.

### 3.3 Identity and authorization ownership

BitWrite owns internal Users, Roles, Permissions, and authorization decisions.

Built-in BitWrite Local User Identity and Authentication Management is the native v1 identity capability. External identity providers may be integrated only through optional, licensed authentication plugins. An external provider authenticates the external identity; it does not own BitWrite authorization.

After successful external authentication, the application resolves or provisions an internal BitWrite User according to the restricted JIT provisioning policy. External groups or roles MUST NOT be treated as BitWrite Permission grants.

Tenant support and broad external authorization federation are out of scope for v1.

## 4. Permission Catalog ownership and source of truth

### 4.1 Product-owned catalog

The official Permission Catalog is owned by the BitWrite product and defined in product source code.

**The product source code is the single source of truth (SoT) for which Permission codes are valid and for the official metadata associated with those Permissions.**

The catalog MAY be exposed in memory at runtime through an implementation-specific abstraction. Its exact class, registration, and loading strategy are implementation decisions; they do not change ownership or source-of-truth rules.

### 4.2 Administrative capabilities

Administrators MUST NOT create, delete, or edit official Permission definitions or codes.

Administrators MAY:

- View the official catalog.
- Assign an assignable catalog Permission to a Role.
- Remove an assigned Permission from a Role.
- View Role assignments and relevant lifecycle information.

Administrators MUST NOT make an unknown code valid by writing it to Redis or another persistence store.

### 4.3 Permission definition metadata

The catalog definition SHOULD support the following product-owned metadata:

| Field | Meaning |
|---|---|
| `Code` | Stable, unique Permission identifier used by application code and authorization checks |
| `Name` | Human-readable display name |
| `Description` | Explanation of the capability |
| `Category` | Product-defined grouping for administration and documentation |
| `Status` | Lifecycle status, at minimum `Active` or `Retired` |
| `Assignable` | Whether administrators may add the Permission to a Role |

The exact source-code representation is not prescribed by T-18. Metadata MUST remain product-owned and MUST NOT be overridden by Redis Role data.

### 4.4 Permission code rules

- Permission codes MUST be unique within the product catalog.
- Permission codes MUST be stable across compatible releases.
- Code normalization MUST be consistent at catalog lookup, assignment, persistence, and evaluation boundaries.
- Normalization MUST NOT cause two distinct catalog codes to become ambiguous.
- Unknown codes MUST NOT be treated as valid permissions.
- A Permission code stored in Redis is a reference, not a definition.

The implementation specification must document the selected normalization strategy. Existing Role behavior has been reported to trim and lowercase codes; that behavior requires source verification before being treated as the final canonicalization contract.

## 5. Role persistence and catalog references

Redis is the current persistence mechanism for IAM Role data.

Redis MAY store:

- Role identity and product-supported Role attributes.
- Role lifecycle state.
- The Permission code strings assigned to each Role.
- Other Role data required by the existing persistence model.

Redis MUST NOT be treated as the source of truth for:

- Whether a Permission code exists.
- Permission names, descriptions, categories, or lifecycle status.
- Whether a Permission is assignable.
- The official Permission Catalog itself.

On every relevant assignment or effective-permission path, persisted Permission references MUST be resolved against the official product catalog. An unknown or non-effective reference MUST NOT grant access.

The storage abstraction SHOULD remain replaceable so that a future persistence migration does not require changing domain or application authorization semantics.

## 6. Permission lifecycle

### 6.1 Active Permission

An Active and assignable Permission may be assigned to a Role. If assigned to an active Role that is assigned to an active User, it may contribute to that User's effective permissions.

### 6.2 Retired Permission — approved policy

The following policy is **Approved**:

1. A Retired Permission MUST NOT be assigned in a new Role grant.
2. Existing historical references to that Permission in Redis MUST NOT be automatically deleted merely because the catalog entry became Retired.
3. A Retired Permission MUST be excluded from Effective Permissions at runtime.
4. A historical Redis reference to a Retired Permission MUST NEVER grant access.
5. The system MUST NOT silently map or replace a Retired Permission with another Permission.
6. Automatic deletion or automatic replacement is not part of the retirement behavior.
7. Administrative reporting, review, migration, and explicit replacement may be provided through a separate workflow.

Retirement therefore changes runtime eligibility without requiring destructive cleanup of historical Role data.

### 6.3 Unknown or removed catalog entries

A persisted code that cannot be resolved in the current official catalog MUST be ignored for authorization and MUST NOT grant access. It SHOULD be surfaced through diagnostics or administrative validation where practical.

No implicit fallback or code substitution is allowed.

## 7. Permission assignment contract

### 7.1 Assignment validation

Before a Permission is added to a Role, the Application/use-case boundary MUST:

1. Normalize the requested code using the selected canonicalization rule.
2. Resolve the code against the official product catalog.
3. Reject the request if the code is unknown.
4. Reject the request if the Permission is Retired or otherwise non-assignable.
5. Check whether the normalized code is already assigned to the Role.
6. If it is not already assigned, invoke the Domain operation to add it.
7. Persist the changed Role and publish/dispatch the appropriate domain event according to the established application flow.

The Application layer MUST NOT bypass catalog validation by directly persisting arbitrary strings.

### 7.2 Idempotent assignment — approved policy

The following policy is **Approved**:

**Re-assigning a Permission that is already assigned to the Role is a successful no-op at the Application/use-case boundary.**

Expected behavior:

- The Application layer checks existing membership using the same normalization rule used by the Domain and persistence model.
- If the Permission is already assigned, the use case returns success/no-change.
- It MUST NOT call the Domain add operation for the duplicate case.
- It MUST NOT emit a duplicate Permission-added domain event.
- It MUST NOT perform an unnecessary Role write for the no-change case, unless a separately documented persistence requirement makes such a write necessary.
- The API MAY represent the result as ordinary success or an explicit no-change result.

This is an Application-level idempotency contract. It does not weaken the Domain aggregate invariant.

### 7.3 Domain invariant preservation

The current Domain behavior has been reported as throwing `PERMISSION_ALREADY_ASSIGNED` when `Role.AddPermission` is called with a duplicate. That behavior is intentionally preserved as the aggregate invariant.

The Application layer is responsible for recognizing an already-satisfied request before invoking the Domain operation. The Domain MUST continue to protect itself against invalid duplicate mutation from other callers.

### 7.4 Concurrency

Two concurrent assignment requests may both observe that a Permission is not yet assigned. The implementation MUST handle this race without creating duplicate assignments, duplicate events, or inconsistent persisted state.

The exact mechanism (for example, optimistic concurrency, atomic Redis operation, repository-level conditional update, or another approach) is an implementation decision outside the architecture-only scope of T-18. It MUST preserve both the Application idempotency contract and the Domain invariant.

### 7.5 Removal

Removing an assigned Permission MUST remove the Role reference through the normal Domain/Application flow. Removing a Permission that is not assigned SHOULD be treated consistently and predictably; the exact no-op/error response should be specified by the relevant use case and API contract.

A Permission being Retired does not prevent removal of its historical reference.

## 8. Effective Permission calculation

Effective permissions are calculated for an internal BitWrite User as follows:

1. Resolve the internal User.
2. If the User is missing or inactive, the effective set is empty and protected operations are denied.
3. Load the Roles assigned to the User.
4. Ignore missing or inactive Roles.
5. For each active Role, resolve its stored Permission codes against the official product catalog.
6. Include only catalog-resolved Permissions that are runtime-effective (Active and otherwise eligible under product policy).
7. De-duplicate the resulting Permission set by canonical Permission code.
8. Use the resulting union as the User's Effective Permissions.

Important consequences:

- A Retired Permission is excluded even if a Role in Redis still references it.
- An unknown code is excluded.
- An inactive Role contributes no permissions.
- An inactive User receives no effective permissions.
- The calculation does not apply scopes, resource filters, inheritance, or external role mappings.

The exact caching strategy and invalidation mechanism are not decided by T-18. Any cache MUST preserve the semantics above and MUST NOT allow stale grants to survive relevant User, Role, assignment, or catalog lifecycle changes beyond the explicitly approved consistency guarantees.

## 9. Authorization Evaluator contract

The Authorization Evaluator answers whether an internal User may perform a protected operation requiring a specific Permission.

Conceptually:

`Authorize(user, requiredPermission) = Allow if requiredPermission ∈ EffectivePermissions(user); otherwise Deny`

Requirements:

- The evaluator MUST use canonical Permission codes.
- The required Permission MUST itself resolve to an official catalog entry and be eligible for runtime authorization.
- A missing User, inactive User, missing Role, inactive Role, unknown Permission, Retired Permission, or unresolved effective-permission state MUST NOT produce an Allow.
- The evaluator MUST deny by default.
- The evaluator MUST NOT infer access from Role names, external IdP groups, UI visibility, or the existence of a Redis string alone.
- The evaluator MUST NOT implement scope or resource-context logic in v1.

The evaluator contract is independent of the eventual implementation mechanism.

## 10. Enforcement requirements

### 10.1 Complete protected-operation enforcement

Every protected application operation MUST enforce the required Permission before executing its business action.

Controller-only authorization is insufficient if application handlers, background jobs, internal dispatchers, or other entry points can invoke the same protected operation without passing through that controller.

The chosen enforcement mechanism MUST ensure that direct or alternate invocation paths cannot bypass authorization.

### 10.2 Enforcement point

T-18 defines the required behavior, not a specific technical mechanism.

The exact enforcement point (for example, application pipeline behavior, explicit application-service/handler checks, or a combination) MUST be selected only after the complete command/query dispatch and invocation paths have been verified.

The implementation MUST NOT assume MediatR or introduce `IRequest`, `IRequestHandler`, or a Mediator abstraction merely to implement authorization. The product's existing explicit CQRS architecture must be respected.

### 10.3 Background and system operations

Background or system-initiated operations MUST have an explicit, bounded trust model. They MUST NOT silently bypass authorization.

Where a system identity or trusted execution policy is required, it MUST be explicit, least-privilege, auditable, and limited to the operation being performed. User-initiated operations MUST retain the initiating User context where applicable.

### 10.4 Business validation is separate

Authorization answers whether the User may perform an operation.

Business/domain validation answers whether the operation is valid in the current business state.

For example, verifying that a Route belongs to an existing Service is business/domain validation; it is not a substitute for checking the Permission required to create or update that Route.

Both checks may be required and MUST remain conceptually separate.

## 11. Permission-to-operation mapping

Every protected operation must map to a specific catalog Permission code.

The mapping must be explicit and testable. It must cover all entry points for the operation, not only visible API controller actions.

The exact v1 Permission list and operation mapping are **Unverified** until the current source tree, product capabilities, and all application execution paths have been reviewed. Candidate codes from earlier drafts MUST NOT be treated as approved catalog entries solely because they appeared in a draft.

A mapping record SHOULD include:

- Product capability / operation.
- Required Permission code.
- Entry point(s).
- Application execution path.
- Whether the operation is user-initiated or system-initiated.
- Enforcement location.
- Test evidence and implementation status.

No protected operation may be considered covered merely because a similarly named controller or policy exists.

## 12. Audit requirements

Security-relevant authorization and Role-management actions SHOULD be auditable, including:

- Role creation, update, activation, and deactivation.
- Permission assignment and removal.
- User-Role assignment and removal.
- Authorization denials for security-sensitive operations, subject to an appropriate logging policy.
- Administrative changes to identity and credential state.
- System-initiated protected actions.

Audit records SHOULD identify the actor or explicit system identity, action, target, timestamp, outcome, and correlation context where available.

Audit logging MUST NOT expose credentials, access tokens, refresh tokens, MFA secrets, or other sensitive authentication material.

A no-op idempotent Permission assignment MAY be recorded as an attempted administrative action if the audit policy requires it, but it MUST NOT create a duplicate Domain event or imply that the Role changed.

## 13. T-18 scope and boundaries

### In scope

- Permission Catalog ownership and source-of-truth contract.
- Permission metadata and lifecycle contract.
- Role-to-Permission reference semantics.
- Assignment validation contract.
- Retired Permission runtime policy.
- Idempotent assignment contract.
- Effective Permission calculation contract.
- Authorization Evaluator contract.
- Enforcement requirements.
- Permission-to-operation mapping requirements and evidence status.
- Documentation, traceability, and acceptance criteria.

### Out of scope

- Implementing or refactoring Domain/Application/Infrastructure/API code.
- Selecting the final source-code catalog class structure.
- Selecting the final authorization pipeline or handler enforcement mechanism.
- Implementing caching or cache invalidation.
- Implementing Redis concurrency control.
- Completing all protected-operation enforcement.
- Finalizing unverified catalog codes without source and capability review.
- Changing JIT default-role policy.
- JWT permission claim design, token refresh, and authorization-version/cache semantics.
- Implementing MFA/TOTP.
- Tenant or Scope support.

These topics belong to the relevant implementation work items or separately approved work.

## 14. Evidence and implementation status

Implementation status MUST be reported honestly using:

- **Implemented:** Verified in source and supported by relevant tests.
- **Partially implemented:** Some required behavior exists, but coverage or guarantees are incomplete.
- **Missing:** Verified absence of the required behavior.
- **Unverified:** Not yet confirmed by source inspection or tests.

A prior model-generated repository review reported existing Role permission string storage and Domain duplicate protection, User Role assignment, Redis persistence, and gaps in application-level authorization enforcement. These are **reported findings only** and MUST be independently re-verified against the current `develop` branch before being represented as verified implementation facts.

## 15. Acceptance criteria

T-18 is complete when:

1. The official Permission Catalog is documented as product-source-owned.
2. Redis is documented as storing Role data and Permission code references only.
3. The v1 RBAC model and explicit no-Scope boundary are documented.
4. Permission metadata and lifecycle rules are documented.
5. The approved Retired Permission policy is documented exactly.
6. The approved Application-level idempotent assignment policy is documented without weakening the Domain invariant.
7. Assignment validation, effective-permission calculation, and authorization evaluation are specified as separate responsibilities.
8. Complete protected-operation enforcement requirements are documented.
9. Business validation is explicitly distinguished from authorization.
10. Permission-to-operation mapping has an evidence status and does not present unverified candidate codes as final.
11. Implementation findings are labeled with accurate evidence status.
12. T-18 documentation is completed without modifying product source code.

## 16. Related work

The following work items are expected to own implementation or adjacent decisions, subject to confirmation against the current backlog:

- T-13 — Role Assignment to User / RBAC Integration.
- T-14 — Default-deny Authorization and enforcement integration.
- T-20 — Domain model details.
- T-21 — User and Role administration use cases.
- T-23 — Restricted JIT default-role policy.
- T-25 — Management API token model and JWT authorization claims.
- T-26 — Resolution of remaining IAM design items.

The exact allocation of complete endpoint/handler enforcement may require a dedicated work item or explicit tasks under the appropriate authorization-enforcement item. Do not silently expand T-18 into implementation.

---

**End of document**
