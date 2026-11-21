# BitWrite Permission Catalog — Implementation Specification

**Document ID:** BW-IAM-T18-IMPL  
**Version:** 1.0  
**Status:** Implementation specification / source verification required  
**Last Updated:** 2026-11-14  
**Related Architecture:** `BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md`  
**Work Item:** T-18 — Permission Catalog and Authorization Model

---

## 1. Purpose

This document translates the approved T-18 architecture contract into implementation-facing requirements, validation rules, test scenarios, and a source-verification plan.

It is a specification for subsequent implementation work. **It does not authorize code changes under T-18.**

The architecture document is authoritative if any wording in this specification appears inconsistent with it.

## 2. Normative decisions

The following decisions are Approved:

1. The official Permission Catalog is defined and owned by BitWrite product source code.
2. Product source code is the single source of truth for valid Permission codes and their official metadata.
3. Redis stores Role data and Permission code references assigned to Roles; Redis does not define Permission validity or metadata.
4. v1 uses additive RBAC with Users, Roles, and Permissions.
5. v1 has no Scope, scoped assignments, resource-context authorization, permission inheritance, tenant authorization, explicit deny rules, or external role mapping.
6. Only active Users and active assigned Roles contribute to effective permissions.
7. Unknown and Retired Permissions do not grant access.
8. A Retired Permission cannot be assigned in a new grant. Historical Redis references are retained, excluded from runtime effective permissions, and are not automatically deleted or replaced.
9. Re-assigning an already-assigned Permission succeeds as a no-op at the Application/use-case boundary.
10. The Domain duplicate invariant remains intact. The Application layer must avoid invoking the Domain add operation for an already-assigned Permission.
11. All protected application operations must enforce authorization; controller-only checks are insufficient where alternate invocation paths exist.
12. T-18 is documentation/specification only; implementation belongs to the relevant implementation work items.

## 3. Responsibility boundaries

| Responsibility | Required owner | Contract |
|---|---|---|
| Official Permission definitions and metadata | Product source code | Defines valid codes and lifecycle |
| Role and assigned Permission-code persistence | Current persistence implementation (Redis in v1) | Stores Role data and code references only |
| Catalog lookup and assignment eligibility | Application boundary using product catalog | Reject unknown/non-assignable codes |
| Role invariants | Domain | Protects aggregate consistency |
| Assignment idempotency | Application/use-case boundary | Existing assignment returns success/no-change |
| Effective Permission calculation | Authorization/application domain service, exact location TBD | Union eligible permissions from active Roles |
| Authorization decision | Authorization Evaluator | Allow only when required Permission is effective |
| Enforcement coverage | Complete protected application execution path | Prevent alternate-path bypass |
| Business validity | Domain/application business rules | Separate from authorization |

The exact class names, namespaces, dependency registration, and dispatch integration are implementation decisions and must be confirmed against the current architecture before coding.

## 4. Permission Catalog implementation requirements

### 4.1 Source-code ownership

The catalog must be represented by product-owned source code. It may be implemented as immutable definitions, a registry, a static catalog, or another maintainable structure.

Regardless of representation:

- Catalog definitions must not be administratively mutable at runtime.
- Catalog metadata must not be loaded from Redis as authoritative definitions.
- Unknown codes must fail catalog resolution.
- Code uniqueness must be validated.
- Catalog lookup must be deterministic.
- Tests must cover duplicate definitions and unknown-code behavior.

### 4.2 Required metadata

Each catalog entry should provide:

| Property | Required | Notes |
|---|---:|---|
| Code | Yes | Stable, unique identifier |
| Name | Yes | Human-readable label |
| Description | Yes | Clear capability description |
| Category | Recommended | Product-defined grouping |
| Status | Yes | At least Active / Retired |
| Assignable | Yes | Controls whether a new Role grant is allowed |

If `Assignable` is derived from `Status` rather than stored explicitly, the rule must be unambiguous and tested. A Retired Permission is never assignable.

### 4.3 Code normalization

A single canonicalization rule must be used consistently by:

- Catalog lookup.
- Application assignment validation.
- Role membership checks.
- Domain mutation.
- Persistence serialization/deserialization.
- Effective Permission calculation.
- Authorization evaluation.

**Verified (current implementation, `develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`):** `src/BitWrite.OcelotConsole.Domain/Aggregates/Role/Role.cs` normalizes Permission codes with `permission.Trim().ToLowerInvariant()` in both `AddPermission` (L70) and `RemovePermission` (L89), and `src/BitWrite.OcelotConsole.Domain/ValueObjects/Identity/RoleId.cs` normalizes via `From` (`value.Trim().ToLowerInvariant()`, L21-22).

**Verified inconsistency / end-to-end status:** `src/BitWrite.OcelotConsole.Domain/Aggregates/User/User.cs` `AssignRole`/`RemoveRole` store/compare the supplied role identifier as-is (no normalization, L154-176). Application handlers pass role identifiers without a single verified canonicalization boundary (e.g. `AssignRoleCommandHandler` stores `role.Name`, L32; `GetUserRolesQueryHandler` resolves stored values via `RoleId.From(...)`, L31-32). **End-to-end normalization consistency across catalog lookup, assignment, membership, persistence, and evaluation is Unverified/incomplete** (catalog/evaluation paths are not implemented). Do not silently change normalization as part of T-18.

Tests must establish that equivalent input forms are treated consistently and that normalization cannot merge distinct catalog entries.

## 5. Redis persistence contract

Redis stores Role data and assigned Permission code strings.

It must not store or authoritatively determine:

- Official Permission definitions.
- Permission names or descriptions.
- Permission categories.
- Permission lifecycle status.
- Permission assignability.
- The set of valid Permission codes.

On deserialization or use of persisted Role data, code references must be resolved against the product catalog before they are used for authorization.

A Role may contain a historical code that is now Retired or no longer resolvable. Such a reference must not be allowed to grant access. The system should make these references observable for administrative diagnosis and migration.

The repository/storage abstraction should isolate Redis-specific details from Domain and Application contracts.

### 5.1 Verified implementation (read-only, `develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`)

| Aspect | Verified finding (source reference) | Status |
|---|---|---|
| Role repository interface | `src/BitWrite.OcelotConsole.Application/Interfaces/IRoleRepository.cs` — `GetByIdAsync(RoleId)`, `GetByNameAsync(string)`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` (L6-14). | **Implemented** |
| Redis Role persistence & serialization | `src/BitWrite.OcelotConsole.Infrastructure/Redis/RedisRoleRepository.cs` — serializes/deserializes full `Role` aggregate as JSON (`JsonSerializer.Serialize/Deserialize`, L26,36,58); `GetByNameAsync` resolves key via `RoleId.From(name)` (L31); `UpdateAsync` delegates to `AddAsync` (L65-68); membership tracked in index set (L41-42,61-62,77-78). | **Implemented** (persistence mechanism) |
| Key/namespace structure | `src/BitWrite.OcelotConsole.Infrastructure/Redis/RedisKeyHelper.cs` — `IamNamespace = "ocelot:iam:v1"` (L71), `IamRole(RoleId id) => $"{IamNamespace}:role:{id.Value}"` (L105), `IndexRoles => $"{IamNamespace}:roles"` (L108). | **Implemented** |
| Stored content | Role Permission code strings are persisted as part of the serialized Role object (`Role._permissions`/`Permissions`, `Role.cs` L17,27). **No official Permission Catalog or Permission metadata is stored in Redis.** | **Implemented** (refs only) / catalog in Redis **Missing** (as required) |
| Catalog resolution on use | No resolution of persisted Permission references against a product catalog before authorization/use (catalog/evaluator not implemented). | **Missing** |

## 6. Assignment use-case algorithm

### 6.1 Add Permission to Role

The use case must implement the following logical sequence:

1. Load the target Role.
2. Normalize the requested Permission code.
3. Resolve the code in the official catalog.
4. If no catalog entry exists, reject the request with a stable application error.
5. If the catalog entry is Retired or not assignable, reject the request.
6. Check whether the normalized code is already assigned to the Role.
7. If already assigned, return successful no-change without calling the Domain add method.
8. Otherwise call the Domain aggregate operation.
9. Persist the Role.
10. Dispatch/publish the resulting Domain event through the established application mechanism.
11. Return success.

The precise error-code names for unknown and non-assignable Permission codes should be aligned with the product's existing error conventions. Do not invent inconsistent error contracts in an isolated feature.

### 6.2 Idempotent duplicate behavior

Approved behavior:

| Condition | Application result | Domain call | Persistence write | Duplicate event |
|---|---|---:|---:|---:|
| Permission is valid, Active, assignable, and not assigned | Success / changed | Yes | Yes | No duplicate; normal add event once |
| Permission is already assigned | Success / no-change | No | No unnecessary write | No |
| Permission code is unknown | Reject | No | No | No |
| Permission is Retired | Reject | No | No | No |
| Permission is otherwise non-assignable | Reject | No | No | No |

The current Domain behavior has been reported to throw `PERMISSION_ALREADY_ASSIGNED` for duplicate `Role.AddPermission` calls. Preserve that invariant. Do not change the Domain method to silently accept duplicates merely to achieve API idempotency.

### 6.3 Concurrency behavior

The implementation must account for concurrent requests attempting to add the same Permission to the same Role.

Required outcome:

- No duplicate Permission references.
- No duplicate Permission-added events.
- No inconsistent Role state.
- A request that loses a race because the Permission was concurrently added should resolve to the approved successful no-change outcome where the operation's final state satisfies the request.

Potential mechanisms include optimistic concurrency, an atomic conditional repository update, or a transaction/script supported by the persistence strategy. Select a mechanism only after inspecting the current repository and Redis persistence patterns.

Concurrency implementation is not a T-18 code task.

### 6.4 Remove Permission from Role

Removal must use the normal Domain/Application flow and persist the changed Role. A Retired Permission's historical reference may be removed by an administrator. Retirement does not block cleanup.

The API/use-case behavior when removing a code that is not assigned must be consistent and explicitly tested. This specification does not force a particular error response if the product's established conventions dictate otherwise.

**Verified current state (`develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`):** No Role-Permission Application use cases (add/remove Permission to/from Role) were found in `src/`. `Role.AddPermission`/`Role.RemovePermission` and events exist at Domain level (`src/BitWrite.OcelotConsole.Domain/Aggregates/Role/Role.cs` L65-97; `src/BitWrite.OcelotConsole.Domain/Events/DomainEvents.cs` L133-134), but **Application-level catalog validation and Application-level idempotent Permission assignment behavior are Missing**. The Domain duplicate invariant remains intact (`PERMISSION_ALREADY_ASSIGNED`, `Role.cs` L71-72).

## 7. Effective Permission calculation

The implementation must calculate the User's effective permissions using this algorithm:

```text
GetEffectivePermissions(user):
    if user does not exist or user is inactive:
        return empty set

    effective = empty set

    for each Role assigned to user:
        if Role does not exist or Role is inactive:
            continue

        for each PermissionCode stored on Role:
            permission = official catalog lookup(PermissionCode)

            if permission does not exist:
                continue

            if permission is Retired or not runtime-effective:
                continue

            add canonical permission.Code to effective

    return effective
```

The result is a de-duplicated union. No Scope or resource-context filtering is applied in v1.

The algorithm must not grant access based solely on a string being present in Redis.

## 8. Authorization Evaluator

The evaluator must:

1. Receive or resolve the internal BitWrite User context.
2. Resolve the required Permission code through the official catalog.
3. Deny if the required Permission is unknown, Retired, or otherwise not runtime-effective.
4. Obtain the User's effective Permission set.
5. Allow only if the required canonical code is present.
6. Deny in every other case.

The evaluator must not infer authorization from:

- Role names.
- External IdP roles or groups.
- UI menu visibility.
- Controller route names.
- Redis values that have not been resolved against the official catalog.
- A previous successful request without an explicitly approved cache consistency guarantee.

Default behavior is deny.

## 9. Protected-operation enforcement

### 9.1 Required coverage

Every protected operation must have a documented required Permission and an enforcement point on its complete application execution path.

A controller attribute or API policy alone is not sufficient if another caller can invoke the same application operation without passing through that controller.

The source review must identify:

- API controller actions.
- Application command/query handlers.
- Direct handler invocations.
- Internal dispatch paths.
- Background jobs and hosted services.
- System-initiated publication/deployment workflows.
- Any other entry point that can execute protected behavior.

### 9.2 Enforcement mechanism

The exact mechanism is **Not Yet Decided**.

Before selecting it, inspect the current CQRS execution path end-to-end. The implementation must respect the existing explicit handler architecture and must not introduce MediatR, `IRequest`, `IRequestHandler`, or a Mediator abstraction as an assumed prerequisite.

A pipeline behavior, explicit application-level authorization, or combined approach may be evaluated after the call graph is verified. The choice must prevent bypass through direct handler invocation.

### 9.3 System-initiated operations

Background/system operations must use an explicit least-privilege system identity or a clearly bounded trusted execution policy. They must not become an undocumented authorization bypass.

Where relevant, preserve the initiating User identity for audit and traceability.

## 10. Permission-to-operation mapping

The exact catalog and operation map must be derived from current product capabilities and verified source paths. **Do not invent Permission codes.** No verified official Permission Catalog exists in source (`develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`); therefore exact Permission codes and enforcement mappings remain **Unverified**.

The following **verified product capability areas** exist (controller/handler entry points). Required Permission and enforcement are **Unverified/Missing** for all until a source-backed catalog and mapping are produced:

| Capability area | Verified entry points / execution references (source) | Required Permission | Enforcement | Status |
|---|---|---|---|---|
| Services | `src/BitWrite.OcelotConsole.Api/Controllers/ServicesController.cs` (`[Route("api/v1/services")]`, CRUD & service-routes actions) → `Application/UseCases/Service/*Handler` (injected L13-18) | **Unverified** (no catalog) | **Missing** (no `[Authorize]`/permission check observed in controller/handlers) | **Missing** |
| Routes | `src/BitWrite.OcelotConsole.Api/Controllers/RoutesController.cs` (`[Route("api/v1/routes")]`) → `Application/UseCases/Route/*Handler` (injected L27-38) | **Unverified** | **Missing** | **Missing** |
| Global Configuration | `src/BitWrite.OcelotConsole.Api/Controllers/GlobalConfigurationController.cs` (`[Route("api/v1/global-configuration")]`, GET/PUT) → `Application/UseCases/GlobalConfiguration/*Handler` (L15-16) | **Unverified** | **Missing** | **Missing** |
| Snapshots | `src/BitWrite.OcelotConsole.Api/Controllers/SnapshotsController.cs` (`[Route("api/v1/snapshots")]`, incl. publish/rollback/deployment) → `Application/UseCases/Snapshot/*Handler` and `UseCases/Publication/PublishSnapshotCommandHandler`/rollback (L17-27) | **Unverified** | **Missing** | **Missing** |
| Publications | `src/BitWrite.OcelotConsole.Api/Controllers/PublicationsController.cs` (`[Route("api/v1/publications")]`) → `Application/UseCases/Publication/*Handler` (L15-23) | **Unverified** | **Missing** | **Missing** |
| Audit | `src/BitWrite.OcelotConsole.Api/Controllers/AuditController.cs` (`[Route("api/v1/audit")]`, read list/id/stats) → `Application/UseCases/Audit/*QueryHandler` (L11-13) | **Unverified** | **Missing** | **Missing** |
| Settings | `src/BitWrite.OcelotConsole.Api/Controllers/SettingsController.cs` (`[Route("api/v1/settings")]`) → `Application/UseCases/SystemSettings/*` (L21); role policy `Admin` on specific actions (L114). Contract: `tests/BitWrite.OcelotConsole.Api.Tests/Controllers/SettingsControllerContractTests.cs` (L31,52,63). | **Unverified** (not permission-based) | **Partially implemented** (role-based, limited) | **Partially implemented** |
| Identity & session/token administration | `src/BitWrite.OcelotConsole.Api/Controllers/IdentityController.cs` → `Application/UseCases/Identity/*` session/token handlers (L14-19); class `[Authorize(Policy="Operator")]` (L11) and action-level attributes (e.g. L115) but **`Operator` policy not registered** in `Program.cs` (see §11.2). | **Unverified** (not permission-based) | **Partially implemented & misaligned** (role policy mismatch) | **Partially implemented** |

Candidate code strings from earlier drafts (e.g. `services.read`, `routes.create`) remain **Unverified/unapproved** and must not be treated as catalog entries. A final per-operation mapping (operation, exact catalog code, all entry/execution paths, enforcement point, test evidence) must be derived from verified source review; full call-graph/background/internal paths remain **Unverified** where not exhaustively inspected.

## 11. Source verification findings

### 11.1 Verification basis

- **Repository:** `develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6` (read-only inspection; no source/test/Redis changes). Prior model-generated observations were re-verified where cited below.

### 11.2 Verified findings

| Area | Verified observation (evidence) | Status |
|---|---|---|
| Role aggregate — storage & normalization | `src/BitWrite.OcelotConsole.Domain/Aggregates/Role/Role.cs`: Permission codes stored as `List<string> _permissions` / `IReadOnlyList<string> Permissions` (L17,L27). `AddPermission` (L65-78) and `RemovePermission` (L84-97) reject empty (`INVALID_PERMISSION`) and normalize `Trim().ToLowerInvariant()` (L70,L89). | **Implemented** |
| Role duplicate invariant & events | `AddPermission` throws `DomainException(..., "PERMISSION_ALREADY_ASSIGNED")` on duplicate (L71-72). Emits `PermissionAddedToRole` (L78) / `PermissionRemovedFromRole` (L97); create/update emit `RoleCreated`/`RoleUpdated` (L59,L101). Records: `src/BitWrite.OcelotConsole.Domain/Events/DomainEvents.cs` L131-134. | **Implemented** |
| Role catalog validation & lifecycle | No catalog/assignability validation in `Role`. `Role` has no Active/Inactive (or equivalent) lifecycle state or activation/deactivation (properties: `Id, Name, Description, IsSystemRole, CreatedAt, UpdatedAt, UpdatedBy` only). **Target requirement (only active Roles contribute) is Approved; current implementation Missing (ownership T-20).** | **Missing** |
| User aggregate — roles & lifecycle | `src/BitWrite.OcelotConsole.Domain/Aggregates/User/User.cs`: `List<string> _roleIds` / `RoleIds` (L20,L35); `AssignRole` enforces disabled-user/duplicate (`USER_DISABLED`, `ROLE_ALREADY_ASSIGNED`, L156-160) and emits `RoleAssignedToUser` (L163); `RemoveRole` enforces missing (`ROLE_NOT_ASSIGNED`, L171-175). Active/Disabled lifecycle via `Status`, `Disable`/`Enable` (L27,115-140). `AssignRole`/`RemoveRole` **do not normalize** supplied role identifier. No effective Permission calculation. | **Partially implemented** (refs & lifecycle) / **Missing** (effective perms, identifier normalization) |
| Role repository interface | `src/BitWrite.OcelotConsole.Application/Interfaces/IRoleRepository.cs` — `GetByIdAsync`, `GetByNameAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` (L6-14). | **Implemented** |
| Redis Role persistence | `src/BitWrite.OcelotConsole.Infrastructure/Redis/RedisRoleRepository.cs` — JSON serialize/deserialize of full `Role` (L26,36,58), keys `RedisKeyHelper.IamRole`/index `IndexRoles` (L21,31,41,57-62,72,77), `GetByNameAsync` via `RoleId.From(name)` (L31), `UpdateAsync`→`AddAsync` (L65-68). Keys: `src/BitWrite.OcelotConsole.Infrastructure/Redis/RedisKeyHelper.cs` `IamNamespace="ocelot:iam:v1"` (L71), `IamRole` (L105), `IndexRoles` (L108). Permission strings persisted in Role object; **no official Permission Catalog in Redis**. | **Implemented** (persistence) |
| User-Role use cases | `Application/UseCases/Role/AssignRoleCommandHandler.cs` (L22-42): loads user/role, `user.AssignRole(role.Name,...)`, persists. `RemoveRoleCommandHandler.cs` (L25-45): similar (`IDomainEventDispatcher` injected but unused). `GetUserRolesQueryHandler.cs` (L21-45): resolves stored values via `RoleId.From(...)` (L31-32). **Verified semantic mismatch risk:** assign stores `role.Name`, get parses as `RoleId`. No role-active checks; no app-level idempotent Permission assignment (not applicable here). | **Partially implemented** + **verified mismatch risk** |
| Role-Permission application use cases | No add/remove Permission-to-Role handlers/use cases found in `src/`. Domain supports mutation/events but no Application boundary with catalog validation/idempotency. | **Missing** |
| JIT provisioning | `src/BitWrite.OcelotConsole.Application/UseCases/Identity/JitProvisionCommandHandler.cs` assigns placeholder default `"restricted-user"` (L83-86). Policy/configuration **Unverified/incomplete**. | **Partially implemented** |
| API authorization (registered vs referenced) | `src/BitWrite.OcelotConsole.Api/Program.cs`: JWT Bearer (L315-321) and role policies `Admin`, `GatewayManager`, `RouteManager`, `SnapshotManager` (L325-328). `src/BitWrite.OcelotConsole.Api/Controllers/IdentityController.cs` references `[Authorize(Policy="Operator")]` (L11,115) — **`Operator` not registered** in `Program.cs` (verified mismatch). `SettingsController` uses `Admin` on specific actions (L114). | **Partially implemented & misaligned** |
| Protected operations enforcement | Core controllers `ServicesController`, `RoutesController`, `GlobalConfigurationController`, `SnapshotsController`, `PublicationsController`, `AuditController` show no `[Authorize]`/permission checks in inspected entry points; no Permission Catalog, no Effective Permission calculation, no Permission Authorization Evaluator found. Direct handler/background/internal invocation paths **Unverified** (not exhaustively traced). | **Missing** (comprehensive permission enforcement) / paths **Unverified** |
| CQRS dispatch | Explicit handler classes with `HandleAsync`, DI-registered and injected into controllers (e.g. service/route/snapshot/publication handlers). Full call graph not exhaustively traced. | **Partially implemented** (pattern) / graph **Unverified** |
| Audit | Audit infrastructure/handlers and `AuditRecorded` dispatch exist in parts of application (per prior inspection); RBAC/security-audit coverage completeness **Unverified**. | **Unverified** |

### 11.3 Role identifier consistency (verified mismatch; follow-up recommendation)

- **Verified:** `AssignRoleCommandHandler` stores `role.Name` (`user.AssignRole(role.Name,...)`, L32); `GetUserRolesQueryHandler` parses stored values with `RoleId.From(...)` (L31-32) — semantic mismatch risk.
- **Proposed direction (recommendation, requires confirmation in T-20/T-21 — not an approved implementation detail):** Use `RoleId` as the stable identity in User-Role assignments; do not use mutable `Role.Name` as the persistent relationship identifier.

### 11.4 Remaining verification required

Full command/query dispatch and **all** direct/background/internal invocation paths, exhaustive protected-operation enumeration, user/role serialization edge cases, and complete test coverage mapping remain **Unverified** where not traced above; they must be verified with exact source/test evidence before implementation claims are finalized.

## 12. Error and API result semantics

The API should distinguish at least these conceptual outcomes:

- Permission code is unknown.
- Permission exists but is Retired/non-assignable.
- Permission is successfully added.
- Permission was already assigned and the request completed as a successful no-op.
- Role or User does not exist.
- Caller is not authorized.

Exact HTTP status codes and error identifiers must follow existing API conventions and be documented consistently. The idempotent duplicate case must not be reported as a failure.

Avoid leaking sensitive internal details in authorization-denial responses.

## 13. Caching and invalidation

Caching is not defined by T-18, but any future cache must preserve the approved authorization semantics.

Implementation design must consider invalidation or versioning when:

- A User is activated or deactivated.
- A Role is activated or deactivated.
- A User-Role assignment changes.
- A Role-Permission assignment changes.
- A Permission lifecycle status changes in a product release.
- A catalog version changes.

A cache must never make a Retired or unknown Permission effective. Cache consistency guarantees and token claim behavior belong to the relevant token/authorization implementation work items.

## 14. Test specification

### 14.1 Catalog tests

- Catalog codes are unique.
- Known Active Permission resolves.
- Unknown code does not resolve.
- Retired Permission resolves as Retired but is not assignable/runtime-effective.
- Catalog metadata is not sourced from Redis.
- Normalization behavior is consistent and non-ambiguous.

### 14.2 Assignment tests

- Assigning a valid, Active, assignable Permission adds it to the Role.
- The successful add produces the expected single Domain event.
- Assigning an unknown Permission is rejected and does not mutate the Role.
- Assigning a Retired Permission is rejected and does not mutate the Role.
- Assigning a non-assignable Permission is rejected.
- Assigning an already-assigned Permission returns success/no-change.
- Duplicate assignment does not call the Domain add method.
- Duplicate assignment does not emit another Domain event.
- Duplicate assignment does not cause an unnecessary persistence write.
- Concurrent duplicate assignments preserve one assignment and one logical add event.

### 14.3 Retirement tests

- A Retired Permission cannot be newly assigned.
- An existing Redis reference to a Retired Permission is retained unless explicitly removed through a separate administrative operation.
- A Retired Permission is excluded from effective permissions.
- A Retired Permission never produces an authorization Allow.
- No implicit replacement or mapping occurs.

### 14.4 Effective Permission tests

- Active User + active Role + Active Permission results in an effective Permission.
- Inactive User results in an empty effective set.
- Inactive Role contributes no Permission.
- Missing Role contributes no Permission.
- Unknown stored Permission code is ignored.
- Retired stored Permission code is ignored.
- Permissions from multiple active Roles are unioned.
- Duplicate codes do not duplicate effective permissions.

### 14.5 Evaluator and enforcement tests

- Required effective Permission results in Allow.
- Missing required Permission results in Deny.
- Unknown required Permission results in Deny.
- Retired required Permission results in Deny.
- Inactive User results in Deny.
- Inactive assigned Role cannot grant access.
- Direct application invocation cannot bypass authorization.
- API and non-API entry points enforce the same required Permission.
- Background/system execution follows an explicit trusted identity/policy.
- Business validation remains independently enforced.

## 15. Implementation sequencing and ownership

T-18 produces the architecture and implementation specification only. **Do not assign implementation of the Permission Catalog to T-18.** T-18 remains documentation/specification only.

The following work items are expected to own subsequent work, subject to confirmation against the current backlog:

| Work item | Expected responsibility |
|---|---|
| T-13 | User-Role assignment / RBAC integration (including Role reference semantics alignment in assignments, once confirmed per §11.3) |
| T-14 | Default-deny authorization and enforcement integration (evaluator, effective-permission usage, enforcement coverage) |
| T-20 | Domain model details and invariants, including **Role lifecycle state** (active/inactive; target Approved, current Missing) and confirmation of Role identifier semantics |
| T-21 | User/Role administration use cases (including Role-Permission assignment use cases and Application-level validation/idempotent behavior) |
| T-23 | Restricted JIT default-role policy (replace verified placeholder `restricted-user` in `JitProvisionCommandHandler`) |
| T-25 | Management API token model and JWT authorization claims |
| T-26 | Remaining IAM design-item resolution |

Complete operation-level enforcement may require a **dedicated work item** or explicit tasks under the appropriate authorization-enforcement item. If implementation of the Permission Catalog itself requires a dedicated work item, recommend a **separate item** rather than expanding T-18. Do not move implementation work into T-18 without an explicit backlog decision.

## 16. T-18 deliverables and acceptance criteria

T-18 deliverables:

1. Approved Permission Catalog and Authorization Model document.
2. This implementation specification.
3. Permission-to-operation mapping template with honest evidence statuses.
4. Source-verification findings clearly separated from approved architecture.
5. Traceability to follow-up implementation work items.

T-18 is accepted when:

- All approved architecture decisions are represented consistently.
- No Scope or resource-context authorization remains in the v1 model.
- Product source code and Redis responsibilities are unambiguous.
- Retired Permission behavior is specified exactly.
- Idempotent assignment behavior is specified exactly while preserving the Domain invariant.
- Assignment, effective-permission calculation, evaluation, and enforcement are distinct.
- Candidate Permission codes are not misrepresented as approved.
- Repository claims are marked Unverified until independently checked.
- No product source code is changed as part of T-18.

---

**End of document**
