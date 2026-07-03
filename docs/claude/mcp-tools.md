# MCP Tools Reference

Three servers are configured in `.mcp.json`. All work on a fresh clone on
Windows and Linux — relative paths or npx only. The lineup and the reasoning
behind it are recorded in `docs/adr/0005-mcp-server-lineup.md`; changes to
the lineup go through that ADR.

| Server | Purpose | Works when |
|--------|---------|------------|
| `koinon-index` | Structural lookup: conventions, endpoints, feature traces | Always (zero deps) |
| `koinon-dev` | Validators, graph queries, impact analysis, RAG + lessons | Always (committed `dist/`) |
| `postgres` | Query the dev database directly instead of guessing schema | Demo/dev stack running |

**Session start:** `lesson_search` on `koinon-dev` with your task keywords
(the indexed institutional-lessons store: gotchas, root causes,
why-decisions), then `get_conventions` on `koinon-index` (the rules).

## koinon-index

Zero-dependency structural index (`tools/mcp-servers/codebase-index/server.js`).

| Tool | Use |
|------|-----|
| `get_conventions` | Serves `docs/reference/conventions.md` live |
| `search_index` | Find symbols/files by name |
| `list_endpoints` | All API endpoints with controllers |
| `trace_feature` | Full entity→DTO→service→controller→frontend chain for a feature |
| `get_stats` | Index size/health |
| `reindex` | Rebuild after large changes |

## koinon-dev

Validation and architecture server (`tools/mcp-koinon-dev`). Ships a
committed `dist/` so it runs with zero setup; CI rebuilds and diffs it. If
you change its `src/`, run `npm run mcp:build` and commit the result.

| Tool | Use |
|------|-----|
| `validate_naming` | snake_case / PascalCase / camelCase / route-shape checks (`{type, names[]}`) |
| `validate_routes` | IdKey-not-int and `/api/v1` shape for route strings |
| `validate_dependencies` | Clean-architecture layer direction for an import list |
| `detect_antipatterns` | Scans a code snippet for known legacy patterns |
| `get_architecture_guidance` | Topic-based guidance (entity, api, ...) |
| `query_api_graph` | `get_controller_pattern` / `get_entity_chain` / `list_inconsistencies` / `validate_new_controller` against `tools/graph/graph-baseline.json` |
| `get_impact_analysis` | Affected files + work units for a file path |
| `rag_search` / `rag_impact_analysis` / `rag_index_status` | Semantic code search over the indexed codebase (stack below); degrades gracefully with a warning when it's down |
| `lesson_search` | Semantic search over the team's institutional lessons (indexed `koinon-lessons` collection). Run at session start and before debugging anything that smells like a known trap |
| `lesson_add` | Record a lesson that cost real time (gotcha, root cause, why-decision) — self-contained text + topic tag. Rules do NOT go here; they go in conventions.md via ADR |

There is no template generator: get the pattern from a real exemplar via
`koinon-index trace_feature` plus `docs/reference/conventions.md` — live code
can't drift, templates did.

## postgres

`npx @modelcontextprotocol/server-postgres` against
`postgresql://koinon:koinon@localhost:5432/koinon`. Only useful while the
dev/demo database is up (`docker compose up -d` or the full stack). Failures
mean "stack is down", not "server is broken".

## Institutional lessons (via koinon-dev)

Team knowledge lives in an **indexed location** — the `koinon-lessons`
Qdrant collection on the inference server — never as a blob in the repo
(ADR 0005). Two-tier rule:

- **Rules** → `docs/reference/conventions.md`, changed via ADR. Reviewable,
  human-maintained, enforced by tests where possible.
- **Lessons** (gotchas, root causes, why-decisions — anything experiential
  that cost real time) → `lesson_add`. Query with `lesson_search` at
  session start and whenever a symptom feels like a known trap
  (e.g. "entity update silently not saving" surfaces the NoTracking trap).

## RAG semantic-search stack

Everything lives on the team inference server at `192.168.1.225` — **no
localhost dependencies, nothing to install or run on your machine**:

- **Qdrant** `http://192.168.1.225:6333` — holds the indexed `koinon-code`
  collection (768-dim, nomic-embed-text).
- **Embeddings** `http://192.168.1.225:4000` — the model gateway (also
  proxies the team's chat models; `GET /v1/models` lists them). Serves
  `nomic-embed-text` via OpenAI-compatible `/v1/embeddings`; the client
  also speaks the Ollama protocol for any future endpoint swap.

Overrides (`RAG_HOST`, `QDRANT_URL`, `EMBEDDINGS_URL`) exist for exceptional
setups only — never commit a config that points at localhost. When an
endpoint is down, `rag_*` return empty results with a warning — never
blocked. Reindex the shared collection after large changes:
`npm run rag:reindex` (incremental) or `npm run rag:index` (full).

## Removed servers (July 2026) and how to get them back

- `filesystem` — superseded by built-in Read/Glob/Grep/Edit. Don't re-add.
- `github` — superseded by the `gh` CLI. If a GitHub MCP is ever wanted,
  use the official remote server; do not resurrect shell-wrapper scripts.
- `code-rag` (`@ancoleman/qdrant-rag-mcp`) — duplicate RAG frontend; the
  capability lives in koinon-dev `rag_*`. Don't re-add.

Rule for any new server: relative paths or npx only, fresh-clone green on
both OSes, documented here — or it doesn't go in `.mcp.json`.

## Graph Baseline System

Baseline at `tools/graph/graph-baseline.json`. Commands:
```bash
npm run graph:validate   # Validate current code against baseline
npm run graph:update     # Regenerate after structural changes
```

Update baseline when adding: entities, DTOs, endpoints, components, or
renaming fields. Not for implementation-detail changes.
