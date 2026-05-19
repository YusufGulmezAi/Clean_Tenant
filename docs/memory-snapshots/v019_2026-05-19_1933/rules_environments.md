---
name: Environment & Secrets Management Rules
description: Four environments (Development/Test/Demo/Production), per-env config and secrets, vault strategy
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Four Environments
| Env | Purpose | Data | Access |
|---|---|---|---|
| **Development** | Local dev on Yusuf's machine + per-developer | Rich fake seed | Developers only |
| **Test** | CI + automated tests + manual QA | Minimal + ephemeral test fixtures (Testcontainers) | CI runners + QA |
| **Demo** | Sales demos / stakeholder previews | Curated showcase data | Demo users (sandboxed) |
| **Production** | Live customers | Real customer data | Operations + audited admins |

## Per-Environment Configuration Files
- `appsettings.json` — baseline (non-sensitive defaults).
- `appsettings.Development.json` — local dev overrides.
- `appsettings.Test.json` — CI / test env.
- `appsettings.Demo.json` — demo env.
- `appsettings.Production.json` — prod overrides.
- **No secrets in any `appsettings.*.json`** — only structural config (feature flags, public URLs, timeouts).
- `ASPNETCORE_ENVIRONMENT` env var selects the layer.

## Per-Environment Docker Compose
- `docker-compose.yml` — base service definitions.
- `docker-compose.dev.yml` — local dev overrides.
- `docker-compose.test.yml` — CI / test.
- `docker-compose.demo.yml` — demo deployment.
- `docker-compose.prod.yml` — production.
- Run pattern: `docker compose -f docker-compose.yml -f docker-compose.<env>.yml up`.

## Per-Environment Scripts
- `scripts/env-up.ps1 -Env <Dev|Test|Demo|Prod>` and `.sh` variant.
- `scripts/env-down.ps1 -Env <...>`.
- `scripts/env-reset.ps1 -Env <...>` — destructive, confirmation required.
- `scripts/env-migrate.ps1 -Env <...>` — applies EF migrations to all DBs for that env.
- `scripts/env-seed.ps1 -Env <...>` — env-appropriate seed (Dev = rich fakes, Test = fixtures, Demo = showcase, Prod = minimal).

## Secrets Management Strategy

### Development
- **`dotnet user-secrets`** per project (kept in `~/.microsoft/usersecrets/<id>/secrets.json`, never in repo).
- Local Docker overrides via `.env.development` (gitignored).
- `.env.development.example` is checked into repo with placeholder values + clear comments.

### Test
- CI: secrets injected via **GitHub Actions Encrypted Secrets** → mapped to env vars in the workflow.
- Local test runs: read from `.env.test` (gitignored).
- Testcontainers produce ephemeral DBs with throwaway credentials.

### Demo
- **`.env.demo`** on the demo host (file permissions `600`, owned by service account).
- Demo passwords rotated quarterly; documented in internal runbook (not in repo).

### Production
- **Primary:** environment variables injected by the orchestrator (Docker Swarm secrets / Kubernetes Secrets, depending on deployment platform).
- **Sensitive secrets (JWT signing key, DB passwords, SMTP, API keys):** stored in a **secrets vault**:
  - Phase 0 baseline: **HashiCorp Vault** (self-hosted) or cloud-native (Azure Key Vault / AWS Secrets Manager) — to be picked at deploy time.
  - Application uses `Microsoft.Extensions.Configuration.AzureKeyVault` or Vault provider; no plain `.env.production` on disk for prod long-term.
  - Bridge mode for first deployment: `.env.production` with `chmod 600` on host until vault is wired.
- **JWT signing keys** rotated on schedule (90 days), with overlap period for active tokens.
- **DB passwords** rotated per policy; connection strings refreshed via vault rotation hooks.

### What is a Secret (never in repo, never in `appsettings.*.json`)
- DB connection strings (full form with password)
- JWT signing keys + refresh-token encryption keys
- SMTP/Email API keys
- SMS gateway credentials
- Payment provider keys
- Storage (MinIO/S3/Azure Blob) credentials
- Identity Provider client secrets
- Redis connection strings (if password-protected)
- Any third-party API keys

### `.env.*.example` Files (checked into repo)
Every secret has a documented placeholder in `.env.<env>.example` with:
- The variable name.
- A one-line description of what it is.
- Example format (not real value).
- Whether it's required or optional.

### Onboarding Script
- `scripts/setup-env.ps1 -Env Development` walks a developer through:
  - Copying `.env.development.example` → `.env.development`.
  - Setting `dotnet user-secrets` for each project.
  - Verifying required tools (Docker, .NET 10, etc.).
  - Initializing local DBs via `env-up` + `env-migrate` + `env-seed`.

## Environment-Specific Behavior
- **Dev:** Detailed errors in responses, Swagger open, Hangfire dashboard open.
- **Test:** Same as Dev plus deterministic clock (`IClock` injected, test can fast-forward).
- **Demo:** Realistic but read-only-ish for many flows; daily reset job restores demo data.
- **Prod:** Generic error messages (no stack traces), Swagger gated by permission, Hangfire dashboard auth-required, all DEBUG logs off.
