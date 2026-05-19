---
name: CleanTenant Project Overview
description: Multi-tenant property/site management SaaS with 3 client apps (Management, Portal, Mobile) on .NET 10 + PostgreSQL
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Vision
**CleanTenant** is a multi-tenant SaaS platform for property/site management. End users are property owners, residents, and renters of buildings/blocks managed by management companies.

**Why:** SaaS for site/apartment management firms. Hierarchical org model + multi-role users in multiple sites simultaneously.
**How to apply:** Domain language is property management (Malik=Owner, Sakin=Resident, Kiracı=Renter). Decisions favor multi-tenant isolation + hierarchical permissions over single-tenant simplicity.

## Applications
- **ManagementApp** — Blazor Server + MudBlazor. Admin/operator panel. Used by system admins, tenant admins, company staff, building managers. Hierarchy: System → Tenant → Company → Building/Block.
- **PortalApp** — Blazor + MudBlazor. End-user portal for Malik/Sakin/Kiracı (Owner/Resident/Renter).
- **MobilApp** — MAUI Hybrid (Android & iOS). Login allows role selection between Management-side and Portal-side personas.

## Technology Baseline
- **.NET 10** (stable) — minimum supported, no preview features.
- **PostgreSQL** — three databases: **Main**, **Log**, **Audit**.
- **EF Core** for Create/Update/Delete; **Dapper** for Read.
- **MediatR + CQRS** + manual mapping extensions (no AutoMapper).
- **Docker** with up/down/reset scripts and per-environment compose files.

## Hard Project Rules (from project owner)
- Clean Architecture + Clean Code strictly enforced.
- One class/enum/interface **per file**, each placed in architecturally correct location.
- Every class/enum/interface has detailed `///` XML doc.
- Every property and variable carries an explanatory comment (relax only for trivially self-explanatory locals).
- Every phase and sub-phase gets versioned documentation; updates appended with version stamp.
- Each phase ends with **git commit + push**.
- Critical operations are logged. Audit tracks user identity, location, time, module, before/after diffs.
- Logging and audit must be **filterable via UI screens**.
- Multi-language: TR, EN, AR, RU, DE. Errors/validations/warnings centrally managed and localized.
- Turkish character (I/İ/ğ/Ğ/ü/Ü/Ş/ş/ı/Ö/ö/ç/Ç) case-insensitive search must work.
- Timezone-correct querying — UI input matched to DB storage format.
