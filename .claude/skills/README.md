# Koinon RMS — Claude Code Skills

Project-scoped skills for agentic development work. They are versioned with
the repo, so **cloning the repo on any machine is the export** — Claude Code
discovers `.claude/skills/*/SKILL.md` automatically.

| Skill | Use for |
|-------|---------|
| `koinon-conventions` | Loading the agreed architectural canon + how to query the codebase index instead of grepping |
| `koinon-demo-stack` | Launching, seeding, verifying, and debugging the Docker demo stack |
| `koinon-feature-slice` | The step-by-step checklist for implementing a feature across all layers |
| `koinon-e2e` | Writing/running Playwright browser tests, the smoke tier, and the print-bridge mocking doctrine for kiosk check-in |

They work together with:

- **`koinon-index` MCP server** (`tools/mcp-servers/codebase-index/`, registered
  in `.mcp.json`) — live structural index: `get_conventions`, `trace_feature`,
  `search_index`, `list_endpoints`. Zero dependencies; needs only Node ≥ 18.
- **`chief-architect` agent** (`.claude/agents/chief-architect.md`) — binding
  rulings on anything without precedent; ADRs in `docs/adr/`.
- **`docs/reference/conventions.md`** — the single source of truth all three
  reference. Change it only via ADR.

## Using these skills on machines working on OTHER projects

The skills are Koinon-specific by design. To reuse the *approach* elsewhere,
copy the trio (conventions doc + index server + skills) and recalibrate:
the index server's `SCAN_SPECS`/parsers in
`tools/mcp-servers/codebase-index/server.js` are ~100 lines of regex tuned to
this codebase's patterns — point them at the new project's layout, rewrite
`conventions.md` for that project's canon, and adjust the skill checklists.
