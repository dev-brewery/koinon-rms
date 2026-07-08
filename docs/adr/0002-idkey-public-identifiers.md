# 0002. IdKey as the only public identifier

Date: 2026-07-03 (documents a decision in force since project start)
Status: Accepted

## Context

Entities use `int` identity primary keys internally (fast joins, small
indexes). Exposing sequential integers publicly leaks record counts and
growth rates, and invites enumeration attacks (`/people/1`, `/people/2`, …)
— a real concern for a system holding congregation members' and children's
data (check-in).

## Decision

`Entity.IdKey` — `IdKeyHelper.Encode(Id)` (`src/Koinon.Domain/Data/IdKeyHelper.cs`)
— is the only identifier that leaves the API. Routes take `string idKey`,
DTOs expose `IdKey`, responses never contain the integer `Id`. Route
templates never use `{id:int}` / `{id:long}`.

## Consequences

- Every controller decodes IdKey at the boundary; services accept IdKeys in
  their public signatures.
- Costs one encode/decode per request — negligible against the <200ms
  check-in budget.
- Enforcement: `RouteParameterTests` in `tests/Koinon.ArchitectureTests`
  fails the build on any controller action with an integer id route
  parameter or an `{id:int}`-style template. `koinon-dev` MCP
  `validate_routes` checks route strings pre-flight.
