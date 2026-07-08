# MCP Servers

Project-local MCP server pieces. The authoritative reference for all
configured servers is `docs/claude/mcp-tools.md`; the lineup decision is
`docs/adr/0005-mcp-server-lineup.md`.

- `codebase-index/` — the `koinon-index` server: zero-dependency structural
  index (conventions, endpoints, feature traces). Runs directly via Node,
  no install step.

The `koinon-dev` validation server lives separately in `tools/mcp-koinon-dev/`.
