# 0005. MCP server lineup

Date: 2026-07-03
Status: Accepted

## Context

At the July 2026 handoff audit, `.mcp.json` configured seven servers; five
were broken on a fresh clone (machine-absolute `G:/` paths, a never-built
`dist/`, a never-installed `node_modules`, a wrapper script that didn't
exist, an external Qdrant nothing started). Broken servers are worse than
missing ones: agents learn to ignore MCP errors, and docs that promise
tooling that doesn't exist train people to distrust the docs.

The bar applied per server: it must (a) work on a fresh clone on Windows
and Linux, (b) save tokens / measurably help agents produce conforming
code, and (c) be fully documented — or it goes. A server is cut only when
something *enforced and always-present* supersedes it, never merely because
it has external dependencies.

## Decision

Four servers in `.mcp.json` (see `docs/claude/mcp-tools.md` for tools):

| Server | Verdict | Notes |
|--------|---------|-------|
| `koinon-index` | Keep | Zero deps, relative path; serves conventions live |
| `koinon-dev` | Keep (repaired) | `PROJECT_ROOT` now defaults to cwd; `dist/` is **committed** so it's zero-step (CI rebuilds and diffs); the drifted `get_implementation_template` tool was **removed** — live exemplars via `trace_feature` can't drift, templates did |
| `postgres` | Keep | npx; useful whenever the dev DB is up |

Removed:

- `filesystem` — superseded by built-in Read/Glob/Grep/Edit (not optional
  habits; the only way agents touch files).
- `github` — superseded by the `gh` CLI, which workflows and agents already
  use unprompted.
- `memory` — a repo-committed knowledge graph (`.claude/memory.jsonl`) was
  briefly trialed during the handoff and rejected: institutional knowledge
  belongs in an **indexed location**, not a blob committed to production
  code. The replacement is two-tier: **rules** live in the reviewable
  canon (conventions.md via ADR, skills, CLAUDE.md); **experiential
  lessons** (gotchas, root causes, why-decisions) live in the
  `koinon-lessons` Qdrant collection on the team inference server,
  written and queried semantically via koinon-dev's `lesson_add` /
  `lesson_search` tools. Nothing knowledge-shaped is committed to the
  repo, and nothing depends on a per-machine store.
- `code-rag` — duplicate RAG frontend; the capability is koinon-dev's
  `rag_*` tools backed entirely by the team inference server at
  `192.168.1.225` (Qdrant `:6333` with the `koinon-code` collection;
  embeddings via the model gateway `:4000`, OpenAI-compatible
  `/v1/embeddings` serving nomic-embed-text — the client speaks both that
  and the Ollama protocol). **No localhost dependencies**: nothing
  RAG-related runs on dev machines. Graceful degradation when either
  endpoint is down.

Rule for additions: relative paths or npx only, fresh-clone green on both
OSes, documented in `docs/claude/mcp-tools.md` — or it doesn't go in.

## Consequences

- Everything configured works; an MCP connection failure is now signal, not
  noise.
- Semantic search requires explicitly starting the RAG stack — acceptable:
  it degrades with a warning and `koinon-index` covers structural lookup.
- Enforcement: CI rebuilds `tools/mcp-koinon-dev` and fails on `dist/`
  drift; the fresh-clone verification checklist requires all four servers
  connecting.
