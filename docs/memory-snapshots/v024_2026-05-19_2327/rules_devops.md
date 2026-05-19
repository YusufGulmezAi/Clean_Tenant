---
name: DevOps & Documentation Rules
description: Docker compose per environment, phase-versioned docs, conventional commits, CI/CD baseline
type: project
originSessionId: 20f787cc-e038-478b-8ebd-8728069291d3
---
## Docker
- Per-environment compose: `docker-compose.dev.yml`, `docker-compose.staging.yml`, `docker-compose.prod.yml` + base `docker-compose.yml`.
- Services: 3 × PostgreSQL (Main, Log, Audit), Redis, the apps, optional Seq (dev), reverse proxy (prod).
- Multi-stage Dockerfiles (build → runtime image; runtime uses `aspnet:10-alpine` where supported).
- Healthchecks defined for every service in compose.
- Named volumes for DB data; bind mount for logs (dev only).
- Migrations run by a dedicated `migrator` one-shot service at startup.

## Required Scripts (PowerShell + Bash variants)
- `scripts/docker-up.ps1` / `.sh` — pull, build, start stack.
- `scripts/docker-down.ps1` / `.sh` — stop containers, preserve volumes.
- `scripts/docker-reset.ps1` / `.sh` — stop containers, **drop volumes**, rebuild, re-seed. Asks for confirmation.
- `scripts/db-migrate.ps1` — apply EF migrations to all 3 DBs.
- `scripts/db-seed.ps1` — seed dev/test data.

## Secrets
- Dev: `dotnet user-secrets` (per project).
- Local Docker: `.env` files (gitignored) + `.env.example` checked in.
- Prod: env vars from orchestrator / vault. No secrets in `appsettings.*.json`.
- JWT signing keys rotate on schedule (documented).

## Git
- **Branch strategy:** GitFlow lite — `main` (prod), `develop` (integration), `feature/*`, `hotfix/*`, `release/*`.
- **Conventional Commits** mandatory: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`, `perf:`, `build:`, `ci:`.
- Commit message includes phase ref when relevant: `feat(buildings): add CreateBuilding handler [phase v1.2]`.
- `.editorconfig`, `.gitattributes`, `.gitignore` checked in from day one.
- PR template + Issue templates in `.github/`.
- Dependabot enabled for NuGet + GitHub Actions.

## CI/CD (GitHub Actions)
- Workflows: `build-test`, `lint`, `security-scan`, `docker-build`, `release`.
- Triggered on PR + on push to `develop`/`main`.
- Required checks for merge: build, tests, analyzer, coverage threshold, secrets scan, vulnerable-package scan.
- Release workflow tags + publishes images on merge to `main`.

## Phase Documentation (versioned)
- Path: `/docs/phases/v<major>.<minor>/`
- Each phase folder contains:
  - `README.md` — scope, goals, dependencies, exit criteria.
  - `design.md` — design decisions, diagrams, ADRs.
  - `test-plan.md` — what will be tested, how.
  - `test-report.md` — results after execution.
  - `security-report.md` — OWASP review + scan results.
  - `CHANGELOG.md` — append-only per sub-phase, with version stamps.
- Sub-phases live as `vX.Y.Z` subfolders or sections within the phase folder, depending on size.
- Every implementation note or addition is appended with a version stamp like `## v1.2.3 — 2026-05-17` so history is preserved chronologically.

## Phase Completion Ritual
1. All tests green (per `rules_testing.md`).
2. Security report signed off.
3. Phase docs updated and versioned.
4. Conventional Commit + push to remote.
5. PR opened to `develop` (or `main` for hotfix); reviewed; merged.
6. Tag pushed when phase concludes a release.

## Parallel Agent Workflow (Multi-Agent Phase Execution)
For phases with independent workstreams, the main session can dispatch parallel sub-agents to compress wall-clock time.

**When to parallelize:**
- Independent feature modules within the same phase (e.g., Localization, Audit Viewer, Tenant Onboarding can each be a separate agent in a feature phase).
- Cross-cutting tasks that don't share files (test writing for module A while module B is being implemented).
- Documentation generation in parallel with implementation review.

**When NOT to parallelize:**
- Foundational setup (solution scaffold, base classes, shared interfaces) — must be serial and live in main session because everything else depends on them.
- Tasks touching the same files or migration order.
- Schema changes (always serial — migration order matters).

**Process:**
1. Main session defines **contracts first** (interfaces, DTOs, API shapes, DB schema).
2. Main session **commits the contracts** so all agents see the same baseline.
3. Spawn parallel agents — each gets a self-contained brief: scope, files allowed, interfaces to implement, tests to pass.
4. Use `Agent` tool with `isolation: "worktree"` so agents work on isolated git worktrees and don't step on each other.
5. Main session **integrates** agent results: reviews each agent's diff, merges worktree branches, runs full test suite.
6. Conflict resolution stays in main session — agents don't merge to develop themselves.

**Per-phase doc lists which sub-tasks are parallelized** in the phase's `README.md` under a "Parallel Execution Plan" heading.
