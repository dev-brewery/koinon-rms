---
name: koinon-conventions
description: >
  Load the agreed Koinon RMS architectural conventions before writing or
  reviewing ANY code in this repo. Use when: starting a coding task, adding an
  endpoint/entity/service/page/hook, reviewing a diff, or unsure how something
  "should" be done here. Triggers: "conventions", "how do we do X here",
  "what's the pattern for", adding features, refactoring, code review.
---

# Koinon RMS Conventions

## Step 1 — Load the canon

Read `docs/reference/conventions.md`. That file is the single source of truth
(layering, API envelope, IdKey, single frontend client, data rules, the
feature-slice flow). Do not infer conventions from grep samples — files predating
the canon exist and copying one propagates drift.

## Step 2 — Look up, don't guess

Use the `koinon-index` MCP server (auto-indexes the codebase; always current):

| Need | Tool |
|------|------|
| The rules | `get_conventions` |
| How an existing feature is wired, end to end | `trace_feature { term }` |
| Does X already exist? Where? | `search_index { query, kind? }` |
| All endpoints under a route | `list_endpoints { prefix? }` |

Workflow for any change: `get_conventions` → `trace_feature` on the nearest
existing feature → mirror its wiring exactly. Only fall back to Grep/Read for
file contents the index has already pointed you at.

## Step 3 — Non-negotiables (fast recall)

- Controllers: validate → delegate to Application service → `Ok(new { data = dto })`.
  Errors are RFC 7807 `ProblemDetails`. No business logic.
- URLs use `IdKey`, never integer IDs.
- Frontend API calls ONLY via `src/web/src/services/api/` modules on the shared
  `client.ts` (never a new client, axios, or raw fetch in components;
  `src/web/src/api/client.ts` is dead code — never import it).
- Server state = TanStack Query hooks; Zod at boundaries; strict TS, no `any`.
- Schema changes only via EF migrations. Reads `AsNoTracking()`; never mutate
  a NoTracking entity.
- New pattern or deviation? Consult the `chief-architect` agent BEFORE
  implementing; precedent changes get an ADR in `docs/adr/`.
