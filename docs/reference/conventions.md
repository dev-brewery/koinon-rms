# Koinon RMS — Agreed Conventions (Canonical)

This is the single source of truth for architectural conventions. The
`koinon-index` MCP server serves this file via `get_conventions`; the
`chief-architect` agent enforces it; the `koinon-conventions` skill loads it.
Change it only via an ADR in `docs/adr/`.

## Layering (dependency rule is absolute)

```
Koinon.Api → Koinon.Application → Koinon.Domain
                    ↑
        Koinon.Infrastructure (implements Application interfaces)
```

- **Domain** (`src/Koinon.Domain`): entities + interfaces. Zero dependencies.
- **Application** (`src/Koinon.Application`): service interfaces + implementations,
  DTOs (C# records), FluentValidation validators. Depends only on Domain.
- **Infrastructure** (`src/Koinon.Infrastructure`): `KoinonDbContext`, EF Core
  configurations, migrations, Redis, external providers (Twilio, SMTP).
- **Api** (`src/Koinon.Api`): controllers, middleware, JWT auth, SignalR hubs.
  Controllers validate → delegate to an Application service → shape the HTTP
  response. **No business logic in controllers.**

## API surface

- Base path `/api/v1/`; controller attribute is `[Route("api/v1/[controller]")]`.
- **IdKey in URLs, never integer IDs.** `Entity.IdKey` (`IdKeyHelper.Encode(Id)`)
  is the only identifier that leaves the API.
- **Success envelope:** `return Ok(new { data = <dto> });` — paginated endpoints
  put a `PagedResult` inside `data`. No bare `Ok(dto)`.
- **Errors:** RFC 7807 `ProblemDetails` only (via `GlobalExceptionHandler`
  middleware or explicit `BadRequest(new ProblemDetails { ... })`).
- Every endpoint: authorization attribute (`[Authorize]` class-level default;
  `[AllowAnonymous]` needs justification), FluentValidation for request bodies,
  `CancellationToken ct` threaded through, entry in `docs/reference/api-contracts.md`.
- New service = interface in `Application/Interfaces` + implementation in
  `Application/Services` + DI registration in
  `Application/Extensions/ServiceCollectionExtensions.cs` (`AddKoinonApplicationServices`).

## Data

- PostgreSQL; snake_case tables/columns; `id` int identity PK; `{entity}_id` FKs.
- Entities extend `Entity` (Id, Guid, IdKey, CreatedDateTime, ModifiedDateTime).
- Schema changes ONLY via EF migrations:
  `dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api`.
- **The DbContext defaults to NoTracking globally**
  (`PostgreSqlProvider.UseQueryTrackingBehavior`). Every query that loads an
  entity you intend to MUTATE must call `.AsTracking()` explicitly — without
  it the mutation silently drops on SaveChanges (no error, no update). This
  has caused real data-loss bugs (person merge #708 sweep + PersonMergeService).
  Reads need no annotation; they're already untracked.
- No N+1: `Include`/projection. No synchronous DB calls.
- `Database:MigrateOnStartup` config is for containerized dev/demo only.

## Frontend (src/web)

- **One HTTP client:** `src/web/src/services/api/client.ts` (fetch + token
  refresh + envelope handling). All API calls go through a service module in
  `src/web/src/services/api/`. Never: a second client, axios, raw `fetch` in
  components, or re-deriving the base URL from `import.meta.env` in feature code.
  (`src/web/src/api/client.ts` is dead code slated for deletion — never import it.)
- Server state via TanStack Query hooks in `src/web/src/hooks/` (query keys
  scoped per feature); Zod validation at API boundaries
  (`services/api/validators.ts`).
- TypeScript strict, no `any`, functional components only.
- Routes registered in `App.tsx`: lazy-loaded, inside `ProtectedRoute` +
  `AdminLayout` for admin pages; kiosk (`/checkin`) and `/groups` are public.

## The feature slice (logic flow for a new feature)

```
Domain entity → EF configuration + migration → Application interface/service/DTOs/validator
  → DI registration → API controller (envelope + IdKey + [Authorize])
  → frontend types (services/api/types.ts) → api service module → Zod validator
  → TanStack Query hook → page component → route in App.tsx → tests (unit + E2E)
```

Skipping layers (controller hitting DbContext, component calling fetch) is a
convention violation, not a shortcut.

## Infrastructure / build

- `docker-compose.yml` = dev infra only (PostGIS-enabled postgres + redis).
- `docker-compose.full.yml` = full containerized stack; config keys must match
  what `Program.cs` reads (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`,
  `Cors__AllowedOrigins__N`, `Database__MigrateOnStartup`).
- Postgres images must include PostGIS (migrations enable the extension).
- `src/Koinon.Api/Dockerfile` builds the API project graph, not the solution.
- New config keys: add to `Program.cs`/appsettings AND both compose files.

## Process

- Architectural decisions and precedent changes → consult the `chief-architect`
  agent; record outcomes as ADRs in `docs/adr/`.
- Before implementing, query the `koinon-index` MCP server
  (`trace_feature`, `find_endpoint`, `search_index`) to find the existing
  pattern for whatever you're adding — do not grep-and-guess, and do not
  invent a second way to do something that already has a way.
- Security, validation, and missing tests are fixed in-change, never deferred
  as tech debt.
