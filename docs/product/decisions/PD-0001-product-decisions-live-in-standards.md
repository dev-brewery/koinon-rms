---
id: PD-0001
status: accepted
decision_type: structural
applies_to: [rag, standards, product-management, agent-workflow]
date: 2026-07-08
supersedes: 
---

# PD-0001. Product decisions live inside `koinon-standards`

## Context

Agents need durable product-management context while developing features: agreed behavior, UX/refinement decisions, and structural product choices that are not merely code examples. Without a deterministic retrieval path, agents can implement code that passes conventions while violating product intent.

The RAG topology already separates code impact (`koinon-code`), experiential lessons (`koinon-lessons`), and standards/canon (`koinon-standards`). Product/refinement decisions are canon-like: they constrain future implementation and should be reviewed like standards.

## Decision

Product-management, refinement, UX, and structural product decisions are indexed into the existing `koinon-standards` collection as `doc_type=product-decision`.

They do **not** get a separate Qdrant collection. The deterministic source of truth is markdown under `docs/product/decisions/*.md`; Qdrant is only a rebuildable projection.

## Rationale

These decisions are standards context, not code similarity and not experiential debugging lessons. Keeping them in `koinon-standards` lets an implementation workflow query one canon collection while still filtering by `doc_type=product-decision` when it needs product intent specifically.

## Implementation implications

- Before changing code, agents query `koinon-code`/impact analysis for likely regressions.
- Before finalizing an implementation approach, agents query `standards_search` with `scope=product_decisions` for accepted product/refinement constraints.
- Before approval, agents query `standards_search` with `scope=rules`, `scope=adrs`, or `scope=all` for conventions and architectural standards.
- New accepted product decisions must be committed as markdown in `docs/product/decisions/` and reindexed with `npm run rag:index:standards`.

## Regression risks

- Creating a separate product-decisions collection would fragment canon retrieval and reintroduce query-order drift.
- Writing decisions directly to Qdrant would make the index the source of truth and bypass review.
- Leaving decisions in chat history only would make future agents rediscover or reverse them.

## Related standards / ADRs

- `docs/adr/0005-mcp-server-lineup.md`
- `docs/adr/0006-standards-rag-collection.md`
- `docs/claude/mcp-tools.md`
