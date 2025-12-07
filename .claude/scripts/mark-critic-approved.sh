#!/bin/bash
# Mark code-critic approval with evidence
# Called by code-critic agent after APPROVED verdict
# Requires summary of review as argument
#
# SECURITY: This script can ONLY be called from within the code-critic agent.
# The PM cannot directly approve code changes - they must spawn the code-critic.

set -euo pipefail

PROJECT_ROOT="$(cd "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/../.." && pwd)"
APPROVAL_FILE="$PROJECT_ROOT/.claude/.code-critic-approved"
REVIEW_LOG="$PROJECT_ROOT/.claude/.code-critic-reviews.log"
AGENT_FILE="$PROJECT_ROOT/.claude/.current-agent"

# Colors
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m'

cd "$PROJECT_ROOT"

# ============================================================================
# SECURITY CHECK: Only code-critic agent can call this script
# ============================================================================

if [ ! -f "$AGENT_FILE" ]; then
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    echo -e "${RED}❌ BLOCKED: Only code-critic agent can approve code${NC}"
    echo "═══════════════════════════════════════════════════════════"
    echo ""
    echo "You cannot directly call mark-critic-approved.sh"
    echo ""
    echo "The code-critic agent must review your changes:"
    echo "  Task(agent='code-critic', prompt='Review all staged changes')"
    echo ""
    echo "The code-critic will call this script after completing review."
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    exit 2
fi

CURRENT_AGENT=$(cat "$AGENT_FILE" 2>/dev/null | head -1 || echo "")

if [[ "$CURRENT_AGENT" != *"code-critic"* ]]; then
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    echo -e "${RED}❌ BLOCKED: Only code-critic agent can approve code${NC}"
    echo "═══════════════════════════════════════════════════════════"
    echo ""
    echo "Current agent: $CURRENT_AGENT"
    echo "Required agent: code-critic"
    echo ""
    echo "Only the code-critic agent can approve code changes."
    echo "Spawn the code-critic to review your changes:"
    echo "  Task(agent='code-critic', prompt='Review all staged changes')"
    echo ""
    echo "═══════════════════════════════════════════════════════════"
    exit 2
fi

echo -e "${GREEN}✓ Verified: Running within code-critic agent context${NC}"

# Require review summary as argument
if [ -z "${1:-}" ]; then
    echo ""
    echo -e "${RED}ERROR: Review summary required${NC}"
    echo ""
    echo "Usage: mark-critic-approved.sh \"X files reviewed, Y issues found, Z minutes spent\""
    echo ""
    echo "Example:"
    echo "  mark-critic-approved.sh \"15 files reviewed, 7 issues found and fixed, 25 minutes spent\""
    echo ""
    echo "The summary must include:"
    echo "  - Number of files reviewed"
    echo "  - Number of issues found (can be 0 only for tiny changesets)"
    echo "  - Approximate time spent"
    echo ""
    exit 1
fi

SUMMARY="$1"

# Validate summary contains required elements
if ! echo "$SUMMARY" | grep -qiE '[0-9]+\s*(files?|file)'; then
    echo -e "${RED}ERROR: Summary must include number of files reviewed${NC}"
    echo "Example: '15 files reviewed, ...'"
    exit 1
fi

if ! echo "$SUMMARY" | grep -qiE '[0-9]+\s*(issues?|issue|problems?|problem)'; then
    echo -e "${RED}ERROR: Summary must include number of issues found${NC}"
    echo "Example: '..., 7 issues found, ...'"
    exit 1
fi

# Extract numbers for validation
FILES_REVIEWED=$(echo "$SUMMARY" | grep -oiE '[0-9]+\s*(files?|file)' | grep -oE '[0-9]+' | head -1)
ISSUES_FOUND=$(echo "$SUMMARY" | grep -oiE '[0-9]+\s*(issues?|issue|problems?|problem)' | grep -oE '[0-9]+' | head -1)

# Sanity check: if many files but 0 issues, log warning but allow
# (The code-critic prompt requires automatic second-pass in this case)
if [ "$FILES_REVIEWED" -gt 10 ] && [ "$ISSUES_FOUND" -eq 0 ]; then
    echo ""
    echo -e "${YELLOW}NOTE: $FILES_REVIEWED files reviewed with 0 issues${NC}"
    echo "Ensure the code-critic performed the required second-pass review"
    echo "(architecture comparison + best practices lookup)"
    echo ""
fi

# Get current staged/changed files for reference
STAGED_FILES=$(git diff --cached --name-only 2>/dev/null | wc -l)
BRANCH_FILES=$(git diff --name-only main...HEAD 2>/dev/null | wc -l || echo "0")

# Create approval record
mkdir -p "$(dirname "$APPROVAL_FILE")"
TIMESTAMP=$(date -Iseconds)

cat > "$APPROVAL_FILE" << EOF
{
  "timestamp": "$TIMESTAMP",
  "summary": "$SUMMARY",
  "files_reviewed": $FILES_REVIEWED,
  "issues_found": $ISSUES_FOUND,
  "staged_files_at_approval": $STAGED_FILES,
  "branch_files_at_approval": $BRANCH_FILES
}
EOF

# Append to review log for audit trail
echo "[$TIMESTAMP] $SUMMARY (staged: $STAGED_FILES, branch: $BRANCH_FILES)" >> "$REVIEW_LOG"

echo ""
echo -e "${GREEN}Code-critic approval recorded${NC}"
echo ""
echo "Timestamp: $TIMESTAMP"
echo "Summary: $SUMMARY"
echo "Files in scope: staged=$STAGED_FILES, branch=$BRANCH_FILES"
echo ""
echo "Approval file: $APPROVAL_FILE"
echo "Review log: $REVIEW_LOG"
echo ""
