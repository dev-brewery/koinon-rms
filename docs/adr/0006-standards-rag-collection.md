# 0006. Standards RAG collection (`koinon-standards`)

Date: 2026-07-07
Status: Accepted

## Context

ADR 0005 fixed the RAG topology at two Qdrant collections on the team
inference server (`192.168.1.225:6333`): `koinon-code` (source, indexed by
`tools/rag/index-codebase.py`) and `koinon-lessons` (experiential knowledge,
written via koinon-dev's `lesson_add`). Embeddings are nomic-embed-text
(768-dim) via the `:4000` gateway. No RAG dependency runs on dev machines.

The Phase 2 architect-review gate (harness-implementation-plan.md) judges
code-change proposals for conformance against the governing standards:
`docs/reference/conventions.md`, `docs/adr/*`, `docs/reference/api-contracts.md`,
`docs/reference/entity-mappings.md`, and the other `docs/reference/*` handbooks.
The gate is only as good as its ability to retrieve the *precise* rule a
change touches (drift mechanism #3 — haystack precision collapse).

Those standards are not retrievable today. The indexer's
`FILE_EXTENSIONS` (`tools/rag/utils.py:72`) is code-only
(`.cs/.ts/.tsx/.js/.jsx`); both the full (`index-codebase.py:121`) and
incremental (`reindex-changes.py:91`) passes gate on it, so no markdown has
ever entered `koinon-code`. The gate would judge conformance against a
corpus that does not contain the rules.

## Decision

Add a third, dedicated Qdrant collection **`koinon-standards`** to the RAG
topology, on the same inference server, same embedding model and endpoints,
populated by a new `tools/rag/index-standards.py`.

- **Dedicated collection, not mixed into `koinon-code`.** `koinon-code`'s
  payload schema (`layer`, `type`) is code-shaped and drives `rag_search`
  filters; markdown chunks would pollute code-similarity results and the
  filter vocabulary. Standards get their own payload
  (`path`, `doc_type`, `section`, `content`, `chunk_index`).
- **Scope:** `docs/reference/*.md` + `docs/adr/*.md`, excluding non-standard
  files (`docs/adr/template.md`, and README indexes).
- **Heading-aware chunking** (split on markdown headings, cap each section at
  the shared `CHUNK_SIZE`) so retrieval returns focused rule sections.
- **Endpoints and embeddings are inherited unchanged** from
  `tools/rag/utils.py` (`QDRANT_URL`/`RAG_HOST`, `get_embeddings`,
  nomic-embed-text, 768-dim). No localhost is introduced (ADR 0005 holds).
  `utils.py` and the existing code-indexing path are not modified.
- **The index is a derived retrieval artifact, never a source of truth.**
  `docs/reference/*.md` and `docs/adr/*` remain the reviewable canon
  (consistent with ADR 0005's rules-vs-lessons split); `koinon-standards`
  is a rebuildable projection of them, exactly as `koinon-code` is of the
  source tree.

## Consequences

- The architect-review gate can be fed the precise standard a change
  implicates, from an indexed store rather than the raw doc tree.
- **Retrieval wiring is a follow-on, not delivered here.** The `rag_search`
  MCP tool hardcodes `koinon-code` (`tools/mcp-koinon-dev/src/rag-client.ts:18,242`);
  it cannot see `koinon-standards`. Phase 2's `architect-review.mjs` must
  query this collection directly (or the client must be extended to accept a
  collection, mirroring the `LESSONS_COLLECTION` env pattern). Until that
  exists, `koinon-standards` is reachable only by direct Qdrant search.
- **Freshness is manual for now.** The incremental reindexer
  (`reindex-changes.py`, run by `validate.py`) is code-only, so editing a
  convention or adding an ADR does not refresh `koinon-standards`.
  `index-standards.py` must be re-run whenever a standard changes. Because
  standards change only through a deliberate, gated act (an ADR merge), the
  re-run is tied to that process rather than automated; automating a markdown
  incremental path is deferred as tracked technical debt.
- The fresh-clone / RAG-stack bring-up documentation and any `npm run rag:*`
  scripts must learn about the new collection so it is built alongside
  `koinon-code`.
