# P-GATE-2: Endpoint → Permission Mapping (Proposed)

**Status:** Proposed — awaiting product approval (P-GATE-2).  
**Constraint:** No permission codes invented. Required permissions are drawn solely from `BitWritePermissionCatalogDefinitions.All` in `src/BitWrite.OcelotConsole.Application/Permissions/BitWritePermissionCatalogDefinitions.cs`.  
**Rule:** GET/read-only endpoints require `*.read`; mutating endpoints (POST/PUT/PATCH/DELETE) require `*.manage`. Anonymous exceptions are explicitly justified.  
**Owner:** IAM-09 / IAM-10a–d.

---

## Catalog codes in scope (all Active, Assignable except `perm.audit.read`)

| Code | Domain | Assignable | Purpose |
|------|--------|------------|---------|
| perm.users.read | Users | yes | View users |
| perm.users.manage | Users | yes | Create/update/enable/disable users |
| perm.roles.read | Roles | yes | View roles |
| perm.roles.manage | Roles | yes | Manage roles |
| perm.permissions.read | Permissions | yes | View catalog |
| perm.permissions.manage | Permissions | yes | Assign/remove perms |
| perm.sessions.read | Sessions | yes | View sessions |
| perm.sessions.manage | Sessions | yes | Revoke sessions |
| perm.tokens.manage | Tokens | yes | Revoke refresh tokens |
| perm.services.read | Services | yes | View services |
| perm.services.manage | Services | yes | Configure services |
| perm.routes.read | Routes | yes | View routes |
| perm.routes.manage | Routes | yes | Configure routes |
| perm.globalconfig.read | GlobalConfiguration | yes | View global config |
| perm.globalconfig.manage | GlobalConfiguration | yes | Configure global settings |
| perm.snapshots.read | Snapshots | yes | View snapshots |
| perm.snapshots.manage | Snapshots | yes | Create/publish snapshots |
| perm.publications.read | Publications | yes | View publications |
| perm.publications.manage | Publications | yes | Create/publish publications |
| perm.gateways.read | Gateways | yes | View gateways |
| perm.gateways.manage | Gateways | yes | Configure gateways |
| perm.runtime.read | Runtime | yes | View runtime state |
| perm.runtime.manage | Runtime | yes | Configure runtime |
| perm.plugins.read | Plugins | yes | View plugins |
| perm.plugins.manage | Plugins | yes | Install/configure plugins |
| perm.licenses.read | Licenses | yes | View licenses |
| perm.licenses.manage | Licenses | yes | Configure licenses |
| perm.settings.read | Settings | yes | View settings |
| perm.settings.manage | Settings | yes | Configure settings |
| perm.audit.read | Audit | yes | View audit log |
| perm.bootstrap.admin | Bootstrap | yes | Initial admin bootstrap (anonymous) |

---

## IAM-09 — IAM & Security-Sensitive APIs

| Controller | Endpoint | Method | Required Permission | Anonymous? | Justification |
|------------|----------|--------|---------------------|------------|---------------|
| UsersController | `POST /api/v1/users` | POST | perm.users.manage | No | Create user |
| UsersController | `GET /api/v1/users` | GET | perm.users.read | No | List users |
| UsersController | `GET /api/v1/users/{id}` | GET | perm.users.read | No | Get user |
| UsersController | `PUT /api/v1/users/{id}` | PUT | perm.users.manage | No | Update user |
| UsersController | `POST /api/v1/users/{id}/enable` | POST | perm.users.manage | No | Enable user |
| UsersController | `POST /api/v1/users/{id}/disable` | POST | perm.users.manage | No | Disable user (security-critical) |
| IdentityController | `POST /api/v1/sessions` | POST | perm.sessions.manage | No | Create session |
| IdentityController | `GET /api/v1/users/{userId}/sessions` | GET | perm.sessions.read | No | List user sessions |
| IdentityController | `POST /api/v1/sessions/{sessionId}/revoke` | POST | perm.sessions.manage | No | Revoke session |
| IdentityController | `POST /api/v1/tokens/refresh` | POST | perm.sessions.manage | **Yes** | Presents refresh token; minimal anon exception (IAM-09) |
| IdentityController | `POST /api/v1/tokens/rotate` | POST | perm.sessions.manage | **Yes** | Presents refresh token; minimal anon exception (IAM-09) |
| IdentityController | `POST /api/v1/tokens/revoke` | POST | perm.tokens.manage | No | Revoke refresh token |
| AuditController | `GET /api/v1/audit` | GET | perm.audit.read | No | List audit logs |
| AuditController | `GET /api/v1/audit/{id}` | GET | perm.audit.read | No | Get audit log |
| AuditController | `GET /api/v1/audit/stats` | GET | perm.audit.read | No | Audit stats |
| BootstrapController | `POST /api/v1/bootstrap/initial-admin` | POST | perm.bootstrap.admin | **Yes** | Callable before any admin exists (T-17); deployment-supplied secrets only |

---

## IAM-10a — Services & Routes

| Controller | Endpoint | Method | Required Permission |
|------------|----------|--------|---------------------|
| ServicesController | `GET /api/v1/services` | GET | perm.services.read |
| ServicesController | `GET /api/v1/services/{id}` | GET | perm.services.read |
| ServicesController | `POST /api/v1/services` | POST | perm.services.manage |
| ServicesController | `PUT /api/v1/services/{id}` | PUT | perm.services.manage |
| ServicesController | `DELETE /api/v1/services/{id}` | DELETE | perm.services.manage |
| ServicesController | `GET /api/v1/services/{id}/routes` | GET | perm.services.read |
| RoutesController | `GET /api/v1/routes` | GET | perm.routes.read |
| RoutesController | `GET /api/v1/routes/{id}` | GET | perm.routes.read |
| RoutesController | `POST /api/v1/routes/validate` | POST | perm.routes.read | draft validation (no mutation) |
| RoutesController | `POST /api/v1/routes` | POST | perm.routes.manage |
| RoutesController | `PUT /api/v1/routes/{id}` | PUT | perm.routes.manage |
| RoutesController | `DELETE /api/v1/routes/{id}` | DELETE | perm.routes.manage |
| RoutesController | `PATCH /api/v1/routes/{id}/enable` | PATCH | perm.routes.manage |
| RoutesController | `PATCH /api/v1/routes/{id}/disable` | PATCH | perm.routes.manage |
| RoutesController | `POST /api/v1/routes/{id}/validate` | POST | perm.routes.read | validation (no mutation) |
| RoutesController | `GET /api/v1/routes/{id}/preview` | GET | perm.routes.read |
| RoutesController | `GET /api/v1/routes/{id}/effective` | GET | perm.routes.read |
| RoutesController | `GET /api/v1/routes/{id}/history` | GET | perm.routes.read |

---

## IAM-10b — Global Configuration, Snapshots & Publications

| Controller | Endpoint | Method | Required Permission |
|------------|----------|--------|---------------------|
| GlobalConfigurationController | `GET /api/v1/global-configuration` | GET | perm.globalconfig.read |
| GlobalConfigurationController | `PUT /api/v1/global-configuration` | PUT | perm.globalconfig.manage |
| SnapshotsController | `GET /api/v1/snapshots` | GET | perm.snapshots.read |
| SnapshotsController | `POST /api/v1/snapshots/preview` | POST | perm.snapshots.read | no storage |
| SnapshotsController | `POST /api/v1/snapshots/validate` | POST | perm.snapshots.read | no storage |
| SnapshotsController | `POST /api/v1/snapshots` | POST | perm.snapshots.manage |
| SnapshotsController | `GET /api/v1/snapshots/{version}` | GET | perm.snapshots.read |
| SnapshotsController | `GET /api/v1/snapshots/{version}/compare` | GET | perm.snapshots.read |
| SnapshotsController | `POST /api/v1/snapshots/{version}/clone` | POST | perm.snapshots.manage |
| SnapshotsController | `GET /api/v1/snapshots/{version}/export` | GET | perm.snapshots.read |
| SnapshotsController | `POST /api/v1/snapshots/{version}/publish` | POST | perm.snapshots.manage |
| SnapshotsController | `POST /api/v1/snapshots/{version}/rollback` | POST | perm.snapshots.manage |
| SnapshotsController | `GET /api/v1/snapshots/{version}/deployment` | GET | perm.snapshots.read |
| PublicationsController | `GET /api/v1/publications` | GET | perm.publications.read |
| PublicationsController | `GET /api/v1/publications/current` | GET | perm.publications.read |
| PublicationsController | `GET /api/v1/publications/history` | GET | perm.publications.read |
| PublicationsController | `GET /api/v1/publications/{id}` | GET | perm.publications.read |
| PublicationsController | `GET /api/v1/publications/{id}/progress` | GET | perm.publications.read |
| PublicationsController | `GET /api/v1/publications/diff` | GET | perm.publications.read |
| PublicationsController | `POST /api/v1/publications/create` | POST | perm.publications.manage |
| PublicationsController | `POST /api/v1/publications/{id}/execute` | POST | perm.publications.manage |
| PublicationsController | `POST /api/v1/publications/{id}/cancel` | POST | perm.publications.manage |

---

## IAM-10c — Gateways & Runtime

| Controller | Endpoint | Method | Required Permission |
|------------|----------|--------|---------------------|
| GatewaysController | `GET /api/v1/gateways` | GET | perm.gateways.read |
| GatewaysController | `GET /api/v1/gateways/{id}` | GET | perm.gateways.read |
| GatewaysController | `GET /api/v1/gateways/{id}/deletion-eligibility` | GET | perm.gateways.read |
| GatewaysController | `POST /api/v1/gateways` | POST | perm.gateways.manage |
| GatewaysController | `PUT /api/v1/gateways/{id}` | PUT | perm.gateways.manage |
| GatewaysController | `DELETE /api/v1/gateways/{id}` | DELETE | perm.gateways.manage |
| GatewaysController | `PATCH /api/v1/gateways/{id}/status` | PATCH | perm.gateways.manage |
| RuntimeController | `GET /api/v1/runtime/delivery-metrics` | GET | perm.runtime.read |
| RuntimeController | `GET /api/v1/runtime/status` | GET | perm.runtime.read |
| RuntimeController | `GET /api/v1/runtime/gateways` | GET | perm.runtime.read |
| RuntimeController | `GET /api/v1/runtime/gateways/{id}` | GET | perm.runtime.read |
| RuntimeController | `POST /api/v1/runtime/reconcile` | POST | perm.runtime.manage |

---

## IAM-10d — Plugins, Licenses, Settings & Remaining

| Controller | Endpoint | Method | Required Permission |
|------------|----------|--------|---------------------|
| PluginsController | `GET /api/v1/plugins` | GET | perm.plugins.read |
| PluginsController | `GET /api/v1/plugins/{id}` | GET | perm.plugins.read |
| PluginsController | `POST /api/v1/plugins` | POST | perm.plugins.manage |
| PluginsController | `PATCH /api/v1/plugins/{id}/enable` | PATCH | perm.plugins.manage |
| PluginsController | `PATCH /api/v1/plugins/{id}/disable` | PATCH | perm.plugins.manage |
| PluginsController | `DELETE /api/v1/plugins/{id}` | DELETE | perm.plugins.manage |
| PluginsController | `PUT /api/v1/plugins/{id}/upgrade` | PUT | perm.plugins.manage |
| LicensesController | `GET /api/v1/licenses` | GET | perm.licenses.read |
| LicensesController | `GET /api/v1/licenses/{id}` | GET | perm.licenses.read |
| LicensesController | `POST /api/v1/licenses` | POST | perm.licenses.manage |
| LicensesController | `POST /api/v1/licenses/activate` | POST | perm.licenses.manage |
| LicensesController | `PUT /api/v1/licenses/{id}` | PUT | perm.licenses.manage |
| LicensesController | `PUT /api/v1/licenses/{id}/renew` | PUT | perm.licenses.manage |
| LicensesController | `DELETE /api/v1/licenses/{id}/revoke` | DELETE | perm.licenses.manage |
| SettingsController | `GET /api/v1/settings` | GET | perm.settings.read |
| SettingsController | `POST /api/v1/settings/first-run` | POST | perm.settings.manage | **Yes** (AllowAnonymous) — first-run setup before any token |
| SettingsController | `PUT /api/v1/settings` | PUT | perm.settings.manage |

---

## Endpoints already protected vs. unprotected

Already `[Authorize]` on class/action: `UsersController`, `IdentityController` (except refresh/rotate), `AuditController`, `SettingsController.Put`, `BootstrapController` (AllowAnonymous exception).

**Unprotected (no `[Authorize]`):** `ServicesController`, `RoutesController`, `GlobalConfigurationController`, `SnapshotsController`, `PublicationsController`, `GatewaysController`, `RuntimeController`, `PluginsController`, `LicensesController`, `SettingsController.Get`.

These require the permission policy to be applied (via `[Authorize(Policy = ...)]` or handler-level enforcement) once this mapping is approved.

---

## Approval requested

Approve the mapping above (permission codes only from the catalog; no new codes). Once approved, implementation proceeds as:
1. Apply `[Authorize(Policy = "perm:{code}")]` to each controller/action per the table (respecting anonymous exceptions).
2. Add `IAM-10a–d` integration tests (authorized / unauthorized / forbidden per endpoint).
3. Mark P-GATE-2 resolved; IAM-09/10a–d completion unblocked.
