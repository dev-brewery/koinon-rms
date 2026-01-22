#!/bin/bash
# RAG Integration Script
# Applies all changes from docs/rag-integration-manual-changes.md to .claude/ files
# Run this script as the human user (not via Claude agent)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
CLAUDE_DIR="$PROJECT_ROOT/.claude"

echo "=== RAG Integration Script ==="
echo "Project root: $PROJECT_ROOT"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

success() { echo -e "${GREEN}✓${NC} $1"; }
warn() { echo -e "${YELLOW}⚠${NC} $1"; }
error() { echo -e "${RED}✗${NC} $1"; }

# Backup function
backup_file() {
    local file="$1"
    if [[ -f "$file" ]]; then
        cp "$file" "${file}.bak.$(date +%Y%m%d%H%M%S)"
        echo "  Backed up: $file"
    fi
}

# ============================================================================
# 1. Update Plan.md - Add RAG Discovery Section
# ============================================================================
echo ""
echo "1. Updating Plan.md..."

PLAN_FILE="$CLAUDE_DIR/agents/Plan.md"
if [[ -f "$PLAN_FILE" ]]; then
    backup_file "$PLAN_FILE"

    # Check if RAG section already exists
    if grep -q "RAG-Powered Discovery" "$PLAN_FILE"; then
        warn "RAG section already exists in Plan.md, skipping"
    else
        # Create the RAG section to insert
        RAG_SECTION='## RAG-Powered Discovery (USE FIRST)

Before grep/glob exploration, query RAG for semantic matches. This is faster and more token-efficient.

### Find Similar Implementations
```python
# Find entities similar to what you are building
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
```

### Impact Analysis Before Planning
```python
# Check what will be affected by changes
mcp__koinon-dev__rag_impact_analysis(
    file_path="src/Koinon.Domain/Entities/Person.cs",
    change_description="adding new address field",
    include_tests=True
)
```

### Check RAG Health (if results are empty)
```python
mcp__koinon-dev__rag_index_status()
```

### Discovery Order
1. **RAG semantic search** (broad understanding, token-efficient)
2. **Graph query** via `mcp__koinon-dev__query_api_graph` (structural relationships)
3. **Targeted grep/glob** (specific patterns when needed)

### Graceful Degradation
If RAG tools return warnings about unavailability, fall back to grep/glob exploration without blocking. RAG is a helper, not a gate.

'
        # Insert before "## Process" section if it exists, otherwise append
        if grep -q "^## Process" "$PLAN_FILE"; then
            # Use awk to insert before ## Process
            awk -v section="$RAG_SECTION" '
                /^## Process/ { print section }
                { print }
            ' "$PLAN_FILE" > "${PLAN_FILE}.tmp" && mv "${PLAN_FILE}.tmp" "$PLAN_FILE"
        else
            # Append to end
            echo "$RAG_SECTION" >> "$PLAN_FILE"
        fi
        success "Updated Plan.md with RAG discovery section"
    fi
else
    error "Plan.md not found at $PLAN_FILE"
fi

# ============================================================================
# 2. Update Dev Agent Prompts - Add Pattern Discovery
# ============================================================================
echo ""
echo "2. Updating dev agent prompts..."

# Function to add pattern discovery to an agent file
add_pattern_discovery() {
    local file="$1"
    local layer="$2"
    local type="$3"
    local query_example="$4"
    local fallback_doc="$5"

    if [[ ! -f "$file" ]]; then
        warn "File not found: $file"
        return
    fi

    if grep -q "Pattern Discovery" "$file"; then
        warn "Pattern Discovery already exists in $(basename "$file"), skipping"
        return
    fi

    backup_file "$file"

    local section="
## Pattern Discovery (Before Implementation)

Find similar patterns before implementing:

\`\`\`python
mcp__koinon-dev__rag_search(
    query=\"$query_example\",
    filter_layer=\"$layer\",
    filter_type=\"$type\",
    limit=5
)
\`\`\`

If RAG unavailable, proceed with documented patterns${fallback_doc}.
"

    # Append the section after the first heading block
    # Find first ## heading after file start and insert after it
    if grep -q "^## " "$file"; then
        # Insert after first major section heading
        awk -v section="$section" '
            BEGIN { inserted=0; found_first=0 }
            /^## / {
                if (!found_first) {
                    found_first=1
                    print
                    next
                }
                if (!inserted && found_first) {
                    print section
                    inserted=1
                }
            }
            { print }
            END { if (!inserted) print section }
        ' "$file" > "${file}.tmp" && mv "${file}.tmp" "$file"
    else
        echo "$section" >> "$file"
    fi

    success "Updated $(basename "$file")"
}

# Apply to each dev agent
add_pattern_discovery "$CLAUDE_DIR/agents/entity.md" \
    "Domain" "Entity" \
    "entity with similar fields and relationships" \
    " in entity-mappings.md"

add_pattern_discovery "$CLAUDE_DIR/agents/data-layer.md" \
    "Infrastructure" "Other" \
    "EF Core configuration for entity with relationships" \
    ""

add_pattern_discovery "$CLAUDE_DIR/agents/core-services.md" \
    "Application" "Service" \
    "service with CRUD operations and validation" \
    ""

add_pattern_discovery "$CLAUDE_DIR/agents/api-controllers.md" \
    "API" "Controller" \
    "REST controller with CRUD endpoints" \
    " in api-contracts.md"

add_pattern_discovery "$CLAUDE_DIR/agents/ui-components.md" \
    "Frontend" "Component" \
    "React component with data fetching and forms" \
    ""

# ============================================================================
# 3. Update code-critic.md - Add RAG Validation Step
# ============================================================================
echo ""
echo "3. Updating code-critic.md..."

CRITIC_FILE="$CLAUDE_DIR/agents/code-critic.md"
if [[ -f "$CRITIC_FILE" ]]; then
    if grep -q "RAG Semantic Validation" "$CRITIC_FILE"; then
        warn "RAG validation section already exists in code-critic.md, skipping"
    else
        backup_file "$CRITIC_FILE"

        RAG_VALIDATION='
### Step 5: RAG Semantic Validation

After file-by-file review, run semantic validators:

```bash
python3 tools/rag/validate.py
```

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

```
## RAG SEMANTIC VIOLATIONS
- **src/Koinon.Api/Controllers/PersonController.cs** - Business logic in controller
  | Fix: Move loop and calculations to PersonService
- **src/web/src/components/PersonList.tsx** - Direct API call
  | Fix: Use usePeople hook instead of fetch()

VERDICT: CHANGES REQUESTED
```

### Graceful Degradation

If RAG validators fail to run (Qdrant/Ollama unavailable):
1. Log warning: "RAG validation skipped - infrastructure unavailable"
2. Continue with structural review only
3. Do NOT block on RAG unavailability
'

        # Append to end of file
        echo "$RAG_VALIDATION" >> "$CRITIC_FILE"
        success "Updated code-critic.md with RAG validation step"
    fi
else
    error "code-critic.md not found at $CRITIC_FILE"
fi

# ============================================================================
# 4. Create post-commit-reindex.sh hook
# ============================================================================
echo ""
echo "4. Creating post-commit-reindex.sh hook..."

HOOK_FILE="$CLAUDE_DIR/hooks/post-commit-reindex.sh"
if [[ -f "$HOOK_FILE" ]]; then
    warn "post-commit-reindex.sh already exists, skipping"
else
    cat > "$HOOK_FILE" << 'HOOK_EOF'
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
HOOK_EOF
    chmod +x "$HOOK_FILE"
    success "Created post-commit-reindex.sh"
fi

# ============================================================================
# 5. Create check-rag-health.sh script
# ============================================================================
echo ""
echo "5. Creating check-rag-health.sh script..."

HEALTH_FILE="$CLAUDE_DIR/scripts/check-rag-health.sh"
if [[ -f "$HEALTH_FILE" ]]; then
    warn "check-rag-health.sh already exists, skipping"
else
    cat > "$HEALTH_FILE" << 'HEALTH_EOF'
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
HEALTH_EOF
    chmod +x "$HEALTH_FILE"
    success "Created check-rag-health.sh"
fi

# ============================================================================
# 6. Create 13-rag-discovery.md rule
# ============================================================================
echo ""
echo "6. Creating 13-rag-discovery.md rule..."

RULE_FILE="$CLAUDE_DIR/rules/13-rag-discovery.md"
if [[ -f "$RULE_FILE" ]]; then
    warn "13-rag-discovery.md already exists, skipping"
else
    cat > "$RULE_FILE" << 'RULE_EOF'
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

```python
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
```

## Fallback
If RAG returns errors or empty results with warnings, proceed with grep/glob without blocking.

## Bypass
None needed - RAG is advisory, not blocking.
RULE_EOF
    success "Created 13-rag-discovery.md"
fi

# ============================================================================
# Summary
# ============================================================================
echo ""
echo "=== Summary ==="
echo ""
echo "Files modified/created:"
find "$CLAUDE_DIR" -name "*.bak.*" -newer "$0" 2>/dev/null | while read f; do
    echo "  (backup) $f"
done

echo ""
echo "Changes applied:"
echo "  1. Plan.md - RAG discovery section"
echo "  2. entity.md - Pattern discovery"
echo "  3. data-layer.md - Pattern discovery"
echo "  4. core-services.md - Pattern discovery"
echo "  5. api-controllers.md - Pattern discovery"
echo "  6. ui-components.md - Pattern discovery"
echo "  7. code-critic.md - RAG validation step"
echo "  8. post-commit-reindex.sh - Auto-reindex hook"
echo "  9. check-rag-health.sh - Health check script"
echo "  10. 13-rag-discovery.md - RAG usage rule"
echo ""
echo -e "${GREEN}Done!${NC} Restart Claude Code to pick up changes."
echo ""
echo "Test with:"
echo "  mcp__koinon-dev__rag_index_status()"
echo "  mcp__koinon-dev__rag_search(query=\"person entity\", limit=3)"
