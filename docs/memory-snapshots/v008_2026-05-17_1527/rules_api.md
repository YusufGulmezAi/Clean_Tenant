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
1. `UnhandledExceptionBehavior` — outermost; converts uncaught exceptions to envelope.
2. `LoggingBehavior` — logs request name + duration; uses correlation id.
3. `PerformanceBehavior` — warns when handler > 500ms.
4. `AuthorizationBehavior` — enforces required permissions on the request.
5. `ValidationBehavior` — runs all `IValidator<TRequest>` (FluentValidation).
6. `TransactionBehavior` — opens a TX for `ICommand` requests (`IQuery` skipped).
7. `CachingBehavior` — for `ICacheableQuery` requests, reads/writes Redis.

## Validation — FluentValidation
- **Use the last free version (11.x).** Version 12+ moved to commercial license; do not upgrade beyond 11.x.
- One validator per Command/Query; lives alongside the handler in the feature folder.
- Validators check input shape only — domain invariants belong in `BusinessRules` services.

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
