#!/bin/bash
# Quick validation for staged files only
# Faster than full validation - use for pre-commit
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

cd "$PROJECT_ROOT"
export PATH="$PATH:$HOME/.dotnet"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo_step() {
    echo -e "${YELLOW}$1${NC}"
}

echo_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

echo_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Get staged files
STAGED_CS=$(git diff --cached --name-only --diff-filter=ACM | grep '\.cs$' || true)
# Only check frontend TypeScript files (in src/web/) - tools/ has its own build process
STAGED_TS=$(git diff --cached --name-only --diff-filter=ACM | grep -E '^src/web/.*\.(ts|tsx)$' || true)

HAS_BACKEND_CHANGES=false
HAS_FRONTEND_CHANGES=false

if [ -n "$STAGED_CS" ]; then
    HAS_BACKEND_CHANGES=true
fi

if [ -n "$STAGED_TS" ]; then
    HAS_FRONTEND_CHANGES=true
fi

# Skip if no relevant changes
if [ "$HAS_BACKEND_CHANGES" = false ] && [ "$HAS_FRONTEND_CHANGES" = false ]; then
    echo_success "No C#/TypeScript changes staged - skipping validation"
    exit 0
fi

START_TIME=$(date +%s)

# Backend checks (if .cs files changed)
if [ "$HAS_BACKEND_CHANGES" = true ]; then
    echo_step "Building backend (staged .cs files detected)..."
    if dotnet build --nologo -v q 2>/dev/null; then
        echo_success "Backend build passed"
    else
        echo_error "Backend build FAILED"
        exit 1
    fi
fi

# Frontend checks (if .ts/.tsx files changed)
if [ "$HAS_FRONTEND_CHANGES" = true ]; then
    echo_step "Type checking frontend (staged .ts/.tsx files detected)..."
    if npm run --prefix src/web typecheck 2>/dev/null; then
        echo_success "Frontend type check passed"
    else
        echo_error "Frontend type check FAILED"
        exit 1
    fi
    
    echo_step "Linting staged frontend files..."
    # Lint only staged files in src/web/ for speed
    STAGED_TS_FULL=$(git diff --cached --name-only --diff-filter=ACM | grep -E '^src/web/.*\.(ts|tsx)$' || true)
    if [ -n "$STAGED_TS_FULL" ]; then
        if echo "$STAGED_TS_FULL" | xargs npx --prefix src/web eslint --max-warnings 0 2>/dev/null; then
            echo_success "Frontend lint passed"
        else
            echo_error "Frontend lint FAILED"
            exit 1
        fi
    fi
fi

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

echo_success "Quick validation passed (${DURATION}s)"
