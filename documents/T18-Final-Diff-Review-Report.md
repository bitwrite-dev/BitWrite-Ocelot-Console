# Final Diff Review — T-18 Documentation Reconciliation (read-only)

- **محدوده:** فقط بررسی و گزارش؛ هیچ تغییری در Source Code/Tests/Redis/Backlog یا اسناد اعمال نشده است.
- **شعبه/کامیت مبنا (طبق گزارش تأیید):** `develop` @ `ea55c6f728dcb0bc6fc47b69db49a809ee7fb4c6`
- **فایل‌های هدف:** 
  - `documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.0.md`
  - `documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md`
  - `documents/BitWrite-Permission-Catalog-Implementation-Specification-v1.0.md`

## 1) خروجی کامل `git diff --stat` (سه فایل)

```text
(خروجی خالی)
```

**توضیح شواهدی:** هر سه فایل در وضعیت `git status --short` به‌صورت `??` (untracked/جدید) هستند، بنابراین `git diff` (tracked diff) برای آن‌ها خروجی تولید نمی‌کند. شمار خطوط فعلی: v1.0 = 747، v1.1 = 466، spec = 454.

```text
$ git status --short -- documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.0.md documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md documents/BitWrite-Permission-Catalog-Implementation-Specification-v1.0.md
?? documents/BitWrite-Permission-Catalog-Implementation-Specification-v1.0.md
?? documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.0.md
?? documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md
```

## 2) Diff کامل فایل‌ها

> توجه: چون فایل‌ها untracked هستند، diff نسبت به `/dev/null` (کل محتوا به‌صورت افزوده `+`) ارائه می‌شود.

### 2.1 `documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.0.md` (کامل)

```diff
--- /dev/null
+++ documents/BitWrite-Permission-Catalog-and-Authorization-Model-v1.0.md
@@ -0,0 +1,747 @@
+> **Superseded — Historical Document**
+>
+> - This document is **superseded by** [`BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md`](BitWrite-Permission-Catalog-and-Authorization-Model-v1.1.md).
+> - It is retained **only as historical documentation** of an earlier draft.
+> - It **must not** be used as the current v1 architecture reference.
+> - Its **Scope-based design** (including Global/Service authorization scopes and scoped assignments/evaluation) is **not part of the approved v1 model**.
+>
+> ---
+
+# BitWrite Ocelot Control Plane
+
+# Permission Catalog and Authorization Model
+
+**Document ID:** BW-IAM-T18\
+**Version:** 1.0\
+**Status:** Draft for Review\
+**Target:** v1\
+**Related Work Items:** T-18 Permission Catalog, T-20 Domain Model
+Details, T-21 User Administration Use Cases\
+**Last Updated:** 2026-11-08
+
+------------------------------------------------------------------------
+
+## 1. Purpose
+
+This document defines the v1 Permission Catalog and the authorization
+model used by BitWrite Ocelot Control Plane.
+
+The goals are to:
+
+-   Establish a stable, product-owned vocabulary of permissions.
+-   Define how permissions are assigned to roles and how roles are
+    assigned to users.
+-   Define the v1 authorization scope model.
+-   Specify permission evaluation rules and security constraints.
+-   Define how the catalog evolves without silently changing existing
+    authorization.
+-   Provide a concrete basis for domain modeling, application use cases,
+    API authorization, and tests.
+
+This document does **not** define authentication, external
+identity-provider integration, JWT claim layout, refresh-token behavior,
+or the complete IAM domain model. Those concerns are covered by their
+respective IAM work items.
+
+## 2. Normative Decisions
+
+The following decisions are normative for v1:
+
+1.  BitWrite owns its Permission definitions and Role management.
+2.  External Identity Providers authenticate identities only. External
+    roles and groups are not automatically imported or mapped to
+    BitWrite Roles.
+3.  Every authenticated identity maps to an internal BitWrite User.
+4.  Permissions are product-defined. Administrators cannot create
+    arbitrary Permission definitions.
+5.  Administrators create and manage Roles by assigning existing catalog
+    permissions.
+6.  A Permission represents an independently grantable
+    capability/action, not merely a UI page or navigation item.
+7.  v1 supports only **Global** and **Service** authorization scopes.
+8.  A Global assignment grants access across all applicable Services
+    only when the Permission explicitly supports Global scope.
+9.  A Service assignment is limited to the identified Service.
+10. Multiple applicable role assignments combine additively. v1 has no
+    explicit deny permission or deny-overrides behavior.
+11. The v1 catalog includes only current and confirmed BitWrite product
+    capabilities. Planned or hypothetical capabilities must not be
+    added.
+12. Plugin-specific permission extensions are outside the v1 Core
+    catalog. This document preserves an extension path but does not
+    define speculative Plugin permissions.
+
+## 3. Scope and Evidence Boundary
+
+### 3.1 In scope
+
+This document defines:
+
+-   Permission identity and naming.
+-   Permission categories and the initial capability-based catalog.
+-   Supported authorization scopes.
+-   Role-Permission and User-Role assignment semantics.
+-   Permission evaluation requirements.
+-   Catalog versioning, synchronization, and compatibility rules.
+-   API and application-layer enforcement requirements.
+-   Security and testing requirements.
+
+### 3.2 Out of scope
+
+-   Authentication and credential policies.
+-   External authentication protocol details.
+-   JWT and refresh-token claim design.
+-   MFA implementation.
+-   Environment- and Gateway-level scopes.
+-   Attribute-based authorization (ABAC).
+-   Explicit deny permissions.
+-   Plugin-defined permissions in v1.
+-   UI menu visibility as an authorization mechanism.
+
+### 3.3 Implementation verification note
+
+The GitHub `develop` branch could not be fetched during preparation of
+this document. Therefore, the catalog below is a **v1 capability
+baseline derived from the confirmed product architecture and
+decisions**, not a claim that every listed operation or API endpoint has
+already been implemented.
+
+Before marking T-18 Done, map every candidate permission to: - An
+existing domain/application capability. - The application use case or
+operation that enforces it. - The relevant API endpoint(s), where
+applicable. - A test proving authorization enforcement.
+
+Remove any permission that has no real v1 capability or execution path.
+Do not add a permission merely because a feature is planned.
+
+## 4. Authorization Architecture
+
+The conceptual authorization relationship is:
+
+``` text
+Permission Catalog
+       |
+       v
+      Role ---- RolePermission ---- Permission
+       |
+       v
+UserRoleAssignment
+       |
+       v
+      User
+       |
+       v
+Authorization Evaluation
+(permission, scope, resource)
+```
+
+The catalog defines **what actions exist**. Roles group permissions.
+User-role assignments determine **who receives those permissions and
+within which scope**. The authorization evaluator decides whether a
+specific operation is allowed for a specific resource.
+
+### 4.1 Core concepts
+
+  -----------------------------------------------------------------------
+  Concept                             Responsibility
+  ----------------------------------- -----------------------------------
+  Permission Definition               Product-owned definition of an
+                                      independently grantable action
+
+  Role                                Named collection of Permission
+                                      assignments
+
+  Role-Permission Assignment          Associates a Role with a catalog
+                                      Permission
+
+  User-Role Assignment                Associates an internal User with a
+                                      Role and authorization scope
+
+  Authorization Scope                 Limits where an assignment is
+                                      effective
+
+  Authorization Evaluator             Evaluates permission and scope
+                                      against the requested
+                                      operation/resource
+  -----------------------------------------------------------------------
+
+These are conceptual responsibilities. Exact aggregate boundaries and
+persistence shapes are resolved by T-20.
+
+## 5. Permission Definition
+
+Each Permission definition must provide, at minimum:
+
+  -----------------------------------------------------------------------
+  Attribute                           Meaning
+  ----------------------------------- -----------------------------------
+  Code                                Immutable, unique, machine-readable
+                                      identifier
+
+  Name                                Human-readable display name
+
+  Description                         Exact capability granted
+
+  Category                            Product capability group
+
+  SupportedScopes                     Scopes in which this Permission may
+                                      be granted
+
+  Status                              Whether the definition is currently
+                                      assignable
+
+  CatalogVersion                      Catalog version in which the
+                                      definition is introduced or changed
+  -----------------------------------------------------------------------
+
+The final implementation may use different property names or a different
+representation. The semantic requirements are mandatory.
+
+### 5.1 Permission code rules
+
+Permission codes:
+
+-   Must be unique and stable.
+-   Must be lowercase.
+-   Must use dot-separated capability/action segments.
+-   Must not contain localized text.
+-   Must not depend on UI route names or controller names.
+-   Must not be reused for a different meaning.
+-   Must not change when display names are translated or updated.
+
+Examples:
+
+-   `services.read`
+-   `services.create`
+-   `routes.update`
+-   `snapshots.publish`
+
+A code is an authorization contract. Renaming or changing its meaning is
+a compatibility change and requires an explicit migration strategy.
+
+### 5.2 Permission granularity
+
+Use separate permissions when an operation represents a meaningful,
+independently grantable authority.
+
+Ordinary resource operations may use `read`, `create`, `update`, and
+`delete` when those operations genuinely exist. Security-sensitive or
+operationally significant actions must have explicit permissions rather
+than being hidden inside a broad update permission.
+
+Examples: - Publishing a snapshot is distinct from editing
+configuration. - Rolling back a snapshot is distinct from reading
+snapshots. - Assigning a Role is distinct from editing a User profile. -
+Disabling a User is distinct from updating ordinary profile fields.
+
+Do not create CRUD permissions for resources that do not support those
+operations.
+
+## 6. Initial v1 Permission Catalog
+
+The following catalog is the initial capability baseline. It must be
+reconciled against the actual `develop` implementation before being
+treated as the final executable catalog.
+
+### 6.1 Service Management
+
+  -----------------------------------------------------------------------
+  Permission Code         Capability              Supported Scope
+  ----------------------- ----------------------- -----------------------
+  `services.read`         View Service            Global, Service
+                          definitions and details 
+
+  `services.create`       Create a Service        Global
+
+  `services.update`       Modify a Service        Global, Service
+                          definition              
+
+  `services.delete`       Delete a Service,       Global, Service
+                          subject to domain       
+                          constraints             
+  -----------------------------------------------------------------------
+
+A Service-scoped assignment must not allow creating a new Service.
+Service creation is a global management operation.
+
+### 6.2 Route Management
+
+  -----------------------------------------------------------------------
+  Permission Code         Capability              Supported Scope
+  ----------------------- ----------------------- -----------------------
+  `routes.read`           View Routes             Global, Service
+
+  `routes.create`         Create a Route under an Global, Service
+                          authorized Service      
+
+  `routes.update`         Modify a Route under an Global, Service
+                          authorized Service      
+
+  `routes.delete`         Delete a Route under an Global, Service
+                          authorized Service      
+  -----------------------------------------------------------------------
+
+The authorization evaluator must resolve the owning Service of a Route
+before evaluating a Service-scoped assignment. A caller must not be able
+to bypass Service restrictions by supplying a Route identifier directly.
+
+### 6.3 Gateway Configuration Management
+
+  ---------------------------------------------------------------------------------
+  Permission Code                   Capability              Supported Scope
+  --------------------------------- ----------------------- -----------------------
+  `configuration.global.read`       View global Ocelot      Global
+                                    configuration           
+
+  `configuration.global.update`     Modify global Ocelot    Global
+                                    configuration           
+
+  `configuration.features.read`     View supported Gateway  Global
+                                    feature configuration   
+
+  `configuration.features.update`   Modify supported        Global
+                                    Gateway feature         
+                                    configuration           
+  ---------------------------------------------------------------------------------
+
+Only configuration areas actually supported by the v1 product should be
+included. Do not introduce permissions for unsupported Ocelot sections
+or future configuration features.
+
+### 6.4 Snapshot Management
+
+  -----------------------------------------------------------------------
+  Permission Code         Capability              Supported Scope
+  ----------------------- ----------------------- -----------------------
+  `snapshots.read`        View snapshots and      Global
+                          snapshot details        
+
+  `snapshots.create`      Create a snapshot from  Global
+                          a validated             
+                          configuration state     
+
+  `snapshots.rollback`    Restore a previous      Global
+                          snapshot as the         
+                          selected configuration  
+                          state                   
+
+  `snapshots.publish`     Publish an eligible     Global
+                          snapshot to the         
+                          configured Gateway      
+                          runtime target          
+  -----------------------------------------------------------------------
+
+Publishing and rollback are separate high-impact actions. A user who can
+edit configuration or create snapshots must not implicitly receive
+publishing authority.
+
+If the implementation distinguishes building, validating, snapshot
+creation, and publication as separate application operations, add
+separate permissions only when each operation is independently grantable
+and actually exists in v1. Do not infer such permissions from internal
+workflow steps.
+
+### 6.5 Audit
+
+  Permission Code   Capability                                  Supported Scope
+  ----------------- ------------------------------------------- -----------------
+  `audit.read`      Read audit records exposed by the product   Global
+
+Audit access is read-only in this catalog. Do not create audit
+update/delete permissions.
+
+### 6.6 Catalog inclusion gate
+
+The tables above are candidates for the v1 baseline, not permission to
+add speculative features. Before implementation or catalog seeding, each
+row must pass all of these checks:
+
+1.  The corresponding capability exists in the v1 product scope.
+2.  A concrete application use case or operation exists.
+3.  The operation has a clear authorization enforcement point.
+4.  Its scope is meaningful and can be evaluated reliably.
+5.  A positive and negative authorization test can be written.
+
+If a row fails any check, exclude it from the active catalog until the
+capability is implemented and approved for v1.
+
+## 7. Authorization Scope Model
+
+### 7.1 v1 scopes
+
+  -----------------------------------------------------------------------
+  Scope                   Meaning                 Scope identifier
+  ----------------------- ----------------------- -----------------------
+  Global                  Permission applies      No resource identifier
+                          across all resources of 
+                          the relevant            
+                          capability, subject to  
+                          the Permission's        
+                          supported scopes        
+
+  Service                 Permission applies only Internal Service
+                          to one specific Service identifier
+  -----------------------------------------------------------------------
+
+Environment and Gateway scopes are explicitly deferred. The model may be
+extensible, but v1 must not implement unused scope types or speculative
+scope resolution.
+
+### 7.2 Supported scopes are defined per Permission
+
+A Permission definition declares which scopes are valid for it.
+
+Examples: - `services.create` supports Global only. - `routes.update`
+may support Global and Service. - `configuration.global.update` supports
+Global only. - `snapshots.publish` supports Global only in v1.
+
+An assignment using an unsupported scope must be rejected during Role
+configuration or assignment validation. It must never be silently
+widened to Global.
+
+### 7.3 Evaluation rules
+
+For a requested operation:
+
+1.  Resolve the internal User from the authenticated principal.
+2.  Resolve the required Permission code from the application operation.
+3.  Resolve the resource context, including its owning Service when
+    relevant.
+4.  Load the User's effective Role assignments.
+5.  Consider only active assignments and active Roles.
+6.  Match the Permission through the Role-Permission assignments.
+7.  Check that the assignment scope is supported by the Permission.
+8.  Check that the assignment scope covers the requested resource.
+9.  Allow if at least one applicable assignment grants the required
+    Permission; otherwise deny.
+
+### 7.4 Global and Service precedence
+
+v1 uses additive grants:
+
+-   A matching Global assignment grants the Permission across the
+    applicable resource set when Global is supported by that Permission.
+-   A matching Service assignment grants the Permission only for that
+    Service.
+-   Multiple matching assignments are combined as a union.
+-   There are no explicit deny assignments.
+-   A Service-scoped grant cannot override or expand another
+    assignment's scope.
+-   An inactive Role or revoked assignment contributes no permissions.
+
+A Global assignment is not valid for a Permission that supports only
+Service scope, and vice versa.
+
+### 7.5 Resource ownership
+
+For Service-owned resources such as Routes, authorization must be
+evaluated against the authoritative Service relationship in the
+domain/application data.
+
+Do not trust a Service identifier supplied by the client without
+verifying that the target resource belongs to that Service.
+
+## 8. Role and Assignment Rules
+
+1.  A Role may contain only Permission codes present in the active
+    catalog.
+2.  Duplicate Role-Permission assignments must be prevented.
+3.  A Role may contain permissions with different supported scopes, but
+    each User-Role assignment must use a valid scope for the permissions
+    it grants.
+4.  If the current persistence model stores scope at User-Role
+    assignment level, all permissions granted by that assignment must be
+    valid for that scope. If mixed-scope grants are required, the model
+    must represent scope at a sufficiently precise assignment boundary.
+    This is a T-20 modeling decision.
+5.  A User may have multiple Role assignments.
+6.  Revoking one assignment must not revoke another independent
+    assignment.
+7.  Deactivating a Role must make its permissions ineffective without
+    deleting historical audit data.
+8.  Role deletion must be constrained when referenced by assignments or
+    historical records. Prefer deactivation where historical
+    traceability is required.
+9.  Assigning a Role and changing its permissions are security-sensitive
+    administrative operations and must be audited.
+10. The restricted default Role used for first-time external-user JIT
+    provisioning must be explicitly configured and must never grant
+    administrative privileges by fallback.
+
+## 9. Catalog Ownership and Lifecycle
+
+### 9.1 Product-owned catalog
+
+The Core Permission Catalog is shipped and versioned with BitWrite
+product code. It is not an administrator-managed CRUD resource.
+
+Administrators may: - View Permission definitions. - View which
+permissions are assigned to a Role. - Assign existing permissions to
+Roles, subject to authorization and scope rules.
+
+Administrators may not: - Create arbitrary Permission definitions. -
+Change Permission codes. - Change the meaning of a Permission. - Add
+permissions that do not exist in the product catalog.
+
+### 9.2 Synchronization
+
+On application startup or controlled upgrade, the application must
+reconcile the persisted Role-Permission references with the product
+catalog.
+
+Synchronization must: - Add newly introduced definitions. - Update safe
+descriptive metadata. - Preserve valid Role-Permission assignments. -
+Detect references to unknown or retired codes. - Avoid silently granting
+a replacement permission. - Avoid silently deleting historical
+assignment evidence.
+
+Catalog synchronization must be idempotent and safe in multi-instance
+deployments.
+
+### 9.3 Retirement and breaking changes
+
+A Permission code must never be reused for a different capability.
+
+When a permission is retired: 1. Mark it unavailable for new
+assignments. 2. Identify Roles that still reference it. 3. Define an
+explicit migration or removal policy. 4. Preserve audit/history
+information. 5. Do not automatically map it to a broader permission.
+
+A permission rename is treated as a migration, not a display-name
+update.
+
+### 9.4 Versioning
+
+The catalog must have an explicit version. The implementation may use a
+product release version plus a catalog schema/version identifier,
+provided it can detect incompatible changes.
+
+Changes that alter the authority granted by a Permission require
+security review and an explicit migration plan.
+
+## 10. Authorization Enforcement
+
+Authorization must be enforced at the application boundary and must not
+depend on the dashboard hiding buttons or navigation items.
+
+Requirements:
+
+-   Every protected application use case declares its required
+    Permission.
+-   API endpoints must delegate to application operations that enforce
+    the same authorization policy.
+-   Direct invocation of an application handler/use case must not bypass
+    authorization.
+-   Service ownership must be checked for Service-scoped operations.
+-   Missing, malformed, unknown, or unresolved authorization context
+    results in denial.
+-   Authorization failures must not disclose protected resource data.
+-   Background and internal operations must use an explicit trusted
+    execution policy; they must not impersonate an arbitrary
+    administrator.
+-   Tests must cover both API entry and direct application use-case
+    invocation where applicable.
+
+UI visibility is a usability feature only. It is not a security
+boundary.
+
+## 11. Caching and Authorization Changes
+
+Authorization data may be cached for performance, but cache behavior
+must not allow revoked access to remain effective indefinitely.
+
+The implementation must define: - Cache keys by internal User and
+authorization context. - A bounded cache lifetime. - Invalidation when
+User-Role assignments change. - Invalidation when Role-Permission
+assignments change. - Invalidation when a Role is disabled. -
+Invalidation when a Permission is retired or its authority changes. -
+Multi-instance invalidation behavior.
+
+The JWT authorization version claim (`authz_ver`) and its exact
+semantics are owned by T-25/T-26. This document requires a reliable
+invalidation strategy but does not prescribe the JWT representation.
+
+## 12. Audit Requirements
+
+The existing BitWrite Audit Context must be reused. This document does
+not introduce a second audit subsystem.
+
+At minimum, audit: - Role creation, update, and deactivation. -
+Role-Permission assignment and removal. - User-Role assignment and
+revocation. - Changes to Service-scoped assignments. - Permission
+catalog upgrades that affect effective authorization. - High-impact
+operations such as snapshot publication and rollback, where those
+operations are implemented.
+
+Audit records should identify the actor, target, operation, timestamp,
+and relevant before/after context. Secrets and credential material must
+never be written to audit records.
+
+## 13. API and Use-Case Mapping
+
+The Permission Catalog is not an API route catalog. A single Permission
+may protect multiple endpoints and application operations.
+
+For each Permission, maintain an implementation mapping with these
+columns:
+
+  ---------------------------------------------------------------------------------------
+  Permission Code       Application   API           Resource    Enforcement   Test
+                        Operation     Endpoint(s)   Context                   
+  --------------------- ------------- ------------- ----------- ------------- -----------
+  `services.read`       To be         To be         Service /   Required      Required
+                        verified      verified      Global                    
+
+  `routes.update`       To be         To be         Owning      Required      Required
+                        verified      verified      Service                   
+
+  `snapshots.publish`   To be         To be         Global      Required      Required
+                        verified      verified                                
+  ---------------------------------------------------------------------------------------
+
+The mapping must be completed from the actual source code before T-18 is
+closed. Do not invent endpoint names.
+
+## 14. Security Requirements
+
+-   Deny by default.
+-   Never treat authentication as authorization.
+-   Never trust external IdP role/group claims as BitWrite permissions.
+-   Never use email or username as the stable external identity key.
+-   Never grant an administrator role as a JIT fallback.
+-   Do not accept unknown Permission codes in Role updates.
+-   Do not allow unsupported scopes.
+-   Do not allow Service-scope bypass through client-supplied resource
+    identifiers.
+-   Keep privileged actions independently permissioned.
+-   Audit security-administration changes.
+-   Avoid logging credentials, tokens, refresh tokens, or secrets.
+-   Ensure cache invalidation after permission-affecting changes.
+
+## 15. Testing Strategy
+
+### 15.1 Catalog tests
+
+-   Every Permission code is unique.
+-   Every Permission has a non-empty description and category.
+-   Every Permission declares at least one supported scope.
+-   Codes follow naming rules.
+-   No retired code is reused.
+-   Every active Permission maps to a confirmed v1 capability and an
+    enforcement point.
+
+### 15.2 Evaluation tests
+
+-   User with a matching Global assignment is allowed where Global is
+    supported.
+-   User with a matching Service assignment is allowed only for that
+    Service.
+-   User with a Service assignment is denied for another Service.
+-   User without a matching Permission is denied.
+-   Inactive Role and revoked assignment do not grant access.
+-   Multiple assignments combine additively.
+-   Unsupported scope is rejected.
+-   Unknown Permission code is rejected.
+-   Route authorization resolves the actual owning Service.
+-   Client-supplied Service identifier cannot bypass resource ownership
+    checks.
+
+### 15.3 Lifecycle tests
+
+-   Catalog synchronization is idempotent.
+-   New catalog entries are added without losing existing valid Role
+    assignments.
+-   Unknown persisted Permission references are detected.
+-   Retired permissions are not silently remapped.
+-   Multi-instance synchronization does not create duplicate
+    definitions.
+-   Authorization cache is invalidated after Role or assignment changes.
+
+### 15.4 API and application tests
+
+-   Protected endpoints reject unauthorized callers.
+-   Application use cases cannot be invoked to bypass authorization.
+-   UI hiding does not substitute for server-side enforcement.
+-   Publish and rollback require their own explicit permissions.
+
+## 16. Future Extension Policy
+
+The design should allow future additions without implementing them in
+v1.
+
+Potential future dimensions include: - Environment scope. - Gateway
+scope. - Plugin-provided permission extensions. - More advanced policy
+evaluation.
+
+These are extension points only. They are not part of the v1 catalog and
+must not be seeded, exposed, or treated as supported authorization
+capabilities until separately approved.
+
+Any future Plugin permission extension contract must: - Use namespaced,
+collision-resistant codes. - Prevent plugins from overwriting Core
+permission definitions. - Validate plugin ownership and lifecycle. -
+Define behavior when a plugin is disabled or uninstalled. - Be covered
+by licensing and security policies.
+
+No Plugin permission codes are defined by this v1 document.
+
+## 17. T-18 Completion Criteria
+
+T-18 can be marked Done only when:
+
+-   [ ] The v1 Permission Catalog is reviewed against the current
+    `develop` source code.
+-   [ ] Every active Permission maps to a real, confirmed v1 capability.
+-   [ ] Every Permission has a stable code, description, category, and
+    supported scope.
+-   [ ] Global and Service scope semantics are approved.
+-   [ ] Role-Permission and User-Role assignment rules are approved.
+-   [ ] Unknown and retired Permission behavior is specified.
+-   [ ] Catalog synchronization/versioning requirements are approved.
+-   [ ] Authorization enforcement points are mapped to application
+    operations and APIs.
+-   [ ] Positive and negative authorization tests are identified.
+-   [ ] T-20 and T-21 can use this document without inventing additional
+    permissions.
+
+## 18. Open Decisions for Related Work Items
+
+  -----------------------------------------------------------------------
+  Topic                               Owner
+  ----------------------------------- -----------------------------------
+  Exact aggregate boundaries and      T-20
+  persistence representation          
+
+  Whether scope is stored on          T-20
+  User-Role assignment or a more      
+  granular grant                      
+
+  Concrete Role administration use    T-21
+  cases and validation                
+
+  JWT permission representation and   T-25
+  token size strategy                 
+
+  Authorization versioning and cache  T-25 / T-26
+  invalidation contract               
+
+  Exact API-to-Permission mapping     T-18 implementation review
+  from source code                    
+
+  Default restricted Role             IAM / T-23
+  provisioning behavior               
+  -----------------------------------------------------------------------
+
+## 19. Final v1 Position
+
+BitWrite v1 uses a product-owned, code-defined Permission Catalog;
+administrator-managed Roles; scoped User-Role assignments; and
+deny-by-default authorization.
+
+The catalog is deliberately limited to confirmed product capabilities.
+Global and Service are the only supported scopes. Grants are additive,
+and high-impact operations such as publishing and rollback are
+independently permissioned.
+
+No speculative Plugin permissions, Environment scope, Gateway scope, or
+future feature permissions belong in the v1 catalog.
```

## 3) خروجی `git diff --check`

```text
(خروجی خالی)
---RC=0---
```

## 4) تأیید تغییرات فایل v1.0 (فقط Supersession Notice، بدنه تاریخی بدون تغییر)

- **Pass (با شواهد):** v1.0 در diff به‌صورت فایل جدید (747 خط) اضافه شده است. خطوط 1–9 دقیقاً بلوک اعلان جایگزینی (Supersession) + جداکننده `---` هستند؛ از خط 10 سند با همان سرآغاز تاریخی `# BitWrite Ocelot Control Plane` ادامه می‌یابد.
- بدنه تاریخی از خط 10 تا انتها همان محتوای پیشین (شامل مدل Scopeمحور، بخش‌ها و موقعیت نهایی) را حفظ کرده است: انتهای سند (بخش 19) همچنان «scoped User-Role assignments… Global and Service are the only supported scopes…» را دارد و تنها پیشوند اعلان در ابتدا افزوده شده است.
- بنابراین: **تغییر مؤثر نسبت به محتوای تاریخی = افزودن Supersession Notice در ابتدای سند؛ بدنه تاریخی تغییری نکرده است.**

## 5) تطبیق تصمیمات مصوب در دو سند مرجع (v1.1 و Implementation Spec)

| تصمیم مصوب | v1.1 (`...-v1.1.md`) | Spec (`...Implementation-Specification-v1.0.md`) | تطبیق |
|---|---|---|---|
| No Scope / No Tenant | §3.2 «v1 authorization model has **no Scope concept**»؛ حذف صریح Global/Service/Environment/Gateway scopes، `Scope`/`ScopeId`، scoped assignment/grant، resource-context authorization، ارث‌بری، deny صریح، نگاشت نقش خارجی، **Tenant-based authorization excluded** (L61-71)؛ §13 Out of scope شامل «Tenant or Scope support» (L374)؛ §8/§9 تأکید بر عدم اعمال scope/filter. | §2 بند 5 «v1 has no Scope, scoped assignments, resource-context authorization, permission inheritance, tenant authorization, explicit deny rules, or external role mapping»؛ §7 «No Scope or resource-context filtering is applied in v1» (L216)؛ پذیرش «No Scope or resource-context authorization remains in the v1 model» (L443). | **Pass** (هم‌راستا و صریح در هر دو) |
| Source-owned Permission Catalog (SoT در سورس‌کد) | §4.1 «owned by the BitWrite product and defined in product source code» و **single source of truth (SoT)** (L89-91)؛ §4.2 ممنوعیت CRUD تعاریف توسط ادمین؛ متادیتا محصول-مالک. | §2 بند 1-2، §3 جدول (Official Permission definitions/metadata → Product source code)، §4.1/§4.2؛ پذیرش «Product source code and Redis responsibilities are unambiguous» (L444). | **Pass** |
| Redis فقط ارجاع Permission نقش‌ها را نگه می‌دارد (نه تعاریف/اعتبار) | §5 Redis نگهدار داده Role و رشته‌های کد Permission؛ **MUST NOT** منبع حقیقت وجود/متادیتا/assignability/کاتالوگ باشد؛ resolve در برابر کاتالوگ محصول الزامی (L136-156). §14.1 تأیید «No official Permission Catalog stored in Redis» (L398). | §2 بند 3، §5 لیست موارد غیرمجاز در Redis + الزام resolve؛ §5.1 یافته تأییدشده: کدها در شیء Role، «**no official Permission Catalog or Permission metadata is stored in Redis**» (L127). | **Pass** |
| سیاست زمان اجرای Retired Permission | §6.2 **Approved**: غیرقابل انتساب جدید، حفظ ارجاعات تاریخی در Redis (بدون حذف خودکار)، **حذف از Effective Permissions در زمان اجرا**، عدم اعطای دسترسی هرگز، بدون نگاشت/جایگزینی خودکار (L164-177). §8/§9 تبعات (Retired مستثنا) (L250,268). | §2 بند 8؛ §6.2 جدول (Retired → Reject)؛ §7 الگوریتم (Retired/not runtime-effective ادامه/حذف اثر)؛ §14.3 تست‌ها (غیرقابل انتساب جدید، حفظ ارجاع، مستثنا از مؤثر، هرگز Allow)؛ پذیرش «Retired Permission behavior is specified exactly» (L445). | **Pass** |
| انتساب تکراری Permission به‌صورت Idempotent (سطح Application، حفظ ناوردای Domain) | §7.2 **Approved**: «successful no-op at the Application/use-case boundary»، بررسی عضویت، عدم فراخوانی Domain add، عدم رویداد تکراری، عدم نوشتن غیرضروری (L200-216). §7.3 حفظ ناوردای دامنه `PERMISSION_ALREADY_ASSIGNED` (L217-220). §14.1 تأیید ناوردا/رویدادها در دامنه. | §2 بند 9-10؛ §6.1 گام 7؛ §6.2 جدول (already assigned → Success/no-change, No domain call/event/unnecessary write)؛ تأکید بر حفظ ناوردای Domain (L162,185)؛ §14.2 تست‌ها؛ پذیرش «preserving the Domain invariant» (L446). | **Pass** |
| الزام Enforcement کامل عملیات محافظت‌شده | §10.1 «Every protected application operation MUST enforce… before executing»؛ Controller-only ناکافی؛ جلوگیری از bypass مسیرهای مستقیم/جایگزین (L279-284). §10.2/§10.3 نقطه اعمال و عملیات سیستمی/پس‌زمینه. | §2 بند 11؛ §3 «Enforcement coverage → Complete protected application execution path»؛ §9.1 الزام کامل و شناسایی همه مسیرها؛ §10 وضعیت فعلی Missing/Unverified؛ پذیرش الزام پوشش کامل (L443,446). | **Pass** |

## 6) بررسی ثبت اشتباه پیشنهاد/یافته به‌عنوان «Approved» (به‌ویژه RoleId به‌جای Role.Name)

- **RoleId به‌جای Role.Name:** در v1.1 §14.3 و spec §11.3 صراحتاً با برچسب «**Proposed direction for follow-up design (recommendation, not an approved implementation detail)**» و الزام تأیید در **T-20/T-21** ثبت شده است. این مورد **Approved** نشده است.
- **سایر موارد حساس:** «Role lifecycle requirement» در v1.1 §14.2 به‌درستی تفکیک شده (الزام هدف معماری **Approved**، پیاده‌سازی فعلی **Missing**، مالکیت T-20). «Retired runtime policy» (§6.2 v1.1) و «Idempotent assignment» (§7.2 v1.1) همان‌هایی هستند که مصوب اعلام شده‌اند. یافته‌های پیاده‌سازی با برچسب‌های Implemented/Partially/Missing/Unverified گزارش شده‌اند، نه به‌عنوان تصمیم معماری مصوب.
- **کدهای Permission کاندید:** در spec (§10/§11) مواردی مانند `services.read`, `routes.create` فقط به‌عنوان «**Unverified/unapproved**» و غیرقابل اتکا به‌عنوان ورودی کاتالوگ ذکر شده‌اند.
- **یافته:** موردی از ثبت اشتباه پیشنهاد/یافته به‌عنوان Approved مشاهده نشد. **Pass**.

## 7) تطبیق وضعیت‌های Missing / Partially implemented / Unverified (دو سند مرجع)

| حوزه | v1.1 (محل/وضعیت) | Spec (محل/وضعیت) | تطبیق |
|---|---|---|---|
| ذخیره/نرمال‌سازی Permission در Role (رشته، trim+lower)؛ فقدان اعتبارسنجی کاتالوگ | §14.1: **Partially implemented** (storage+norm) / catalog validation **Missing** | §4.3/§11.2: normalization **Implemented/Verified** در Role/RoleId؛ اعتبارسنجی کاتالوگ **Missing** | **Pass** |
| ناوردای تکراری Domain و رویدادهای Permission | §14.1: **Implemented** (`PERMISSION_ALREADY_ASSIGNED`, events) | §6.4/§11.2: **Implemented** (حفظ invariant) | **Pass** |
| وضعیت چرخه عمر Role (Active/Inactive) | §14.1/§14.2: **Missing** (هدف Approved، مالکیت T-20) | §11.2: **Missing** (همان نکته) | **Pass** |
| ارجاعات نقش کاربر، چرخه عمر User (Active/Disabled)، نرمال‌سازی شناسه نقش در User | §14.1: refs+lifecycle **Implemented**؛ identifier normalization **Missing**؛ effective perms **Missing** | §11.2: **Partially implemented** (refs+lifecycle) / **Missing** (effective perms, identifier normalization) | **Pass** |
| محاسبه Effective Permissions | §14.1: **Missing** | §7/§11.2: **Missing** | **Pass** |
| پایداری Redis (Role JSON، کلیدهای IAM، ارجاع کدها؛ بدون کاتالوگ در Redis) | §14.1: **Partially implemented** (persistence present; catalog/validation absent) | §5.1: **Implemented** (persistence/refs) / catalog in Redis **Missing** (مطابق الزام) / resolution **Missing** | **Pass** (معنا هم‌خوان؛ بیان کمی متفاوت ولی ناسازگار نیست) |
| Use caseهای User-Role (Assign/Remove/Get) و ناهمخوانی `role.Name`/`RoleId.From` | §14.1/§14.3: **Partially implemented** + **verified semantic mismatch risk** | §11.2/§11.3: **Partially implemented** + **verified mismatch risk** | **Pass** |
| Use caseهای Role-Permission در Application و idempotency کاربردی انتساب Permission | §14.1: **Missing** | §6.4/§11.2: **Missing** | **Pass** |
| JIT provisioning | §14.1: **Partially implemented** (placeholder `"restricted-user"`؛ policy **Unverified/incomplete**) | §11.2: **Partially implemented** (placeholder؛ policy **Unverified/incomplete**) | **Pass** |
| کاتالوگ رسمی / Evaluator / enforcement جامع پرمیشن | §14.1: **Missing** (catalog/evaluator/comprehensive enforcement) | §10/§11.2: **Missing** | **Pass** |
| احراز هویت/مجوز فعلی API (role-based) و ناسازگاری `Operator` | §14.1: **Partially implemented & misaligned** (role-based auth)؛ `Operator` ارجاع‌شده ولی ثبت‌نشده | §10 جدول/§11.2: **Partially implemented & misaligned** (mismatch تأییدشده) | **Pass** |
| مسیرهای مستقیم هندلر / پس‌زمینه / داخلی (گراف فراخوانی کامل) | §14.1/§14.4: **Unverified** (not exhaustively traced) | §10/§11.2/§11.4: **Unverified** | **Pass** |
| نگاشت Permission→Operation و کدهای نهایی کاتالوگ | §11/§14.4: exact mapping/codes **Unverified**؛ بدون اختراع کد | §10/§11.4: **Unverified**؛ کدهای کاندید draft **Unverified/unapproved** | **Pass** |
| پوشش ممیزی امنیتی RBAC | (ذکر الزام در §12؛ جزئیات پوشش کنونی در §14 صریحاً باز نشده) | §11.2: **Unverified** | **Needs Review (جزئی، غیرمتناقض)** |

## 8) بررسی Documentation-only بودن T-18 و عدم انتقال مسئولیت پیاده‌سازی

- v1.1: §1 و §13 صراحتاً «architecture and documentation contract… does not authorize implementation changes as part of T-18» و Out of scope شامل پیاده‌سازی/انتخاب مکانیزم‌ها؛ §16 «T-18 remains **documentation/specification only**. Do not assign implementation of the Permission Catalog to T-18 and do not silently expand T-18 into implementation» و پیشنهاد آیتم جداگانه در صورت نیاز.
- Spec: §1/§2 بند 12، §15 و §16 تأکید بر «does not authorize code changes under T-18»، عدم انتساب پیاده‌سازی Permission Catalog به T-18، و امکان آیتم جداگانه برای enforcement/کاتالوگ.
- **نتیجه:** T-18 همچنان Documentation-only است و مسئولیت پیاده‌سازی Catalog یا Enforcement به آن منتقل نشده است. **Pass**.

## جدول نهایی (Pass / Fail / Needs Review)

| # | مورد بررسی | نتیجه | توضیح/محل |
|---|---|---|---|
| 1 | `git diff --stat` سه فایل | **Pass (با توضیح)** | خروجی خالی؛ هر سه فایل `??` (untracked/جدید) — طبیعی. |
| 2 | Diff کامل سه فایل | **Pass** | diff کامل (نسبت به `/dev/null`) برای هر سه ارائه شد. |
| 3 | `git diff --check` | **Pass** | بدون خطا (`RC=0`). |
| 4 | v1.0 فقط Supersession Notice افزوده، بدنه تاریخی بدون تغییر | **Pass** | پیشوند خطوط 1–9؛ بدنه از خط 10 (`# BitWrite Ocelot Control Plane`) تا انتهای سند تاریخی حفظ شده. |
| 5 | تطبیق تصمیمات مصوب (No Scope/Tenant، SoT سورس‌کد، Redis refs only، Retired runtime، Idempotent assignment، Complete enforcement) | **Pass** | در v1.1 و Spec هم‌راستا و صریح (§3.2,§4,§5,§6.2,§7.2,§10 در v1.1؛ §2,§4,§5,§6,§7,§9,§10 در Spec). |
| 6 | عدم ثبت اشتباه پیشنهاد/یافته به‌عنوان Approved (به‌ویژه RoleId به‌جای Role.Name) | **Pass** | RoleId صرفاً «Proposed direction/recommendation… requires confirmation in T-20/T-21» (v1.1 §14.3، Spec §11.3)؛ کدهای کاندید Unverified/unapproved. |
| 7 | تطبیق Missing / Partially implemented / Unverified بین دو سند مرجع | **Pass** (۱ مورد جزئی **Needs Review**) | هم‌خوانی در حوزه‌های کلیدی (Role/User/Redis/Use cases/JIT/کاتالوگ-ارزیاب-enforcement/نگاشت و مسیرها). نکته جزئی: پوشش ممیزی RBAC در Spec **Unverified** ولی در v1.1 صریحاً هم‌سطح ردیف نشده (غیرمتناقض). |
| 8 | T-18 همچنان Documentation-only و بدون انتقال پیاده‌سازی Catalog/Enforcement | **Pass** | v1.1 §1,§13,§16 و Spec §1,§2(12),§15,§16. |

**جمع‌بندی:** تغییرات سندی با تصمیمات مصوب و مرزبندی T-18 هم‌راستا هستند و مغایرت/تصمیم جدیدی شناسایی نشد. یک نکته جزئی برای شفافیت بیشتر (هم‌ترازی صریح وضعیت پوشش ممیزی RBAC بین دو سند) در حد **Needs Review** باقی می‌ماند و مانع تأیید ساختاری نیست، مگر به تشخیص شما.

در انتظار تأیید شما هستم. (هیچ تغییری اعمال نشده است.)
