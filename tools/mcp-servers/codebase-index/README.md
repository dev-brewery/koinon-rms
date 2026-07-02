# koinon-index MCP server

Zero-dependency MCP server that keeps a structural index of the Koinon RMS
codebase so agents understand conventions and wiring instead of grepping for
strings and guessing.

## Why this exists

Agents doing development work were sampling the codebase with grep, inferring
conventions from whatever files the search happened to hit, and then
implementing "a" pattern rather than "the" pattern. This server gives them:

- **`get_conventions`** — the canonical conventions doc
  (`docs/reference/conventions.md`), verbatim. The agreed rules, not a guess.
- **`trace_feature`** — the full wiring of an existing feature across layers:
  frontend route → page → hooks → api service functions → API endpoints →
  controller → application services → entities/DTOs. Run this before adding
  anything; copy the wiring of the nearest existing feature.
- **`search_index`** — typed lookup (endpoint / controller / appService /
  appInterface / dto / entity / validator / apiFunction / hook / page / route)
  with file:line.
- **`list_endpoints`** — every API endpoint with method, route,
  controller.action, and auth status.
- **`get_stats`**, **`reindex`** — coverage sanity check and manual rebuild.

## How the index stays fresh

The full index builds at server start (~1s for ~600 files) and every tool call
does a cheap mtime staleness check over the indexed file set — any edit,
addition, or deletion triggers an automatic rebuild. No daemon, no watcher,
no external database.

## Portability

- **No npm install.** The MCP stdio transport (newline-delimited JSON-RPC 2.0)
  is implemented in `server.js` directly. Requires only Node >= 18.
- The repo root is resolved relative to `server.js`, so the working directory
  doesn't matter.
- Registered in the project-scoped `.mcp.json` with a relative path — cloning
  the repo on any machine is the entire setup.

## Testing by hand

```bash
printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"t","version":"0"}}}' \
  '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"trace_feature","arguments":{"term":"families"}}}' \
  | node tools/mcp-servers/codebase-index/server.js
```

## Extending

Index coverage is defined by `SCAN_SPECS` at the top of `server.js` (directory,
extension, parser kind). Parsers are regex-based and calibrated to this
codebase's patterns (primary constructors, `[Route("api/v1/[controller]")]`,
`export async function` service modules, `export function useX` hooks,
`<Route path element>` in App.tsx). If a convention changes shape, update the
parser in the same PR — `get_stats` dropping a category to zero is the smoke
signal.
