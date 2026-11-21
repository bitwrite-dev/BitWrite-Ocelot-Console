# User Management UI — UI Issues Backlog

**Source:** `dashboard/` — React + Vite + TypeScript SPA (shadcn/ui, Tailwind v4)
**Status:** No user management UI exists yet. No Users page, no Roles page, no Login page, no wired auth.

---

## Gap Summary

| Area | Exists? | Tracking |
|------|---------|----------|
| Users page (`/users`) | No | — |
| Roles page (`/roles`) | No | — |
| Login / Sign-in page (`/login`) | No | — |
| Auth token provider (real, not anonymous) | No | #433 |
| Application shell (sidebar + navbar + routes) | Placeholder | #434 |
| Users/Roles nav items | No | — |
| Permission-based nav roles (vs legacy Role model) | No | — |

---

## Navigation Model (current — legacy, to be replaced)

`src/navigation.ts` uses Role-based access:
```ts
export type Role = 'Admin' | 'GatewayManager' | 'RouteManager' | 'SnapshotManager'
```

This mirrors the **old** API policies (`Admin`, `GatewayManager`, `RouteManager`, `SnapshotManager`), not the new permission catalog (`perm.users.read`, `perm.roles.manage`, etc.) defined in `src/BitWrite.OcelotConsole.Application/Permissions/BitWritePermissionCatalogDefinitions.cs`.

The comment at `router.tsx:92` says: *"Replace with real role resolution once #433 lands auth."*

---

## Proposed UI Issues (to create on GitHub)

### UI-01 — Login / Sign-in Page (`/login`)
**Priority:** P0 (blocks all authenticated UI)  
**Scope:**
- Login form (username/email + password)
- POST to API auth/token endpoint (per #433 decision: local `POST /api/v1/auth/login` or OIDC)
- Store access token, attach `Authorization: Bearer` header on API requests
- Redirect to `/` after successful login
- "Invalid credentials" error state
- Link to this page from navbar (user profile dropdown)

**Depends on:** #433 (auth strategy decision)

### UI-02 — Users List Page (`/users`)
**Priority:** P1  
**Scope:**
- `GET /api/v1/users` list with server-side paging (reuse `data-table.tsx`)
- Columns: id, username, display name, email, status (active/disabled), roles, created at
- Search/filter by username or email
- "Create user" button → navigates to `/users/new`
- Row actions: view (`/users/{id}`), edit (`/users/{id}/edit`), disable/enable
- Permission: `perm.users.read`

### UI-03 — User Create/Edit Page (`/users/new`, `/users/{id}/edit`)
**Priority:** P1  
**Scope:**
- Form: username, display name, email, password (create only), roles (multi-select from catalog), status toggle
- POST `CreateUserCommand` / PUT `UpdateUserCommand`
- Validation errors displayed per-field
- Cancel → back to list
- Permission: `perm.users.manage`

### UI-04 — User Detail Page (`/users/{id}`)
**Priority:** P2  
**Scope:**
- Read-only view of user: id, username, display name, email, status, assigned roles, sessions, created/updated at
- Sessions sub-list (`GET /api/v1/users/{userId}/sessions`) with revoke action
- Permission: `perm.users.read`

### UI-05 — Roles List Page (`/roles`)
**Priority:** P1  
**Scope:**
- `GET` roles list (API: role management endpoints per IAM-05)
- Columns: id, name, description, status (active/inactive), permission count
- "Create role" button → `/roles/new`
- Row actions: edit (`/roles/{id}/edit`), activate/deactivate, delete (refused if hard-delete not allowed)
- Permission: `perm.roles.read`

### UI-06 — Role Create/Edit Page (`/roles/new`, `/roles/{id}/edit`)
**Priority:** P1  
**Scope:**
- Form: name, description, status, permissions (multi-select from `perm.*` catalog)
- Assign/remove permissions from role (validates catalog: only Active assignable)
- Validation: rename rejected when role has stored permission references (IAM-01)
- Permission: `perm.roles.manage`

### UI-07 — Role Detail Page (`/roles/{id}`)
**Priority:** P2  
**Scope:**
- Read-only view: id, name, description, status, assigned permissions (with catalog status Active/Retired), users assigned to this role
- Permission: `perm.roles.read`

### UI-08 — Navigation Updates (sidebar + navbar)
**Priority:** P1  
**Scope:**
- Add `/users` and `/roles` to `NAV_SECTIONS` (System section or new "Identity & Access" section)
- Replace legacy `Role[]` with permission-based visibility (or keep dual during migration)
- Navbar: add user profile / logout button
- Logout: clear token, redirect to `/login`
- Permission-gated items: show/hide based on user's effective permissions

### UI-09 — Auth Token Provider (replace `anonymousTokenProvider`)
**Priority:** P0  
**Scope:**
- `src/api/auth.ts`: implement real `AccessTokenProvider` that reads token from memory/localStorage
- Attach `Authorization: Bearer <token>` on every request
- Handle 401 → `onUnauthorized` callback (redirect to `/login`)
- Token expiry handling
- Depends on #433 (strategy: local token endpoint vs OIDC)

### UI-10 — Forbidden / Unauthorized States
**Priority:** P2  
**Scope:**
- Reuse `components/page-state.tsx` (`ForbiddenState`, `ErrorState`)
- 403 → show "You don't have permission to view this page"
- 401 → redirect to `/login` with `returnUrl`
- Permission denials surfaced from API error body

---

## API Endpoints the UI Will Call

| UI Action | API Endpoint | Method | Permission |
|-----------|-------------|--------|------------|
| List users | `GET /api/v1/users` | GET | perm.users.read |
| Get user | `GET /api/v1/users/{id}` | GET | perm.users.read |
| Create user | `POST /api/v1/users` | POST | perm.users.manage |
| Update user | `PUT /api/v1/users/{id}` | PUT | perm.users.manage |
| Enable user | `POST /api/v1/users/{id}/enable` | POST | perm.users.manage |
| Disable user | `POST /api/v1/users/{id}/disable` | POST | perm.users.manage |
| List user sessions | `GET /api/v1/users/{userId}/sessions` | GET | perm.sessions.read |
| Revoke session | `POST /api/v1/sessions/{sessionId}/revoke` | POST | perm.sessions.manage |
| List roles | (role mgmt endpoints per IAM-05) | GET | perm.roles.read |
| Create role | (role mgmt endpoints per IAM-05) | POST | perm.roles.manage |
| Update role | (role mgmt endpoints per IAM-05) | PUT | perm.roles.manage |
| Activate/deactivate role | (role mgmt endpoints per IAM-05) | POST | perm.roles.manage |
| Login | `POST /api/v1/auth/login` (per #433) | POST | — |

---

## Files to Create / Modify

**New files:**
- `dashboard/src/pages/users/users-page.tsx` (placeholder → real)
- `dashboard/src/pages/users/user-create-page.tsx`
- `dashboard/src/pages/users/user-edit-page.tsx`
- `dashboard/src/pages/users/user-detail-page.tsx`
- `dashboard/src/pages/roles/roles-page.tsx`
- `dashboard/src/pages/roles/role-create-page.tsx`
- `dashboard/src/pages/roles/role-edit-page.tsx`
- `dashboard/src/pages/roles/role-detail-page.tsx`
- `dashboard/src/pages/login/login-page.tsx`
- `dashboard/src/features/users/queries.ts` (TanStack Query hooks)
- `dashboard/src/features/roles/queries.ts` (TanStack Query hooks)
- `dashboard/src/components/app-layout.tsx` (add logout/user profile to navbar)

**Modify:**
- `dashboard/src/navigation.ts` — add Users/Roles nav items, switch to permission-based roles
- `dashboard/src/router.tsx` — add `/users`, `/roles`, `/login` routes
- `dashboard/src/api/auth.ts` — real token provider
- `dashboard/src/App.tsx` — real layout (replace placeholder)

---

## Dependencies

- **#433** — API client and auth integration (token strategy, login endpoint)
- **#434** — Application shell (sidebar, navbar, routes)
- **IAM-09 / IAM-10a–d** — endpoint→permission mapping (approved in `documents/P-GATE-2-Endpoint-Permission-Mapping-v1.0.md`)
- **IAM-15** — User administration use cases (backend handlers exist)
- **P-GATE-2** — must be approved before permission-based UI enforcement

---

## Order of Implementation

1. **#433** auth strategy + real token provider (UI-09)
2. **#434** application shell + login page (UI-01, UI-08)
3. Users list + create/edit + detail (UI-02, UI-03, UI-04)
4. Roles list + create/edit + detail (UI-05, UI-06, UI-07)
5. Permission-based nav + forbidden states (UI-08, UI-10)
