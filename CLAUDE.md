# CLAUDE.md — Koinon RMS

Church Management System. .NET 8 API (`src/Koinon.Api`) with clean
architecture (`src/Koinon.Domain|Application|Infrastructure`), React 18 +
TypeScript frontend (`src/web`), PostgreSQL (PostGIS) + Redis via Docker.
MVP is the check-in kiosk: <200ms online search, <50ms offline.

Every path and command in this file exists. If you find one that doesn't,
that's a bug — fix the doc.

## Session start

1. `lesson_search` on the `koinon-dev` MCP server with your task keywords —
   the team's institutional lessons (gotchas, root causes, why-decisions)
   live in an indexed store on the inference server, not in this repo.
2. Load conventions: the `koinon-conventions` skill, or `get_conventions`
   on `koinon-index`. `docs/reference/conventions.md` is the single source
   of truth for rules; changes to it require an ADR.

When something costs you real time, put it where the next person will find
it: a rule → `conventions.md` via ADR; an experiential lesson →
`lesson_add`. Never a repo blob, never just the chat transcript.

## Skills — invoke before working

| Skill | When |
|-------|------|
| `koinon-conventions` | Before writing or reviewing any code |
| `koinon-feature-slice` | Any new feature/endpoint/entity/page — the layer-by-layer checklist |
| `koinon-demo-stack` | Running the app, demoing, login/container issues |
| `koinon-e2e` | Writing or running browser tests (required for user-facing features) |

Architectural decisions or precedent changes: consult the `chief-architect`
agent; outcomes are recorded as ADRs in `docs/adr/`.

## Non-negotiable invariants

- **IdKey, never integer IDs.** `Entity.IdKey` is the only identifier that
  leaves the API — routes, DTOs, responses. Enforced by
  `tests/Koinon.ArchitectureTests` (RouteParameterTests).
- **NoTracking is the global default.** Queries that load entities you will
  MUTATE need `.AsTracking()` — without it SaveChanges silently drops the
  change (real data-loss bugs: person merge #708). Reads need nothing.
- **snake_case columns via explicit `HasColumnName`.** No global convention
  exists; every entity needs a `*Configuration.cs`. Enforced by
  SnakeCaseNamingTests.
- **Layer direction is absolute:** Api → Application → Domain;
  Infrastructure implements Application interfaces. No business logic in
  controllers. Enforced by LayerDependencyTests.
- **Success envelope** `Ok(new { data = dto })`; errors are RFC 7807
  `ProblemDetails`. `[Authorize]` by default; `[AllowAnonymous]` needs
  justification (kiosk `/checkin` and `/groups` are deliberately public).
- **Schema changes only via EF migrations**
  (`dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api`).
  Postgres images must include PostGIS.
- **Frontend:** one HTTP client (`src/web/src/services/api/client.ts`);
  TanStack Query hooks for server state; TypeScript strict, no `any`, no
  class components. (`src/web/src/api/client.ts` is dead code — never import.)
- **Structural changes update the graph baseline:** `npm run graph:update`,
  commit `tools/graph/graph-baseline.json` with the code.

## Commands

```bash
docker compose up -d                      # dev infra (postgres+redis)
docker compose -f docker-compose.full.yml up -d   # full demo stack
npm run build                             # dotnet build
npm test                                  # dotnet test + vitest
npm run typecheck && npm run lint         # frontend gates
npm run dev:api                           # API watch mode
npm run dev:web                           # Vite dev server
npm run graph:validate                    # graph baseline drift check
npm run validate:quick                    # what pre-commit runs
tools/qa/run-e2e-demo.ps1                 # one-command E2E (Windows)
tools/qa/run-e2e-demo.sh                  # one-command E2E (Linux/macOS)
```

Quick reference: PostgreSQL `localhost:5432` (koinon/koinon), Redis `6379`,
API `5000`, Web dev `5173`, demo stack web `3000`. Connection string name is
`ConnectionStrings__DefaultConnection` — everywhere.

## MCP servers

Three, all fresh-clone safe (see `docs/claude/mcp-tools.md`, ADR 0005):
`koinon-index` (conventions, endpoints, `trace_feature`), `koinon-dev`
(naming/route/dependency validators, graph queries, impact analysis,
semantic `rag_search`), `postgres` (query the dev DB). Prefer
`koinon-index`/`koinon-dev` lookups over grep-and-guess: `trace_feature`
shows the complete existing pattern for anything you're about to build.

## Don'ts

- No absolute or machine-specific paths in `.mcp.json`, scripts, or config.
- No bash-only dev scripts — Node or `.ps1`/`.sh` twins (Windows and Linux
  are both first-class).
- No `--no-verify`; if a hook fails wrongly, fix the hook.
- No hand-written SQL migrations; no `Database:MigrateOnStartup` outside
  containers.
- Security, validation, and missing tests are fixed in-change — never
  deferred as tech debt.

## Key documentation

| Doc | Purpose |
|-----|---------|
| `docs/reference/conventions.md` | Canonical conventions (via `get_conventions`) |
| `docs/reference/qa-playbook.md` | Testing handbook |
| `docs/reference/api-contracts.md` | REST contracts |
| `docs/reference/entity-mappings.md` | Entity definitions |
| `docs/reference/migration-guidelines.md` | EF migration playbook |
| `docs/adr/` | Architecture decisions |
| `docs/README.md` | Map of everything else |
