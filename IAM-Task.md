# IAM - User Management Task List

**Source:** BitWrite-User-Management-Architecture-v1.0.md  
**Status:** Planned  
**Total Tasks:** 26  
**Critical:** 7 | **Important:** 8 | **Deferred:** 11  

---

## Task Prioritization Matrix

| ID | Title | Type | Priority | Status | Dependencies | GitHub Issue | Est. Effort |
|----|-------|------|----------|--------|--------------|--------------|-------------|
| T-01 | Internal User Model - Core Domain Entity | Domain | Critical | Todo | - | #625 | 3 days |
| T-02 | Local Credentials - Secure Password Hashing | Infrastructure | Critical | Todo | T-01 | #627 | 4 days |
| T-03 | Authentication Throttling & Brute-force Protection | Infrastructure | Critical | Todo | T-02 | #628 | 3 days |
| T-04 | JIT Provisioning - Auto Create External User | Application | Critical | Todo | T-01, T-06 | #630 | 5 days |
| T-05 | User Lifecycle - Create, Update, Enable/Disable | Application | Critical | Todo | T-01 | #626 | 4 days |
| T-06 | External Identity Mapping - Issuer + Subject | Domain | Critical | Todo | T-01 | #629 | 3 days |
| T-07 | Security Requirements - Secure Bootstrapping & Token Mgmt | Infrastructure | Critical | Todo | T-02 | #650 | 5 days |
| T-08 | Redis Persistence Namespace bitwrite:iam:v1:* | Infrastructure | Important | Todo | - | #635 | 2 days |
| T-09 | Replaceable Persistence Abstraction | Domain/Application | Important | Todo | - | #636 | 3 days |
| T-10 | Atomic Operations for Concurrent Safety | Infrastructure | Important | Todo | T-01 | #637 | 3 days |
| T-11 | Session Revocation on User Disable | Application | Important | Todo | T-01, T-01 Session | #633 | 3 days |
| T-12 | Audit All User Management Operations | Application | Important | Todo | T-01 | #634 | 2 days |
| T-13 | Role Assignment to User - RBAC Integration | Application | Important | Todo | T-01 | #631 | 3 days |
| T-14 | Default-deny Authorization | Application | Important | Todo | T-01 | #632 | 2 days |
| T-15 | Session and Refresh Token Management | Infrastructure | Important | Todo | T-01 | #647 | 4 days |
| T-16 | MFA Architecture Readiness (TOTP-ready) | Architecture | Deferred | Todo | - | #638 | 2 days |
| T-17 | Bootstrap & Initial Administrator | Infrastructure | Deferred | Todo | T-01 | #639 | 4 days |
| T-18 | Permission Catalog Definition | Documentation | Deferred | Todo | - | #640 | 2 days |
| T-19 | External Authentication Plugin Contract | Application/Infrastructure | Deferred | Todo | T-01 | #641 | 5 days |
| T-20 | Domain Model - User, LocalCredential, ExternalIdentity | Domain | Deferred | Todo | T-01 | #642 | 3 days |
| T-21 | Application Use Cases - User Administration | Application | Deferred | Todo | T-01 | #643 | 4 days |
| T-22 | Application Use Cases - Credential Management | Application | Deferred | Todo | T-02 | #644 | 3 days |
| T-23 | Application Use Cases - External Identity Management | Application | Deferred | Todo | T-06 | #645 | 4 days |
| T-24 | Application Use Cases - Account Security | Application | Deferred | Todo | T-01 | #646 | 3 days |
| T-25 | Management API Token Model - JWT Issuance | Application | Deferred | Todo | T-01 | #649 | 4 days |
| T-26 | Open Design Items Resolution | Documentation | Deferred | Todo | - | #648 | 2 days |

---

## Priority Groups

### 🔴 Critical (7 tasks) - Must be done first
| ID | Title | Type | Issue |
|----|-------|------|-------|
| T-01 | Internal User Model | Domain | #625 |
| T-02 | Local Credentials - Password Hashing | Infrastructure | #627 |
| T-03 | Authentication Throttling | Infrastructure | #628 |
| T-04 | JIT Provisioning | Application | #630 |
| T-05 | User Lifecycle | Application | #626 |
| T-06 | External Identity Mapping | Domain | #629 |
| T-07 | Security Requirements | Infrastructure | #650 |

### 🟠 Important (8 tasks) - Do after Critical
| ID | Title | Type | Issue |
|----|-------|------|-------|
| T-08 | Redis Persistence Namespace | Infrastructure | #635 |
| T-09 | Replaceable Persistence Abstraction | Domain/Application | #636 |
| T-10 | Atomic Operations | Infrastructure | #637 |
| T-11 | Session Revocation on Disable | Application | #633 |
| T-12 | Audit All Operations | Application | #634 |
| T-13 | Role Assignment | Application | #631 |
| T-14 | Default-deny Authorization | Application | #632 |
| T-15 | Session & Refresh Token Mgmt | Infrastructure | #647 |

### 🟡 Deferred (11 tasks) - Do after Important
| ID | Title | Type | Issue |
|----|-------|------|-------|
| T-16 | MFA Architecture Readiness | Architecture | #638 |
| T-17 | Bootstrap & Initial Admin | Infrastructure | #639 |
| T-18 | Permission Catalog | Documentation | #640 |
| T-19 | External Auth Plugin Contract | Application/Infrastructure | #641 |
| T-20 | Domain Model - User, LocalCredential, ExternalIdentity | Domain | #642 |
| T-21 | User Administration Use Cases | Application | #643 |
| T-22 | Credential Management Use Cases | Application | #644 |
| T-23 | External Identity Management Use Cases | Application | #645 |
| T-24 | Account Security Use Cases | Application | #646 |
| T-25 | Management API Token Model - JWT | Application | #649 |
| T-26 | Open Design Items Resolution | Documentation | #648 |

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
| ID | Title | Type |
|----|-------|------|
| T-01 | Internal User Model | Domain |
| T-02 | Local Credentials - Password Hashing | Infrastructure |
| T-03 | Authentication Throttling | Infrastructure |

### Sprint 2 - Core Features (Week 3-4)
| ID | Title | Type |
|----|-------|------|
| T-05 | User Lifecycle | Application |
| T-06 | External Identity Mapping | Domain |
| T-04 | JIT Provisioning | Application |

### Sprint 3 - Security & Infrastructure (Week 5-6)
| ID | Title | Type |
|----|-------|------|
| T-07 | Security Requirements | Infrastructure |
| T-08 | Redis Persistence Namespace | Infrastructure |
| T-09 | Replaceable Persistence Abstraction | Domain/Application |
| T-10 | Atomic Operations | Infrastructure |

### Sprint 4 - Authorization & Session (Week 7-8)
| ID | Title | Type |
|----|-------|------|
| T-13 | Role Assignment | Application |
| T-14 | Default-deny Authorization | Application |
| T-11 | Session Revocation | Application |
| T-15 | Session & Refresh Token Mgmt | Infrastructure |

### Sprint 5 - Audit & Observability (Week 9)
| ID | Title | Type |
|----|-------|------|
| T-12 | Audit All Operations | Application |

### Sprint 6 - Deferred Features (Week 10+)
| ID | Title | Type |
|----|-------|------|
| T-16 | MFA Architecture Readiness | Architecture |
| T-17 | Bootstrap & Initial Admin | Infrastructure |
| T-18 | Permission Catalog | Documentation |
| T-19 | External Auth Plugin Contract | Application/Infrastructure |
| T-20 | Domain Model Details | Domain |
| T-21 | User Administration Use Cases | Application |
| T-22 | Credential Management Use Cases | Application |
| T-23 | External Identity Management Use Cases | Application |
| T-24 | Account Security Use Cases | Application |
| T-25 | Management API Token Model - JWT | Application |
| T-26 | Open Design Items Resolution | Documentation |

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Todo | Not started |
| In Progress | Currently working |
| Done | Completed and tested |

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