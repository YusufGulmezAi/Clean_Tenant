---
name: Identity & Authorization Rules
description: Central identity, scope levels System/Tenant/Company/Unit, multi-context per browser tab, per-context tokens
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Central Identity
- **Single global user store** in **Catalog DB** (alongside the tenant registry).
- A user can hold roles across multiple scopes simultaneously.
- Auth provider: **ASP.NET Core Identity** base + custom hierarchical assignment store.

## Scope Hierarchy (Final)
Role assignments attach at exactly one of these scope levels:
```
System          (cross-tenant; e.g., Developer, SystemAdmin, Support)
  Tenant        (within one tenant)
    Company     (within one company; a user may hold multiple roles in the same company)
      Unit      (within one unit; e.g., Malik / Hissedar / Kiracı)
```
- **Building is NOT a role scope.** If Buildings exist as data entities to group Units, role bindings still attach at Unit (or Company) — never at Building.
- A user (e.g., "Yusuf") may simultaneously hold:
  - `System / Developer`
  - `Tenant A / TenantAdmin`
  - `Tenant B / Operator`
  - `Company X / Manager` and `Company X / Approver` (multiple roles in same Company)
  - `Company Y / Accountant`
  - `Unit 101 / Malik` (Hissedar / Owner) and `Unit 205 / Kiracı` (Renter)

## Data Model
- `Users` — global identity in Catalog DB.
- `Roles` — catalog: System / Tenant / Company / Unit-scoped role definitions.
- `Permissions` — fine-grained, mapped to roles.
- `RolePermissions` — role → permissions mapping.
- `UserRoleAssignments` columns:
  - `Id`, `UserId`, `RoleId`,
  - `ScopeLevel` (enum: System / Tenant / Company / Unit),
  - `TenantId` (nullable, required for Tenant/Company/Unit scopes),
  - `CompanyId` (nullable, required for Company/Unit scopes),
  - `UnitId` (nullable, required for Unit scope),
  - `AssignedAt`, `AssignedBy`, `ExpiresAt` (nullable).
- A user may have **many** rows here, including multiple rows for the **same scope** (multi-role in same Company/Unit).

## Multi-Context in Same Browser
- **Every browser tab can run an independent context.**
- ManagementApp: when the user **switches context** (Tenant / Company / Unit selection), backend issues a **fresh JWT bound to that context** — old token's claims do not migrate.
- Each tab gets its own token; tokens stored in **`sessionStorage`** (per-tab, not `localStorage` which is shared).
- Each context has its own refresh token (rotating, stored hashed server-side, scoped to the context).
- Closing the tab ends that context's session — intentional, improves security.

## JWT Claims per Context
- `userId`, `contextId` (unique per session+context),
- `scopeLevel` (System / Tenant / Company / Unit),
- `tenantId?`, `companyId?`, `unitId?` — populated up to the current scope,
- `roles[]` (only those active in this context),
- `permissions[]` (derived from roles in this context),
- `personaSide` (Management / Portal) — set at login or persona switch (mobile).

## Context-Switch Endpoint
- `POST /api/v1/auth/switch-context` with target scope payload.
- Server validates the user has at least one assignment matching the target.
- Server issues new access + refresh token pair with updated claims.
- Old context's refresh token is **not** revoked (other tabs may still be using it).

## 2FA Policy
- **System users → 2FA mandatory.** Cannot disable; enforced at login.
- **ManagementApp non-system users → 2FA optional** (user opt-in via settings).
- **PortalApp / MobilApp → 2FA optional** (user opt-in).
- TOTP (RFC 6238) baseline; recovery codes generated on enrollment.

## Mobile App Login Flow
- If user has only Management-side roles → enter Management directly.
- If user has only Portal-side roles (Malik/Sakin/Kiracı) → enter Portal directly.
- If both → prompt persona selection screen; selection drives token's `personaSide` claim and UI flow.
- Persona switch in-app re-issues token.

## Authorization Enforcement
- MediatR `AuthorizationBehavior` checks required permissions before handler runs.
- For tenant/company/unit scoped operations: the request's target IDs **must match** the JWT's scope claims (defense in depth — JWT claim AND data filter both checked).
- Failures return standard error code (`AUTH-xxx`).

## API Keys (Service-to-Service)
- Hashed at rest, scoped to a tenant + permission set, expirable, rotatable, revocable.
