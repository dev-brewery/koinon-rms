# RAG Integration - Manual Changes Required

The following changes need to be manually applied because the files are in `.claude/` which is protected (gitignored + hook-protected per Rule 10).

## Summary

| File | Change Type | Priority |
|------|-------------|----------|
| `.claude/agents/Plan.md` | Add RAG discovery section | HIGH |
| `.claude/agents/entity.md` | Add pattern discovery | MEDIUM |
| `.claude/agents/data-layer.md` | Add pattern discovery | MEDIUM |
| `.claude/agents/core-services.md` | Add pattern discovery | MEDIUM |
| `.claude/agents/api-controllers.md` | Add pattern discovery | MEDIUM |
| `.claude/agents/ui-components.md` | Add pattern discovery | MEDIUM |
| `.claude/agents/code-critic.md` | Add RAG validation step | HIGH |
| `.claude/hooks/post-commit-reindex.sh` | Create new hook | MEDIUM |
| `.claude/scripts/check-rag-health.sh` | Create new script | LOW |
| `.claude/rules/13-rag-discovery.md` | Create new rule | LOW |

---

## 1. Plan.md - Add RAG Discovery Section

**File:** `.claude/agents/Plan.md`

Add this section BEFORE the "## Process" section:

```markdown
## RAG-Powered Discovery (USE FIRST)

Before grep/glob exploration, query RAG for semantic matches. This is faster and more token-efficient.

### Find Similar Implementations
\`\`\`python
# Find entities similar to what you're building
mcp__koinon-dev__rag_search(
    query="entity with audit fields and family relationship",
    filter_layer="Domain",
    limit=5
)

# Find how similar features were implemented
mcp__koinon-dev__rag_search(
    query="person CRUD operations with validation",
    filter_layer="Application",
    limit=10
)
\`\`\`

### Impact Analysis Before Planning
\`\`\`python
# Check what will be affected by changes
mcp__koinon-dev__rag_impact_analysis(
    file_path="src/Koinon.Domain/Entities/Person.cs",
    change_description="adding new address field",
    include_tests=True
)
\`\`\`

### Check RAG Health (if results are empty)
\`\`\`python
mcp__koinon-dev__rag_index_status()
\`\`\`

### Discovery Order
1. **RAG semantic search** (broad understanding, token-efficient)
2. **Graph query** via `mcp__koinon-dev__query_api_graph` (structural relationships)
3. **Targeted grep/glob** (specific patterns when needed)

### Graceful Degradation
If RAG tools return warnings about unavailability, fall back to grep/glob exploration without blocking. RAG is a helper, not a gate.
```

Also update the "## Process" section to include RAG steps:

```markdown
## Process

1. **Read the issue** - Understand requirements and acceptance criteria
2. **Explore with RAG** - Use `rag_search` to find similar patterns and related code
3. **Check impact** - Use `rag_impact_analysis` to understand cross-layer implications
4. **Explore the codebase** - Use grep/glob for specific patterns not found via RAG
5. **Identify layers** - Domain → Application → Infrastructure → Api → Web
6. **Sequence steps** - Order by dependencies (entities before repos, repos before services)
7. **Output JSON plan** - Structured, parseable by PM
8. **TERMINATE** - Your job is done
```

---

## 2. Dev Agent Prompts - Add Pattern Discovery

**Files to update:**
- `.claude/agents/entity.md`
- `.claude/agents/data-layer.md`
- `.claude/agents/core-services.md`
- `.claude/agents/api-controllers.md`
- `.claude/agents/ui-components.md`

Add this section to each (customize `filter_layer` and `filter_type` per agent):

### For entity.md:
```markdown
## Pattern Discovery (Before Implementation)

Find similar entity patterns before implementing:

\`\`\`python
mcp__koinon-dev__rag_search(
    query="entity with similar fields and relationships",
    filter_layer="Domain",
    filter_type="Entity",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns in entity-mappings.md.
```

### For data-layer.md:
```markdown
## Pattern Discovery (Before Implementation)

Find similar repository and configuration patterns:

\`\`\`python
mcp__koinon-dev__rag_search(
    query="EF Core configuration for entity with relationships",
    filter_layer="Infrastructure",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns.
```

### For core-services.md:
```markdown
## Pattern Discovery (Before Implementation)

Find similar service and DTO patterns:

\`\`\`python
mcp__koinon-dev__rag_search(
    query="service with CRUD operations and validation",
    filter_layer="Application",
    filter_type="Service",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns.
```

### For api-controllers.md:
```markdown
## Pattern Discovery (Before Implementation)

Find similar controller patterns:

\`\`\`python
mcp__koinon-dev__rag_search(
    query="REST controller with CRUD endpoints",
    filter_layer="API",
    filter_type="Controller",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns in api-contracts.md.
```

### For ui-components.md:
```markdown
## Pattern Discovery (Before Implementation)

Find similar React component patterns:

\`\`\`python
mcp__koinon-dev__rag_search(
    query="React component with data fetching and forms",
    filter_layer="Frontend",
    filter_type="Component",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns.
```

---

## 3. code-critic.md - Add RAG Validation Step

**File:** `.claude/agents/code-critic.md`

Add this section after the file-by-file review step (typically after "Step 4: Hunt for Problems"):

```markdown
### Step 5: RAG Semantic Validation

After file-by-file review, run semantic validators:

\`\`\`bash
python3 tools/rag/validate.py
\`\`\`

### What RAG Validators Detect (regex cannot)

| Validator | Detection | Severity |
|-----------|-----------|----------|
| `validate_no_business_logic_in_controllers` | Loops, calculations, complex conditionals in controllers | HIGH |
| `validate_no_direct_api_calls_in_components` | fetch/axios in React components | MEDIUM |
| `detect_n_plus_one_queries` | Loops with DB queries inside | HIGH |
| `detect_missing_async` | Sync EF Core calls (.ToList(), .First()) | MEDIUM |
| `validate_dto_coverage` | Entities without corresponding DTOs | MEDIUM |
| `validate_controller_uses_services` | Repository injection in controllers | HIGH |

### Handling RAG Validation Failures

If `tools/rag/validate.py` returns violations (exit code 2):

\`\`\`
## RAG SEMANTIC VIOLATIONS
- **src/Koinon.Api/Controllers/PersonController.cs** - Business logic in controller
  | Fix: Move loop and calculations to PersonService
- **src/web/src/components/PersonList.tsx** - Direct API call
  | Fix: Use usePeople hook instead of fetch()

VERDICT: CHANGES REQUESTED
\`\`\`

### Graceful Degradation

If RAG validators fail to run (Qdrant/Ollama unavailable):
1. Log warning: "RAG validation skipped - infrastructure unavailable"
2. Continue with structural review only
3. Do NOT block on RAG unavailability
```

---

## 4. post-commit-reindex.sh - Create New Hook

**File:** `.claude/hooks/post-commit-reindex.sh`

Create this file:

```bash
#!/bin/bash
# Post-commit: Incrementally reindex changed files for RAG
# This hook runs after successful commits to keep the RAG index fresh.

set -euo pipefail

# Check if RAG infrastructure is available
if ! curl -s http://localhost:6333/health >/dev/null 2>&1; then
    echo "RAG: Qdrant unavailable, skipping reindex"
    exit 0
fi

if ! curl -s http://localhost:11434/api/tags >/dev/null 2>&1; then
    echo "RAG: Ollama unavailable, skipping reindex"
    exit 0
fi

# Run incremental reindex
echo "RAG: Reindexing changed files..."
cd /home/mbrewer/projects/koinon-rms
python3 tools/rag/reindex-changes.py --quiet 2>/dev/null || true

exit 0  # Never block
```

Make it executable: `chmod +x .claude/hooks/post-commit-reindex.sh`

---

## 5. check-rag-health.sh - Create New Script

**File:** `.claude/scripts/check-rag-health.sh`

Create this file:

```bash
#!/bin/bash
# Quick health check for RAG infrastructure

echo "=== RAG Infrastructure Health ==="

echo -n "Qdrant: "
if curl -s http://localhost:6333/health | jq -r '.status' 2>/dev/null; then
    :
else
    echo "UNAVAILABLE"
fi

echo -n "Ollama: "
if curl -s http://localhost:11434/api/tags | jq -r '.models[0].name' 2>/dev/null; then
    :
else
    echo "UNAVAILABLE"
fi

echo -n "Index: "
python3 -c "
from qdrant_client import QdrantClient
try:
    client = QdrantClient(url='http://localhost:6333')
    info = client.get_collection('koinon-code')
    print(f'{info.points_count} chunks')
except Exception as e:
    print(f'ERROR: {e}')
" 2>/dev/null || echo "ERROR"

echo ""
echo "=== Quick Test ==="
echo "Try: mcp__koinon-dev__rag_index_status()"
```

Make it executable: `chmod +x .claude/scripts/check-rag-health.sh`

---

## 6. 13-rag-discovery.md - Create New Rule

**File:** `.claude/rules/13-rag-discovery.md`

Create this file:

```markdown
# Rule 13: RAG Discovery (RECOMMENDED)

## Requirement
Agents SHOULD use RAG search before grep/glob for code discovery.

## Enforcement
- **Not blocking** - RAG unavailability never stops work
- Advisory warnings only
- `post-commit-reindex` hook keeps index fresh

## Why RAG First?

| Discovery Method | Tokens | Accuracy |
|------------------|--------|----------|
| grep/glob (multiple) | 5,000-15,000 | Pattern-match only |
| RAG search | 300-600 | Semantic understanding |

RAG finds:
- Semantically related code (not just text matches)
- Patterns across layers
- Tests that need updating
- Similar implementations to pattern-match

## Usage

\`\`\`python
# Find similar patterns
mcp__koinon-dev__rag_search(
    query="person validation with email",
    filter_layer="Application"
)

# Check impact before changes
mcp__koinon-dev__rag_impact_analysis(
    file_path="src/Koinon.Domain/Entities/Person.cs",
    change_description="adding new field"
)

# Check health
mcp__koinon-dev__rag_index_status()
\`\`\`

## Fallback
If RAG returns errors or empty results with warnings, proceed with grep/glob without blocking.

## Bypass
None needed - RAG is advisory, not blocking.
```

---

## How to Apply These Changes

1. Open each file listed above
2. Copy the content from this document
3. Paste into the appropriate location in each file
4. Save the files

These files are protected because they're gitignored infrastructure. Only the human user can modify them directly.

## After Applying Changes

1. Rebuild the koinon-dev MCP server:
   ```bash
   cd tools/mcp-koinon-dev && npm run build
   ```

2. Restart Claude Code to pick up the new MCP tools

3. Test the RAG tools:
   ```python
   mcp__koinon-dev__rag_index_status()
   mcp__koinon-dev__rag_search(query="person entity", limit=3)
   ```
