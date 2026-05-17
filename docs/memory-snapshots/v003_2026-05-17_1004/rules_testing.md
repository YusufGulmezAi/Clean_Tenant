---
name: Testing & Security Rules
description: Per-phase mandatory test suite + security test gate before phase closure
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Phase Gate Rule
**Every phase and sub-phase must have:**
1. A documented **test plan** (unit, integration, e2e where applicable, security checks).
2. All planned tests **passing** before the phase is considered complete.
3. A documented **security review** of the changes made in the phase.

Phase cannot be merged / committed-and-pushed unless gates 1–3 are green.

## Test Stack
- **xUnit** as test framework.
- **FluentAssertions** for readable assertions.
- **NSubstitute** for mocking (not Moq).
- **Testcontainers for .NET** with real PostgreSQL for integration tests — **no mocked DBs**.
- **bUnit** for Blazor component tests (Management + Portal).
- **WireMock.Net** for stubbing outbound HTTP.
- **Bogus** for test data generation.
- **Coverlet** for coverage reporting.

## Test Categories per Phase
1. **Unit tests** — Domain entities, business rule services, mappers, value objects.
2. **Handler tests** — Each Command/Query handler against an in-memory or Testcontainer DB.
3. **Pipeline behavior tests** — Validation, authorization, transaction, caching behave as specified.
4. **Integration tests** — API endpoint → handler → DB round trip, including auth + tenant resolution.
5. **Multi-tenancy isolation tests** — Verify Tenant A cannot read/write Tenant B's data (both shared and dedicated DB modes).
6. **Localization tests** — Each error code resolves in every supported culture.
7. **Turkish character / case-insensitive search tests** — Cover I/İ/ı/i edge cases explicitly.
8. **Timezone tests** — Cross-timezone date filter queries return correct rows.
9. **Concurrency tests** — Optimistic concurrency detects conflicts.
10. **Audit tests** — Mutations produce expected audit rows with before/after diffs.

## Coverage Targets
- Domain + Application layers: **≥ 85%** line coverage.
- Infrastructure: ≥ 60% (integration-heavy).
- Overall solution: **≥ 70%**.

## Security Testing per Phase
- **Static analysis** — SonarAnalyzer + Microsoft.CodeAnalysis.NetAnalyzers run on every build.
- **Dependency scan** — `dotnet list package --vulnerable` + Dependabot alerts; no known-vulnerable packages may ship.
- **Secrets scan** — gitleaks (or similar) runs in CI; commits with secrets blocked.
- **OWASP checklist review** — A1–A10 walkthrough on changed endpoints/features:
  - Auth/session: token expiry, refresh rotation, lockouts.
  - Authorization: vertical (privilege) + horizontal (tenant boundary) checks.
  - Injection: parameterized SQL (Dapper params, EF interpolation), no dynamic LINQ from raw input.
  - SSRF/XXE/Deserialization: validated.
  - Rate-limit + brute-force protection.
- **DAST / fuzz** for endpoints exposing public surface (OWASP ZAP baseline scan in CI).
- **Manual pentest checklist** for sensitive flows (login, password reset, payment, tenant onboarding).
- Findings documented in the phase's security report (`/docs/phases/vX.Y/security-report.md`).

## CI Test Pipeline
- `dotnet build -c Release` with warnings as errors.
- `dotnet test --collect:"XPlat Code Coverage"` — coverage report uploaded.
- Linting + analyzer gates.
- Testcontainers spin up Postgres, Redis if needed.
- Phase closes only when CI is green.
