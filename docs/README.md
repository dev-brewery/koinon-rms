# docs/ — map

Start at the repo root `CLAUDE.md` (agents) or `README.md` (humans).

## Canonical — maintained, trust these

| Location | Purpose |
|----------|---------|
| `reference/conventions.md` | **Single source of truth** for architecture conventions (served live by the `koinon-index` MCP `get_conventions`) |
| `reference/qa-playbook.md` | Testing handbook: tiers, printer mocking, flake policy |
| `reference/api-contracts.md` | REST API contracts |
| `reference/entity-mappings.md` | Entity definitions and relationships |
| `reference/migration-guidelines.md` | EF Core migration playbook |
| `reference/work-breakdown.md` | Work-unit specs |
| `adr/` | Architecture Decision Records — why things are the way they are |
| `claude/mcp-tools.md` | MCP server + tool reference |

## Useful context — mostly current, verify before relying

- `architecture.md` — system overview
- `features.md`, `features/` — feature descriptions
- `qa-test-protocol.md` — manual QA protocol
- `styleguide/` — UI style guide
- `performance/` — perf notes for the check-in path
- `ci-migration-safety-best-practices.md` — background for the CI migration check

## Historical — do not trust for current state

- `archive/` — superseded reports, summaries, old setup guides (see its README)
- `sprints/`, `plans/`, `proposals/`, `postmortems/` — point-in-time records
- `ALPHA-*.md`, `issue-256-spec.md`, `navigation-audit.md`,
  `rag-integration-manual-changes.md`, `ERROR_TRACKING.md` — alpha-era artifacts
- `examples/`, `components/` — check dates before copying patterns
