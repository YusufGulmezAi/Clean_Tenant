---
name: API & Response Rules
description: Global response envelope, MediatR pipeline behaviors, error code catalog, validation with FluentValidation 11.x
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Global Response Envelope
Every API endpoint returns a single envelope shape:

```csharp
public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    ApiError? Error,
    ApiMeta Meta);

public sealed record ApiError(
    string Code,        // e.g. "USR-001"
    string Message,     // localized
    IReadOnlyList<ApiFieldError>? Fields,
    string? TraceId);

public sealed record ApiMeta(
    string TraceId,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    Pagination? Pagination);
```
- Errors follow **RFC 7807 ProblemDetails** internally; the envelope is the wire format.
- All status codes (200/400/401/403/404/409/422/500) return the same envelope shape.

## Error Code Catalog
- Every error has a **stable unique code** like `USR-001`, `AUTH-002`, `BLD-014`.
- Codes live in a central registry (DB-backed for editability) keyed to localized messages.
- Naming: `<MODULE>-<NNN>` — 3-letter module + 3-digit number.
- Validation errors carry per-field codes inside `Fields[]`.

## MediatR Pipeline Behaviors (registration order matters)
v0.1.6 implementasyonu — 3 behavior kayıt edildi, sırası önemli:
1. `AuthorizationBehavior` — `[RequirePermission(...)]` attribute'unu okur; permission yoksa `AUTH-PERMISSION-DENIED` (403). Yetkisiz kullanıcıya validation hata mesajı bile dönmesin diye **EN BAŞTA**.
2. `ValidationBehavior` — Tüm `IValidator<TRequest>`'ları çalıştırır; **tüm ihlaller toplanır** (form için kullanıcı dostu). `VAL-001` default error code.
3. `LoggingBehavior` — `Information` seviyede `MediatR {Request} user={UserId} elapsed={ms}ms`. **Payload loglanmaz** (PII riski; v0.1.7 audit interceptor PII-aware audit yapar).

Sonra eklenecekler (Faz 1+):
- `UnhandledExceptionBehavior` — Faz 1'de eklenecek; outermost, envelope'a çevirir.
- `TransactionBehavior` — ICommand'lar için TX açar; ICommand vs IQuery marker'lar Faz 1'de.
- `CachingBehavior` — `ICacheableQuery` için Redis cache; v0.1.8'de.

**MediatR 11.x kullanılır** — 12.x ücretli lisansa geçti (Jimmy Bogard ticari model). 11.1.0 son MIT sürümü, pin'lendi.

## `[RequirePermission]` Kullanımı
- Command/Query class'ına eklenir: `[RequirePermission("Tenant.Read", "Tenant.Manage")]`.
- Çoklu kod **OR** semantiği (any-of). AND için ayrı attribute (v0.1.7+'da gerekirse).
- v0.1.6'da altyapı hazır ama handler'lara henüz konmadı — Faz 1 ManagementApp ile birlikte permission rol-map'i geldiğinde attribute'lar yerleştirilecek.

## `IPermissionChecker`
- `HasPermission(string)` / `HasAnyPermission(IReadOnlyList<string>)`.
- Implementasyon: `SessionPermissionChecker` — `ICurrentSessionAccessor.Current.Permissions.Contains(code)`.
- Anlık revocation: Redis session'daki permission listesi güncellendiği an yetki de değişir.

## Validation — FluentValidation
- **Use the last free version (11.x).** Version 12+ moved to commercial license; do not upgrade beyond 11.x. v0.1.6'da 11.11.0 pin'lendi.
- One validator per Command/Query; lives alongside the handler in the feature folder (örn. `LoginCommand.cs` + `LoginCommandHandler.cs` + `LoginCommandValidator.cs` aynı klasörde).
- Validators check input shape only — domain invariants belong in `BusinessRules` services.
- v0.1.6 ile **inline validation blokları handler'lardan kaldırıldı** — tüm `if (string.IsNullOrWhiteSpace(...))` türü kontroller Validator class'larına taşındı.
- Error code'lar `WithErrorCode("AUTH-001")` ile korunur — istemcilerin error code katalogu refactor'la değişmedi.

## API Versioning, Rate Limiting, Health
- URL versioning `/api/v1/...` via `Asp.Versioning`.
- Rate limiting via `Microsoft.AspNetCore.RateLimiting` — per-user, per-IP, and per-tenant.
- Health checks at `/health` (liveness), `/health/ready` (readiness — checks DBs + Redis).
- OpenAPI docs auto-generated; Scalar or Swashbuckle exposed at `/swagger` (dev only) or `/docs` (gated by permission in prod).

## HTTP Security Headers
- HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy.
- CORS configured per environment; never `AllowAnyOrigin` in prod.
- Anti-forgery tokens for Blazor Server pages.

## Idempotency
- POST/PUT for critical operations accept `Idempotency-Key` header.
- Server stores key → response in Redis for 24h; replays return cached response without re-executing.

## Pagination & Filtering Standard
- Cursor-based pagination preferred (`?cursor=...&limit=20`); offset allowed only for management screens.
- Filters via typed query objects; sorting via `?sort=field:asc,field2:desc`.
- Response includes `Meta.Pagination { NextCursor, PrevCursor, TotalCount? }`.
