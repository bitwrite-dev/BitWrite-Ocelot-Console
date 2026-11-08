# IAM - User Management Task List

**Source:** BitWrite-User-Management-Architecture-v1.0.md  
**Status:** In Progress  
**Total Tasks:** 26  
**Completed:** 8  
**In Review:** 2  
**In Progress:** 1  
**Pending:** 15  
**Critical:** 7 | **Important:** 8 | **Deferred:** 11  

---

## Task Prioritization Matrix

| ID | Title | Type | Priority | Status | Dependencies | GitHub Issue | Est. Effort | PR |
|----|-------|------|----------|--------|--------------|--------------|-------------|-----|
| T-01 | Internal User Model - Core Domain Entity | Domain | Critical | Done | - | #625 | 3 days | #651 |
| T-02 | Local Credentials - Secure Password Hashing | Infrastructure | Critical | Done | T-01 | #627 | 4 days | #652 |
| T-03 | Authentication Throttling & Brute-force Protection | Infrastructure | Critical | Todo | T-02 | #628 | 3 days | - |
| T-04 | JIT Provisioning - Auto Create External User | Application | Critical | Todo | T-01, T-06 | #630 | 5 days | - |
| T-05 | User Lifecycle - Create, Update, Enable/Disable | Application | Critical | Todo | T-01 | #626 | 4 days | - |
| T-06 | External Identity Mapping - Issuer + Subject | Domain | Critical | Todo | T-01 | #629 | 3 days | - |
| T-07 | Security Requirements - Secure Bootstrapping & Token Mgmt | Infrastructure | Critical | Todo | T-02 | #650 | 5 days | - |
| T-08 | Redis Persistence Namespace bitwrite:iam:v1:* | Infrastructure | Important | Todo | - | #635 | 2 days | - |
| T-09 | Replaceable Persistence Abstraction | Domain/Application | Important | Todo | - | #636 | 3 days | - |
| T-10 | Atomic Operations for Concurrent Safety | Infrastructure | Important | Todo | T-01 | #637 | 3 days | - |
| T-11 | Session Revocation on User Disable | Application | Important | Todo | T-01, T-01 Session | #633 | 3 days | - |
| T-12 | Audit All User Management Operations | Application | Important | Todo | T-01 | #634 | 2 days | - |
| T-13 | Role Assignment to User - RBAC Integration | Application | Important | Todo | T-01 | #631 | 3 days | - |
| T-14 | Default-deny Authorization | Application | Important | Todo | T-01 | #632 | 2 days | - |
| T-15 | Session and Refresh Token Management | Infrastructure | Important | In Review | T-01 | #647 | 4 days | #664 |
| T-16 | MFA Architecture Readiness (TOTP-ready) | Architecture | Deferred | In Review | - | #638 | 2 days | #665 |
| T-17 | Bootstrap & Initial Administrator | Infrastructure | Deferred | In Progress | T-01 | #639 | 4 days | - |
| T-18 | Permission Catalog Definition | Documentation | Deferred | Todo | - | #640 | 2 days | - |
| T-19 | External Authentication Plugin Contract | Application/Infrastructure | Deferred | Todo | T-01 | #641 | 5 days | - |
| T-20 | Domain Model - User, LocalCredential, ExternalIdentity | Domain | Deferred | Todo | T-01 | #642 | 3 days | - |
| T-21 | Application Use Cases - User Administration | Application | Deferred | Todo | T-01 | #643 | 4 days | - |
| T-22 | Application Use Cases - Credential Management | Application | Deferred | Todo | T-02 | #644 | 3 days | - |
| T-23 | Application Use Cases - External Identity Management | Application | Deferred | Todo | T-06 | #645 | 4 days | - |
| T-24 | Application Use Cases - Account Security | Application | Deferred | Todo | T-01 | #646 | 3 days | - |
| T-25 | Management API Token Model - JWT Issuance | Application | Deferred | Todo | T-01 | #649 | 4 days | - |
| T-26 | Open Design Items Resolution | Documentation | Deferred | Todo | - | #648 | 2 days | - |

---

## Priority Groups

### 🔴 Critical (7 tasks) - Must be done first
| ID | Title | Type | Status | Issue | PR |
|----|-------|------|--------|-------|-----|
| T-01 | Internal User Model | Domain | Done | #625 | #651 |
| T-02 | Local Credentials - Password Hashing | Infrastructure | Done | #627 | #652 |
| T-03 | Authentication Throttling | Infrastructure | Todo | #628 | - |
| T-04 | JIT Provisioning | Application | Todo | #630 | - |
| T-05 | User Lifecycle | Application | Todo | #626 | - |
| T-06 | External Identity Mapping | Domain | Todo | #629 | - |
| T-07 | Security Requirements | Infrastructure | Todo | #650 | - |

### 🟠 Important (8 tasks) - Do after Critical
| ID | Title | Type | Status | Issue | PR |
|----|-------|------|--------|-------|-----|
| T-08 | Redis Persistence Namespace | Infrastructure | Todo | #635 | - |
| T-09 | Replaceable Persistence Abstraction | Domain/Application | Todo | #636 | - |
| T-10 | Atomic Operations | Infrastructure | Todo | #637 | - |
| T-11 | Session Revocation on Disable | Application | Todo | #633 | - |
| T-12 | Audit All Operations | Application | Todo | #634 | - |
| T-13 | Role Assignment | Application | Todo | #631 | - |
| T-14 | Default-deny Authorization | Application | Todo | #632 | - |
| T-15 | Session & Refresh Token Mgmt | Infrastructure | In Review | #647 | #664 |

### 🟡 Deferred (11 tasks) - Do after Important
| ID | Title | Type | Status | Issue | PR |
|----|-------|------|--------|-------|-----|
| T-16 | MFA Architecture Readiness | Architecture | In Review | #638 | #665 |
| T-17 | Bootstrap & Initial Admin | Infrastructure | In Progress | #639 | - |
| T-18 | Permission Catalog | Documentation | Todo | #640 | - |
| T-19 | External Auth Plugin Contract | Application/Infrastructure | Todo | #641 | - |
| T-20 | Domain Model - User, LocalCredential, ExternalIdentity | Domain | Todo | #642 | - |
| T-21 | User Administration Use Cases | Application | Todo | #643 | - |
| T-22 | Credential Management Use Cases | Application | Todo | #644 | - |
| T-23 | External Identity Management Use Cases | Application | Todo | #645 | - |
| T-24 | Account Security Use Cases | Application | Todo | #646 | - |
| T-25 | Management API Token Model - JWT | Application | Todo | #649 | - |
| T-26 | Open Design Items Resolution | Documentation | Todo | #648 | - |

---

## Dependency Graph

```
T-01 (User Model) ──→ T-02 (Password Hashing) ──→ T-03 (Auth Throttling)
      │                    │
      ├─────→ T-06 (External Identity Mapping) ──→ T-04 (JIT Provisioning)
      │                    │
      ├─────→ T-05 (User Lifecycle) ──→ T-11 (Session Revocation)
      │                    │
      ├─────→ T-13 (Role Assignment)
      ├─────→ T-14 (Default-deny Auth)
      │
      ├─────→ T-12 (Audit All Operations)
      │
      └─────→ T-17 (Bootstrap Admin)
```

---

## Execution Order

### Sprint 1 - Foundation (Week 1-2)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-01 | Internal User Model | Domain | Done |
| T-02 | Local Credentials - Password Hashing | Infrastructure | Done |
| T-03 | Authentication Throttling | Infrastructure | Todo |

### Sprint 2 - Core Features (Week 3-4)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-05 | User Lifecycle | Application | Todo |
| T-06 | External Identity Mapping | Domain | Todo |
| T-04 | JIT Provisioning | Application | Todo |

### Sprint 3 - Security & Infrastructure (Week 5-6)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-07 | Security Requirements | Infrastructure | Todo |
| T-08 | Redis Persistence Namespace | Infrastructure | Todo |
| T-09 | Replaceable Persistence Abstraction | Domain/Application | Todo |
| T-10 | Atomic Operations | Infrastructure | Todo |

### Sprint 4 - Authorization & Session (Week 7-8)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-13 | Role Assignment | Application | Todo |
| T-14 | Default-deny Authorization | Application | Todo |
| T-11 | Session Revocation | Application | Todo |
| T-15 | Session & Refresh Token Mgmt | Infrastructure | In Review |

### Sprint 5 - Audit & Observability (Week 9)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-12 | Audit All Operations | Application | Todo |

### Sprint 6 - Deferred Features (Week 10+)
| ID | Title | Type | Status |
|----|-------|------|--------|
| T-16 | MFA Architecture Readiness | Architecture | In Review |
| T-17 | Bootstrap & Initial Admin | Infrastructure | In Progress |
| T-18 | Permission Catalog | Documentation | Todo |
| T-19 | External Auth Plugin Contract | Application/Infrastructure | Todo |
| T-20 | Domain Model Details | Domain | Todo |
| T-21 | User Administration Use Cases | Application | Todo |
| T-22 | Credential Management Use Cases | Application | Todo |
| T-23 | External Identity Management Use Cases | Application | Todo |
| T-24 | Account Security Use Cases | Application | Todo |
| T-25 | Management API Token Model - JWT | Application | Todo |
| T-26 | Open Design Items Resolution | Documentation | Todo |

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Todo | Not started |
| In Progress | Currently working |
| Done | Completed and verified |

---

## Type Legend

| Type | Description |
|------|-------------|
| Domain | Business logic, entities, domain rules |
| Application | Use cases, application services, orchestration |
| Infrastructure | Persistence, external services, Redis, security |
| Architecture | Design decisions, cross-cutting concerns |
| Documentation | Specs, requirements, catalog definitions |

---

## Priority Legend

| Priority | Meaning | SLA |
|----------|---------|-----|
| Critical | Must have for MVP | Week 1-4 |
| Important | Should have for MVP | Week 5-8 |
| Deferred | Can wait for future release | Week 10+ |

---

## GitHub Issue Links

All tasks are linked to GitHub Issues #625 through #650 in the BitWrite-Ocelot-Control-Plane repository.

### Completed PRs
| ID | PR | Status |
|----|-----|--------|
| T-01 | #651 | Merged/Pending |
| T-02 | #652 | Merged/Pending |
| T-03 | #653 | Merged/Pending |
| T-04 | #654 | Merged/Pending |
| T-05 | #655 | Merged/Pending |
| T-06 | Included in T-04 | N/A |
| T-07 | #656 | Merged/Pending |
| T-08 | #657 | Merged/Pending |