---
name: Architecture Rules
description: Clean Architecture layering, CQRS with MediatR, manual mapping, file/comment conventions for CleanTenant
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Layering (Clean Architecture)
- `Domain` — Entities, value objects, domain events, domain exceptions, enums. No external dependencies.
- `Application` — Use cases (Commands/Queries via MediatR), DTOs, validators, business-rule services, abstractions (interfaces).
- `Infrastructure` — EF Core DbContexts (3 DBs), Dapper repositories, external services, identity, logging sinks, caching, message bus.
- `Presentation` — `ManagementApp`, `PortalApp`, `MobilApp`, `WebApi` (if separate). Composition root, controllers/minimal APIs, Blazor components.
- `BuildingBlocks` / `Shared` — Cross-cutting kernel (Result type, BaseEntity, common abstractions). Avoid bloating.

**Why:** Strict inward-only dependency flow keeps domain testable and frameworks swappable.
**How to apply:** New feature → start in Domain (model), Application (use case), then Infrastructure (impl), then Presentation (endpoint/UI). Never reach into Infrastructure from Application except via interfaces.

## CQRS with MediatR
- Every action is a `Command` or `Query` with its own Handler.
- **Commands** mutate via EF Core. **Queries** read via Dapper.
- Handler must stay thin — delegate to `BusinessRules` services for invariants/validations beyond input shape.
- MediatR Pipeline Behaviors (registered globally):
  - `LoggingBehavior`, `ValidationBehavior` (FluentValidation), `AuthorizationBehavior`,
  - `PerformanceBehavior`, `TransactionBehavior` (Commands only),
  - `CachingBehavior` (Queries only), `UnhandledExceptionBehavior`.

## Mapping
- **Manual mapping only** via static extension classes named `XyzMappingExtensions`.
- One mapping file per entity/DTO pairing, placed alongside the consuming feature.
- No AutoMapper / Mapster / reflection-based mappers.

## File Conventions
- **One type per file** (class, record, interface, enum, struct). Including `record struct`.
- File name = type name. Folder = architectural location.
- Folder layout under `Application` follows **feature folders**: `Features/<Aggregate>/<Action>/...` (e.g. `Features/Buildings/CreateBuilding/`).

## Comment Conventions
- Every class/interface/enum/record/struct has `///` XML doc explaining purpose, responsibilities, and key collaborators.
- Every public property carries an inline or XML comment describing meaning + units/format.
- Local variables: comment when non-obvious; skip when the name is self-explanatory.
- `TreatWarningsAsErrors = true`; XML doc warnings enabled to catch missing comments early.

## Code Quality Gates (project-wide)
- `Nullable enable` everywhere.
- `Directory.Build.props` + `Directory.Packages.props` (Central Package Management).
- Analyzers: Microsoft.CodeAnalysis.NetAnalyzers, SonarAnalyzer.CSharp, StyleCop.Analyzers.
- `DateTime.Now` is **forbidden** — only `DateTime.UtcNow` (analyzer rule).
- `.editorconfig` defines naming + style; CI fails on violations.
