# BitWrite Ocelot Control — User Management Architecture

**Document Version:** 1.0  
**Status:** Draft for Review  
**Product:** BitWrite Ocelot Control  
**Bounded Context:** Identity & Access Management (IAM)  
**Scope:** User Management  
**Target Platform:** .NET 8  
**Initial Persistence Provider:** Redis

---

## 1. Purpose

This document defines the architecture and functional boundaries of User Management in BitWrite Ocelot Control.

User Management provides the capabilities required to create, maintain, identify, enable, disable, and administer users who access the BitWrite Management Plane.

The feature supports both:

- Locally managed users authenticated by BitWrite.
- Users authenticated by an external Identity Provider and represented by an internal BitWrite User.

This document focuses on the **User Management** capabilities and their interactions with authentication, external identity mapping, Roles, Permissions, sessions, and audit. It does not replace the broader IAM architecture specification.

## 2. Architectural Principles

1. Every person who accesses the BitWrite Management Plane must have an internal BitWrite User.
2. BitWrite is the authoritative owner of its internal User records.
3. Authentication method does not change the ownership of the internal User.
4. External Identity Providers authenticate identities; they do not create or manage BitWrite Users, Roles, or Permissions directly.
5. Role and Permission management remains entirely inside BitWrite.
6. A User's access must follow the principle of least privilege.
7. User lifecycle operations must be auditable.
8. User Management must not depend on a specific authentication provider or persistence technology.
9. Domain and Application layers must remain independent of Redis and other Infrastructure implementation details.

## 3. Scope

### 3.1 Included

The initial User Management capability includes:

- Creating local users.
- Updating user profile and account information.
- Enabling and disabling users.
- Viewing and searching users.
- Managing local credentials for local users.
- Associating external identities with internal users.
- Automatically provisioning users after successful external authentication (JIT).
- Assigning a restricted default Role to automatically provisioned external users.
- Viewing a user's assigned Roles.
- Revoking a user's active sessions when required by account or security operations.
- Recording user-management activities through the existing Audit Context.

### 3.2 Excluded

The following capabilities are outside this document's direct scope:

- Definition and management of the complete Role and Permission catalog.
- Detailed permission evaluation and authorization policies.
- External Identity Provider implementation details.
- Gateway request authentication and downstream API authorization.
- Environment- and Gateway-level authorization scopes.
- TOTP implementation and advanced MFA workflows.
- Centralized enterprise directory synchronization or bulk provisioning, unless introduced by a future requirement.

These capabilities may be specified in separate IAM, Authorization, or Plugin architecture documents.

## 4. User Identity Model

### 4.1 Internal User

BitWrite maintains an internal User record for every user who accesses its Management Plane.

The internal User ID is the stable identifier used by BitWrite for:

- User management.
- Role assignments.
- Audit attribution.
- Session ownership.
- Management API token subject (`sub`).
- Linking one or more external identities.

The internal User ID must not be replaced by an external provider's subject identifier.

### 4.2 User Categories

The initial model supports two authentication categories:

**Local User**

A user whose credentials are managed and verified by BitWrite.

**External User**

A user whose identity is authenticated by an external Identity Provider, such as Keycloak, and mapped to an internal BitWrite User.

These categories describe the authentication source. They do not create separate authorization systems. Both categories use BitWrite's internal Roles and Permissions.

### 4.3 Suggested User Attributes

The User model may contain the following attributes:

| Attribute | Description |
|---|---|
| `Id` | Stable internal User identifier |
| `Username` | BitWrite username, subject to uniqueness rules |
| `DisplayName` | User-facing display name |
| `Email` | Email address, if available |
| `Status` | Account lifecycle status |
| `UserType` | Local or External |
| `CreatedAt` | Creation timestamp |
| `CreatedBy` | Actor responsible for creation, when applicable |
| `UpdatedAt` | Last update timestamp |
| `UpdatedBy` | Actor responsible for the last update, when applicable |
| `LastAuthenticatedAt` | Last successful authentication timestamp, when available |

The exact required/optional status of profile fields and username normalization rules must be finalized during detailed design.

External attributes such as email and display name may be populated from a trusted authentication result, but must not be treated as the stable identity key.

## 5. User Lifecycle

### 5.1 Initial Lifecycle States

The initial User lifecycle should support at least:

- **Active** — the User may authenticate, subject to authentication and authorization rules.
- **Disabled** — the User is not permitted to establish a new authenticated Management session.

Additional states such as Pending, Locked, or Deleted should only be introduced if required by a concrete workflow. They must not be added merely to anticipate hypothetical scenarios.

A disabled user must not gain access through an existing refresh token. Disabling a user should trigger revocation of that user's active sessions, or an equivalent enforcement mechanism that prevents continued use of those sessions.

### 5.2 Create Local User

An authorized administrator creates a local User by providing the required account information.

The operation should:

1. Validate the supplied user data.
2. Enforce username uniqueness according to the defined normalization policy.
3. Create the internal User.
4. Create the local credential using a secure password-hashing mechanism, if credentials are supplied as part of the workflow.
5. Apply Roles only through the BitWrite authorization capabilities.
6. Record the operation in the Audit Context.

The system must not store plaintext passwords.

The exact invitation, initial-password, and password-reset workflows require a detailed security design.

### 5.3 Update User

An authorized administrator may update permitted profile fields.

The operation must:

- Validate the new values.
- Enforce applicable uniqueness constraints.
- Prevent unauthorized changes to protected fields.
- Record the change in audit history.

Changing profile information must not implicitly change the user's Roles or Permissions.

### 5.4 Disable User

An authorized administrator may disable a User.

Disabling a User must:

- Prevent new local authentication.
- Prevent new Management sessions for that User.
- Prevent refresh-token use from restoring access.
- Revoke existing sessions or ensure they are rejected by the Management API.
- Preserve the User record and its audit history.

### 5.5 Enable User

An authorized administrator may re-enable a disabled User.

Re-enabling a User must not silently restore previously revoked sessions. The User must authenticate again.

### 5.6 Delete User

Physical deletion is not part of the initial lifecycle.

The initial approach should preserve User records and audit references. If a deletion or anonymization requirement is introduced, it must define its impact on audit integrity, external identity links, Role assignments, and historical records.

## 6. Local User Credentials

For Local Users, BitWrite manages the credentials required for authentication.

Credential responsibilities include:

- Secure password hashing and verification.
- Password change and reset workflows.
- Password policy enforcement.
- Authentication failure controls.
- Credential replacement and invalidation.

Passwords must never be stored in plaintext or recoverable form.

Credential data should be separated logically from general User profile data. The detailed aggregate and persistence boundaries will be defined in the IAM implementation design.

## 7. External Users and JIT Provisioning

### 7.1 Ownership

An external Identity Provider authenticates the user. BitWrite owns the internal User record and all BitWrite authorization data associated with that User.

For example, Keycloak confirms the identity of Reza. BitWrite then resolves or creates Reza's internal User record and determines his BitWrite access.

### 7.2 Selected JIT Policy

**Decision: Automatic JIT provisioning with a restricted default Role.**

When a user successfully authenticates through a configured external Identity Provider for the first time:

1. The external authentication plugin validates the authentication result.
2. The plugin returns a normalized identity to the IAM Application layer.
3. BitWrite looks up the external identity association.
4. If an association exists, BitWrite resolves the existing internal User.
5. If no association exists, BitWrite creates an internal User.
6. BitWrite creates the external identity association.
7. BitWrite assigns the configured restricted default Role.
8. BitWrite issues its own Management API access token if the User is eligible to access the Management Plane.

The newly created User is not granted administrative or unrestricted access.

The default Role must be configurable and must follow the principle of least privilege. If the required default Role is missing, disabled, or invalid, provisioning must fail safely rather than creating a user with unintended permissions.

### 7.3 External Identity Key

An external identity must be identified using the combination of:

- `Issuer`
- `Subject`

This combination must be unique.

Email address, display name, and username must not be used as the sole external identity key because they may change or may not be unique across providers.

Suggested `ExternalIdentity` attributes:

| Attribute | Description |
|---|---|
| `Id` | Internal association identifier |
| `UserId` | Internal BitWrite User ID |
| `Issuer` | Identity Provider issuer |
| `Subject` | Stable subject identifier from the provider |
| `ProviderType` | Provider category or plugin identifier |
| `CreatedAt` | Association creation timestamp |
| `LastAuthenticatedAt` | Last successful authentication timestamp |

### 7.4 Identity Linking

Linking an additional external identity to an existing internal User must be an explicit, protected operation.

BitWrite must not automatically merge two internal Users merely because they share an email address.

The linking workflow must validate the external identity and prevent an identity from being associated with more than one internal User.

### 7.5 Repeated Authentication

On subsequent authentication:

- BitWrite resolves the same internal User using the `Issuer` + `Subject` association.
- A duplicate internal User must not be created.
- User status must be checked before issuing a Management API token.
- The authentication timestamp may be updated.
- BitWrite authorization remains based on internal Role assignments.

## 8. Relationship with Roles and Permissions

User Management does not own the Role and Permission catalog. However, it must integrate with the BitWrite Authorization capabilities.

Key rules:

- Users receive Roles through BitWrite-managed assignments.
- External providers do not assign BitWrite Roles.
- External provider groups or role claims must not automatically become BitWrite permissions.
- JIT-created external users receive only the configured restricted default Role.
- Additional access is granted through explicit BitWrite administration.
- Disabling a User must prevent the User from continuing to exercise assigned permissions.

The initial authorization scopes are Global and Service. Environment and Gateway scopes are deferred and may be introduced later if real customer scenarios require them.

## 9. Session Interaction

Sessions are owned and managed by BitWrite, regardless of the authentication method.

User Management operations that affect account access must integrate with session management.

Examples:

- Disabling a User revokes or invalidates active sessions.
- Enabling a User does not restore previously revoked sessions.
- A security-sensitive credential change may require session revocation according to the security policy.
- Removing an external identity must not silently leave an unmanaged authentication path.

Session creation, refresh-token rotation, and token revocation mechanics are specified in the IAM architecture document.

## 10. Audit Requirements

User Management must reuse the existing BitWrite Audit Context.

It must not introduce a separate audit subsystem.

At minimum, the following actions should be auditable:

- User created.
- User profile updated.
- User enabled.
- User disabled.
- Local credential changed or reset.
- External identity associated.
- External identity removed.
- JIT User provisioned.
- Default Role assigned during JIT provisioning.
- User sessions revoked.
- User-related security operation failed.

Audit records should include the actor, action, target User, timestamp, result, and correlation information where available.

Passwords, tokens, credential hashes, and other secrets must never be written to audit records.

## 11. Authorization and Administrative Access

All User Management operations must be protected by BitWrite's internal authorization model.

Examples of permission concepts include:

- `users.read`
- `users.create`
- `users.update`
- `users.enable`
- `users.disable`
- `users.manage-identities`
- `users.revoke-sessions`

These are illustrative permission identifiers. The final permission catalog and operation-to-permission mapping belong to the Authorization design.

The system must apply default-deny behavior when the caller does not have the required permission.

A user must not be able to grant themselves additional Roles or Permissions through ordinary profile-update operations.

## 12. Persistence and Infrastructure Boundaries

### 12.1 Initial Persistence

Redis is the selected initial persistence provider. The existing Redis infrastructure will be reused.

IAM data must use a dedicated, versioned namespace, for example:

`bitwrite:iam:v1:*`

This namespace is a logical isolation boundary. It does not imply a separate Redis deployment.

### 12.2 Replaceable Persistence

Redis-specific implementation details must remain in the Infrastructure layer.

Domain and Application must not depend on:

- StackExchange.Redis.
- Redis key naming or data structures.
- Redis serialization formats.
- Redis transactions or Lua scripts.

Application-facing contracts must express User Management operations and required consistency behavior.

The architecture must permit a future persistence provider, such as PostgreSQL, to be implemented through a new Infrastructure implementation and controlled data migration without changing Domain or Application business logic.

Provider replacement may still require data migration, deployment coordination, and operational validation.

### 12.3 Consistency Requirements

The implementation must address atomicity and concurrency for operations such as:

- Creating a User with a unique username.
- Creating an external User and its identity association.
- Assigning the JIT default Role.
- Linking an external identity.
- Disabling a User and invalidating sessions.

The exact Redis data model and atomic operation strategy belong to the Infrastructure design.

## 13. Suggested Domain Concepts

The initial conceptual model includes:

- `User`
- `LocalCredential`
- `ExternalIdentity`
- `UserRoleAssignment` (owned by or coordinated with the Authorization capability)
- `Session` (owned by session management)

The final aggregate boundaries must be established during detailed domain design. This document does not mandate that every concept be implemented as a separate aggregate or repository.

## 14. Suggested Application Use Cases

### User Administration

- Create Local User.
- Get User.
- Search Users.
- Update User.
- Enable User.
- Disable User.

### Credential Management

- Change Password.
- Reset Password.
- Validate Local Credentials.

### External Identity Management

- Resolve External Identity.
- Provision External User.
- Link External Identity.
- Unlink External Identity.

### Account Security

- Revoke User Sessions.
- Retrieve User Authentication Information.
- Record Successful Authentication.

Use-case names are conceptual and may be adjusted to match the project's naming conventions and command/query architecture.

## 15. Security Requirements

The implementation must address:

- Secure password hashing.
- Authentication throttling and brute-force protection.
- Username uniqueness and normalization.
- External issuer and subject validation.
- Safe JIT provisioning under concurrent requests.
- Least-privilege default Role assignment.
- Explicit and protected identity linking.
- Session invalidation when a User is disabled.
- Secure credential reset.
- Audit logging without secrets.
- Secure Redis deployment, persistence, and backup.
- Default-deny authorization.

TOTP MFA is anticipated by the broader IAM architecture but is not implemented as part of this initial User Management scope. Its implementation is deferred to a future release unless a mandatory v1 security requirement is identified.

## 16. Version 1 Decisions

| Area | Decision |
|---|---|
| Internal User record | Required for every Management Plane user |
| Local user management | Built-in |
| Local credentials | Managed by BitWrite |
| External authentication | Through licensed plugins |
| Authorization ownership | BitWrite |
| External identity key | `Issuer` + `Subject` |
| First-time external user | Automatic JIT provisioning |
| JIT default access | Restricted default Role |
| User lifecycle | Active / Disabled |
| Physical deletion | Not included initially |
| Initial authorization scopes | Global + Service |
| Environment scope | Deferred |
| Gateway scope | Deferred |
| Session ownership | BitWrite |
| Audit | Existing Audit Context |
| Initial persistence | Redis |
| Future persistence replacement | Supported through Infrastructure implementation and migration |
| TOTP MFA | Deferred; architecture readiness handled in IAM |

## 17. Open Design Items

The following details must be finalized before or during implementation:

1. Required and optional User profile fields.
2. Username normalization and uniqueness rules.
3. User ID format.
4. Local account invitation and initial-password workflow.
5. Password policy and reset workflow.
6. Whether account lockout is required in v1 and its exact behavior.
7. Exact restricted default Role and how it is configured.
8. Behavior when the configured default Role is unavailable.
9. External identity unlinking rules.
10. Whether multiple external identities may be linked to one internal User in v1.
11. Exact permission identifiers for User Management operations.
12. Redis serialization, atomicity, backup, and recovery design.
13. Session invalidation strategy for disabled users.

These open items do not change the architectural decisions already stated in this document.

---

**Document Status: Draft for Review**

This document defines the User Management architecture and initial v1 boundaries for BitWrite Ocelot Control. Implementation should begin after review and approval.
