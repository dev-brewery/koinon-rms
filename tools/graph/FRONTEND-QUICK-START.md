# Frontend Graph Generator - Quick Start

## TL;DR

Generate a complete map of your React frontend in one command:

```bash
node tools/graph/generate-frontend.js
```

Output: `tools/graph/frontend-graph.json` (336 architectural nodes)

## What You Get

A JSON file with:
- **109 types** - All TypeScript interfaces, types, and enums
- **69 API functions** - HTTP client wrappers
- **71 hooks** - React hooks using TanStack Query
- **87 components** - React components with hook usage
- **25 edges** - Relationships between these elements

## Quick Queries

```bash
# List all API endpoints
jq '.api_functions[] | {name, method, endpoint}' tools/graph/frontend-graph.json

# Find components using a hook
jq '.components | with_entries[] | select(.value.hooksUsed | contains(["useFamilies"])) | .key' tools/graph/frontend-graph.json

# Check which hook calls which API
jq '.hooks[] | {name, apiBinding}' tools/graph/frontend-graph.json | head -20

# Count nodes by type
jq '{types: (.types | length), apis: (.api_functions | length), hooks: (.hooks | length), components: (.components | length), edges: (.edges | length)}' tools/graph/frontend-graph.json
```

## File Locations

| File | Purpose |
|------|---------|
| `tools/graph/generate-frontend.js` | Generator script |
| `tools/graph/frontend-graph.json` | Generated output |
| `tools/graph/FRONTEND-GENERATOR.md` | Full documentation |
| `tools/graph/FRONTEND-GRAPH-STATS.md` | Architecture analysis |
| `tools/graph/IMPLEMENTATION-SUMMARY.md` | Technical details |

## How It Works

1. Reads TypeScript source files
2. Uses regex to extract types, functions, hooks, components
3. Builds relationship graph
4. Outputs JSON for analysis

**No external dependencies. Runs instantly from project root.**

## Architecture Pattern

```
Component
    ↓ (uses)
  Hook
    ↓ (binds to)
API Function
    ↓ (returns)
  Type/DTO
```

## Common Tasks

### Add new API function?
Generator auto-detects it next time you run.

### Add new hook?
Generator auto-detects it next time you run.

### Add new component?
Generator auto-detects it next time you run.

### Check what changed?
```bash
git diff tools/graph/frontend-graph.json
```

### Validate against backend?
Compare `types` in frontend graph with backend DTOs in `backend-graph.json`.

## Known Limitations

- Uses regex parsing (not full AST)
- Component prop types not extracted
- Circular dependencies not detected
- Complex generics may not parse perfectly

But it works great for our current frontend structure!

## Next Steps

1. **Regenerate** after major frontend changes
2. **Compare** with backend graph for type alignment
3. **Visualize** components and their dependencies
4. **Validate** API contracts between frontend and backend
5. **Automate** in CI/CD pipeline

## Help

Full docs: `tools/graph/FRONTEND-GENERATOR.md`

