# Architecture Decision Records

Why the system is the way it is. Each ADR is a decision that sets precedent:
Context (what forced the decision), Decision (what we chose), Consequences
(what it costs and what enforces it).

- Numbered sequentially: `NNNN-short-title.md`. Copy `template.md`.
- Written by whoever sets the precedent — usually via a `chief-architect`
  agent consultation. Rulings that change `docs/reference/conventions.md`
  require one.
- ADRs are immutable history: supersede with a new ADR rather than editing
  the decision of an old one (status updates are fine).

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-idkey-public-identifiers.md) | IdKey as the only public identifier | Accepted |
| [0003](0003-snake-case-database-naming.md) | snake_case database naming via explicit configuration | Accepted |
| [0004](0004-clean-architecture-layering.md) | Clean architecture layer dependencies | Accepted |
| [0005](0005-mcp-server-lineup.md) | MCP server lineup | Accepted |
