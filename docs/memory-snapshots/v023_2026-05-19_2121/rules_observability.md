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

## Log DB Schema (v0.1.7 implementasyonu)
`logs` tablosu (Serilog PostgreSQL sink yazar):
- `Id` (BIGSERIAL), `Timestamp` (timestamptz), `Level` (smallint),
- `Message` (text), `MessageTemplate` (text), `Exception` (text),
- `Properties` (jsonb) — **30+ enricher property burada**: UserId, UserEmail, UserFullName, TenantId, TenantName, ScopeLevel, CompanyId, UnitId, PersonaSide, Roles, IsSystemSession, SupportSessionId, ImpersonatedByUserId, IpAddress, UserAgent, BrowserName, BrowserVersion, OperatingSystem, DeviceType, AcceptLanguage, Referer, TraceIdRequest, CorrelationId, RequestPath, RequestMethod, EnvironmentName, MachineName, ApplicationName, ApplicationVersion, ProcessId, ThreadId.
- `TraceId`, `CorrelationId` — top-level (sorgu hızı için ayrı kolon).
- İndeksler: Timestamp DESC, Level, TraceId.
- Partitioned by month for retention — Faz 1+'da (şu an tek tablo).

Custom enricher: `AuditMetadataEnricher` (Infrastructure.Logging) `IAuditMetadataAccessor.Capture()` çağırır.

## What Gets Logged (critical operations)
- All authentication events (login/logout/failed/lockout/MFA).
- All authorization denials.
- All Commands (handler entry/exit, success/failure).
- All external service calls (DB beyond p95 threshold, HTTP outbound, payment, SMS, email).
- All background job runs.

## Audit DB — Append-only Trail (v0.1.7 implementasyonu)
Separate from Log DB. Captures **business state changes**, not just events.
- One row per changed entity per operation.
- **`audit_entries` tablosu — 35+ denormalize alan** (kullanıcı/lokasyon/environment query'leri için join'siz):
  - `Id` (uuid v7), `Timestamp` (timestamptz UTC, microsecond hassasiyetli),
  - **Kullanıcı:** `UserId`, `UserEmail`, `UserFullName`, `TenantId`, `TenantName`, `ScopeLevel`, `CompanyId`, `UnitId`, `PersonaSide`, `RolesJson` (jsonb), `IsSystemSession`, `SupportSessionId`, `ImpersonatedByUserId`.
  - **Lokasyon:** `IpAddress`, `UserAgent`, `BrowserName`, `BrowserVersion`, `OperatingSystem`, `DeviceType` (Desktop/Mobile/Tablet/Bot), `AcceptLanguage`, `Referer`, `Country`, `City` (GeoIP — Faz 1+).
  - **Request bağlamı:** `TraceId`, `CorrelationId`, `RequestPath`, `RequestMethod`.
  - **Environment:** `EnvironmentName`, `MachineName`, `ApplicationName`, `ApplicationVersion`, `ProcessId`, `ThreadId`.
  - **Değişiklik:** `EntityType`, `EntityId`, `Action` (Create/Update/Delete), `ChangesJson` (delta — sadece değişen property'ler, PII redact'li).
- İndeksler: `(tenant_id, timestamp DESC)`, `(entity_type, entity_id)`, `support_session_id` (partial WHERE NOT NULL).
- **Trigger:** `FullAuditInterceptor` (`SaveChangesInterceptor`) Catalog ChangeTracker'ından entries toplar, Dapper ile audit DB'ye **batch INSERT** (yüksek hacim → EF Core overhead kabul edilemez).
- **Atomiklik:** Catalog SaveChanges başarılı olduktan SONRA audit yazımı — "data yazılmadan audit yazılmaz" garantisi. Eventually-consistent. Catalog fail → audit yazılmaz.
- **Soft-delete:** `IsDeleted` false→true güncellemesi audit'te **`Delete`** action olarak görünür (kullanıcı algısı: silindi).
- **WriteActionCount:** Aktif Support Session varsa `SupportSession.WriteActionCount` aynı transaction içinde otomatik artırılır.
- Dapper-side mutations: manual audit insert required (Faz 1+; şu an EF Core write only).

## Filtering & Listing UI
- ManagementApp ships log/audit viewer screens.
- Filters: date range, user, tenant, module, entity, action, level, correlation id, free text on `Message`/`ChangedFields`.
- Drill-down: click any log → show full row + linked audit entries by `CorrelationId`.
- Export: CSV/Excel for compliance.
- Access is permission-gated (e.g., `Audit.Read.Tenant`, `Audit.Read.All`).

## Sensitive Data (v0.1.7 implementasyonu)
- **`[Sensitive]` attribute** (Domain/Auditing) kendi entity'lerimizin PII property'lerine işaretleme.
- **Merkezi liste** — `PasswordHash`, `SecurityStamp`, `ConcurrencyStamp`, `TokenHash`, `RefreshTokenHash`, `AuthenticatorKey` (IdentityUser miras alınan property'ler attribute eklenemediği için).
- Audit `ChangesJson`'da bu alanların değeri `"[REDACTED]"`.
- Serilog log'larda Destructure.ByTransforming<T> Faz 1+'da eklenecek.
