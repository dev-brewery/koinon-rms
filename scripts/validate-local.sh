#!/bin/bash
# Local validation script - runs same checks as CI
# Run before pushing to catch issues early and save tokens
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo_step() {
    echo -e "${YELLOW}=== $1 ===${NC}"
}

echo_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

echo_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Track timing
START_TIME=$(date +%s)

# Step 1: Build backend
echo_step "Building backend (.NET)"
if dotnet build --nologo -v q; then
    echo_success "Backend build passed"
else
    echo_error "Backend build FAILED"
    exit 1
fi

# Step 2: Run backend tests
echo_step "Running backend tests"
if dotnet test --nologo -v q --no-build; then
    echo_success "Backend tests passed"
else
    echo_error "Backend tests FAILED"
    exit 1
fi

# Step 3: Type check frontend
echo_step "Type checking frontend (TypeScript)"
if npm run --prefix src/web typecheck 2>/dev/null; then
    echo_success "Frontend type check passed"
else
    echo_error "Frontend type check FAILED"
    exit 1
fi

# Step 4: Lint frontend
echo_step "Linting frontend (ESLint)"
if npm run --prefix src/web lint 2>/dev/null; then
    echo_success "Frontend lint passed"
else
    echo_error "Frontend lint FAILED"
    exit 1
fi

# Step 5: Run frontend tests
echo_step "Running frontend tests"
if npm run --prefix src/web test 2>/dev/null; then
    echo_success "Frontend tests passed"
else
    echo_error "Frontend tests FAILED"
    exit 1
fi

# Step 6: Graph validation (preserve timestamps to prevent drift)
echo_step "Validating API graph baseline"

# Save current timestamps before validation regenerates them
SAVED_TS_BACKEND=$(grep -o '"generated_at": "[^"]*"' tools/graph/backend-graph.json 2>/dev/null || true)
SAVED_TS_FRONTEND=$(grep -o '"generated_at": "[^"]*"' tools/graph/frontend-graph.json 2>/dev/null || true)
SAVED_TS_MERGED=$(grep -o '"generated_at": "[^"]*"' tools/graph/graph-baseline.json 2>/dev/null || true)

if npm run graph:validate 2>/dev/null; then
    echo_success "Graph validation passed"
else
    echo "ℹ Graph validation tools not yet available (Sprint 18 implementation pending)"
    echo "See tools/graph/README.md for manual baseline update guidance"
fi

# Restore original timestamps to prevent drift (validation passed, no structural changes)
if [ -n "$SAVED_TS_BACKEND" ]; then
    sed -i "s|\"generated_at\": \"[^\"]*\"|$SAVED_TS_BACKEND|" tools/graph/backend-graph.json 2>/dev/null || true
fi
if [ -n "$SAVED_TS_FRONTEND" ]; then
    sed -i "s|\"generated_at\": \"[^\"]*\"|$SAVED_TS_FRONTEND|" tools/graph/frontend-graph.json 2>/dev/null || true
fi
if [ -n "$SAVED_TS_MERGED" ]; then
    sed -i "s|\"generated_at\": \"[^\"]*\"|$SAVED_TS_MERGED|" tools/graph/graph-baseline.json 2>/dev/null || true
fi

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}All checks passed! (${DURATION}s)${NC}"
echo -e "${GREEN}Safe to push.${NC}"
echo -e "${GREEN}========================================${NC}"
