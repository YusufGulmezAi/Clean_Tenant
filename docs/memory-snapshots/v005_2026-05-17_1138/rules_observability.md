---
name: Observability — Logging & Audit Rules
description: Serilog into Log DB, append-only Audit DB schema, structured logging, filterable UI screens
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Logging Stack
- **Serilog** as the only logging facade.
- Sinks: Console (dev), File (rolling, dev/prod), **PostgreSQL → Log DB** (always), optionally Seq for dev.
- **Structured logging mandatory** — no string interpolation in log messages. Use templated parameters: `Log.Information("User {UserId} created building {BuildingId}", userId, buildingId)`.
- **Correlation Id** propagated through every request (`X-Correlation-Id` header generated if missing, attached to every log entry and downstream call).
- OpenTelemetry hooks reserved for later (tracing, metrics) — design Serilog enricher pipeline to coexist.

## Log DB Schema (minimum columns)
- `Id` (uuid v7), `Timestamp` (timestamptz UTC), `Level`, `Message`, `MessageTemplate`,
- `Exception` (text), `Properties` (jsonb),
- `TenantId`, `UserId`, `UserName`, `UserEmail`,
- `IpAddress`, `UserAgent`, `Location` (city/country from IP), 
- `Module`, `Action`, `CorrelationId`, `TraceId`, `RequestPath`, `HttpMethod`, `StatusCode`, `DurationMs`.
- Partitioned by month for retention/cleanup.

## What Gets Logged (critical operations)
- All authentication events (login/logout/failed/lockout/MFA).
- All authorization denials.
- All Commands (handler entry/exit, success/failure).
- All external service calls (DB beyond p95 threshold, HTTP outbound, payment, SMS, email).
- All background job runs.

## Audit DB — Append-only Trail
Separate from Log DB. Captures **business state changes**, not just events.
- One row per changed entity per operation.
- Columns:
  - `Id` (uuid v7), `Timestamp` (UTC),
  - `TenantId`, `UserId`, `UserName`, `UserEmail`, `UserRole` (active scope),
  - `IpAddress`, `Location`, `UserAgent`,
  - `Module`, `EntityType`, `EntityId`,
  - `Action` (Create/Update/Delete/Restore/Approve/...),
  - `Before` (jsonb), `After` (jsonb),
  - `ChangedFields` (text[]),
  - `CorrelationId`.
- Trigger: EF Core `SaveChangesInterceptor` builds audit entries from `ChangeTracker` before SaveChanges, persists in same transaction so audit and business state are consistent.
- For Dapper-side mutations (rare — should not happen per the EF-write rule, but for migrations): manual audit insert required.

## Filtering & Listing UI
- ManagementApp ships log/audit viewer screens.
- Filters: date range, user, tenant, module, entity, action, level, correlation id, free text on `Message`/`ChangedFields`.
- Drill-down: click any log → show full row + linked audit entries by `CorrelationId`.
- Export: CSV/Excel for compliance.
- Access is permission-gated (e.g., `Audit.Read.Tenant`, `Audit.Read.All`).

## Sensitive Data
- PII redaction in logs (password, token, card numbers). Serilog `Destructure.ByTransforming<T>` for known sensitive types.
- Audit before/after diffs mask configured sensitive fields (e.g., `User.PasswordHash` never appears).
