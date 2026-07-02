---
name: koinon-feature-slice
description: >
  Step-by-step checklist for implementing a feature slice in Koinon RMS —
  entity through migration, service, controller, frontend service, hook, page,
  route, and tests, in the agreed order with the agreed patterns. Use when:
  adding a new feature, endpoint, entity, or admin page; wiring frontend to a
  new API. Triggers: "add a feature", "new endpoint", "new entity", "build the
  X page", "implement issue #N".
---

# Koinon Feature Slice

Before step 1: run `koinon-index → get_conventions`, then
`trace_feature { term: <nearest-existing-feature> }` and keep that trace open —
you are mirroring its wiring, file for file. Consult the `chief-architect`
agent if anything about your slice has no existing precedent.

## Backend (work inward-out: Domain → Application → Api)

1. **Entity** — `src/Koinon.Domain/Entities/<Name>.cs`, extends `Entity`
   (gives Id/Guid/IdKey/audit stamps). Navigation properties, no logic beyond
   invariants. Zero external dependencies.
2. **EF configuration** — mapping in `src/Koinon.Infrastructure/Data/`
   (snake_case table/columns, `{entity}_id` FKs, indexes for query paths).
3. **Migration** —
   `dotnet ef migrations add Add<Name> -p src/Koinon.Infrastructure -s src/Koinon.Api`.
   Review the generated SQL; never hand-edit the snapshot.
4. **DTOs** — records in `src/Koinon.Application/DTOs/` (`<Name>Dto`,
   `<Name>SummaryDto`, `Create<Name>Request`, `Update<Name>Request`).
   Expose `IdKey` strings, never `Id` ints.
5. **Validator** — FluentValidation in `src/Koinon.Application/Validators/`
   for every request DTO.
6. **Service** — interface `I<Name>Service` in `Application/Interfaces` +
   implementation in `Application/Services`. Reads `AsNoTracking()` with
   projection to DTOs; mutations load tracked entities. `CancellationToken`
   on every method. Register in
   `Application/Extensions/ServiceCollectionExtensions.cs`.
7. **Controller** — `src/Koinon.Api/Controllers/<Name>sController.cs`:
   `[ApiController] [Route("api/v1/[controller]")] [Authorize]`, primary
   constructor injection, validate → delegate → `Ok(new { data = result })`,
   404/400 as `ProblemDetails`. No business logic.
8. **Contract doc** — add endpoints to `docs/reference/api-contracts.md`.
9. **Backend tests** — service tests in `tests/Koinon.Application.Tests`,
   controller/integration tests in `tests/Koinon.Api.Tests`. Run `dotnet test`.

## Frontend (src/web)

10. **Types** — add DTO interfaces to `src/services/api/types.ts` (camelCase
    mirror of the C# records).
11. **API service** — `src/services/api/<name>.ts`: exported async functions
    using `get/post/put/del` from `./client` (NEVER raw fetch or a new client),
    unwrap the `{ data }` envelope, Zod-validate via `validators.ts` pattern.
    Export from `services/api/index.ts`.
12. **Hook** — `src/hooks/use<Name>.ts`: TanStack Query
    (`useQuery`/`useMutation`), feature-scoped query keys, invalidate on
    mutation success.
13. **Page** — `src/pages/admin/<name>/…` functional components; list/detail/
    form split like the families feature; UI state local, server state in hooks.
14. **Route** — register in `App.tsx`: lazy import + `<Route>` inside the
    `/admin` `ProtectedRoute` layout (or public section if truly public).
15. **Frontend tests** — hook/component tests next to code; E2E flow in
    `src/web/e2e` if it's a user-facing flow (follow the `koinon-e2e` skill:
    mocked-API feature spec + one `@smoke` golden-path test). Run
    `npm run test` and `npx tsc --noEmit` from `src/web`.

## Done means

- `dotnet build` + `dotnet test` green; `tsc --noEmit` + frontend tests green.
- Feature verified live against the demo stack (see `koinon-demo-stack` skill).
- `trace_feature` on your new term shows the complete chain with no missing layer.
- No new pattern introduced without a chief-architect ruling + ADR.
