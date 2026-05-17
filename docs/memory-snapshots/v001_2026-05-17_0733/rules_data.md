---
name: Data Access Rules
description: Hybrid multi-tenancy strategy, EF Core (writes) + Dapper (reads), PostgreSQL Turkish/timezone handling, 3 DBs
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Databases (4)
- **Catalog DB** — Small shared master DB. Holds `Tenants` registry (TenantId → connection string), global `Users` identity, system-level roles. Never per-tenant.
- **Main DB** — Business data. Shared by default for small tenants; per-tenant dedicated for large customers (connection string resolved from Catalog).
- **Log DB** — Serilog sink target. Shared across all tenants; rows carry `TenantId`.
- **Audit DB** — Append-only audit trail. Shared; rows carry `TenantId`.

## UrlCode (Global Pretty URL Identifier)
- **Every aggregate root** (Tenant, Company, Building, Unit, User, Invoice, etc.) carries a `UrlCode` field used in URLs instead of raw GUID.
- **9 characters**, generated from a Guid, encoded in **Base58** (excludes `0`, `O`, `I`, `l` for visual clarity).
- Unique **per database** (Catalog DB has its own pool; each Main DB has its own pool).
- Stored in a per-DB `url_codes(code PK, owner_type, owner_id)` lookup table, plus mirrored on the owning row for direct lookup.
- Generation: `Guid.NewGuid()` → Base58-encode bytes → take first 9 chars → INSERT into `url_codes`; on unique-violation, retry (probability of collision is ~10⁻¹⁵ per attempt).
- Routes use `UrlCode`: `/portal/units/{urlCode}`, `/management/companies/{urlCode}` — never raw IDs in user-facing URLs.
- Internal foreign keys still use the underlying `uuid` PK; UrlCode is **lookup-only**, not the join key.

## Multi-Tenancy — Hybrid Strategy
- Large customers → **dedicated Main DB** (per-tenant connection string).
- Small/medium customers → **shared Main DB** with `TenantId` discriminator column.
- Log DB and Audit DB always shared (TenantId-tagged), even for dedicated-DB tenants.
- A **Tenants registry** table in a *catalog* DB (or master schema) maps `TenantId → ConnectionString | NULL`. NULL = use shared.
- `ITenantContext` resolves tenant per request from JWT claim / subdomain / header.
- `ITenantConnectionFactory` returns the correct connection (EF DbContext or Npgsql connection) based on tenant.
- EF Core **Global Query Filter** auto-applies `TenantId == _tenant.CurrentTenantId` for shared-DB entities.
- Dapper queries do **not** get automatic filtering — a `TenantAwareQueryBuilder` helper enforces `WHERE tenant_id = @tenantId` and queries are reviewed for this in PRs.

## EF Core (Write Side — Commands)
- One `DbContext` per database boundary (`MainDbContext`, `LogDbContext`, `AuditDbContext`).
- Migrations managed via EF Core only; Dapper never alters schema.
- `SaveChangesInterceptor` populates audit fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `TenantId`) automatically.
- Soft delete: `IsDeleted` flag + global query filter; never hard delete except where domain demands.
- **Optimistic concurrency** via `xmin` (PostgreSQL) or `RowVersion` byte[] — required on all aggregate roots.
- Bulk operations: `EFCore.BulkExtensions` when row count > 1000.

## Dapper (Read Side — Queries)
- Connection from `IDbConnectionFactory` (tenant-aware).
- DTOs are query-specific (no leaking of EF entity types into read models).
- Complex listing/reporting queries live in `Infrastructure/Persistence/Queries/<Feature>/`.
- Parameterized queries only — no string concatenation. Npgsql type handlers for custom types.
- Multi-row results use `IAsyncEnumerable<T>` or paginated containers.

## PostgreSQL Specifics
- Extensions enabled in every Main/Log/Audit DB: `citext`, `unaccent`, `pg_trgm`, `uuid-ossp` (or `pgcrypto`).
- Text columns needing case-insensitive equality → `citext`.
- Searchable text (names, descriptions) → indexed with **GIN + pg_trgm**.
- Sorting → `COLLATE "tr-TR-x-icu"` for Turkish.
- All timestamps → `timestamptz` (UTC).
- IDs: `uuid` (v7 preferred for sortability) for aggregate roots; `bigint` for internal high-volume tables.

## Turkish Case-Insensitive Search
- Helper SQL: `unaccent(lower(column)) ILIKE unaccent(lower(@search))`.
- Custom PostgreSQL function `normalize_tr(text)` handles I/İ/ı/i edge cases (Turkish dotless I).
- A `.NET` helper `TurkishStringNormalizer.Normalize(string)` mirrors the SQL function for in-memory matching.

## Timezone Handling
- Store: always UTC (`timestamptz`).
- Forbidden: `DateTime.Now`, `DateTime.Today`. Use `DateTime.UtcNow`, `DateOnly`, `TimeOnly`, or `NodaTime`.
- Every API request carries `X-TimeZone` header (IANA name, e.g. `Europe/Istanbul`).
- Query layer converts user-supplied date/time filters from `X-TimeZone` → UTC range before hitting DB.
- Response layer converts UTC back to caller's timezone (or returns ISO 8601 with offset).
- Consider `NodaTime` for explicit timezone modeling in business logic.
