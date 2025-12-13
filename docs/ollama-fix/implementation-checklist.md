# Ollama Integration Implementation Checklist

## Pre-Implementation Verification

### 1. Ollama Server Status
- [ ] Verify Ollama is running: `curl http://localhost:11434/api/tags`
- [ ] Verify model is available: Check for `qwen2.5-coder:7b` in tags
- [ ] Test basic generation: `curl -X POST http://localhost:11434/api/generate -d '{"model":"qwen2.5-coder:7b","prompt":"test","stream":false}'`

### 2. MCP Server Status
- [ ] Check MCP server exists: `ls /mnt/g/repos/wsl-mcp-ollama/dist/index.js`
- [ ] Test MCP wrapper: `/home/mbrewer/.claude/scripts/ollama-mcp.sh`
- [ ] Verify MCP tools available in Claude Code: Check for `mcp__ollama__*` tools
- [ ] **BLOCKER**: If no tools available, proceed with direct API calls (Phase 2)

### 3. Quality Baseline
- [ ] Generate 5 test commit messages
- [ ] Generate 2 test PR descriptions
- [ ] Generate 1 CI error summary
- [ ] Manual review: Acceptable quality? (Y/N)

## Implementation Phases

### Phase 1: Create Helper Scripts Directory

```bash
mkdir -p .claude/scripts/ollama
chmod +x .claude/scripts/ollama
```

**Scripts to Create**:
- [ ] `.claude/scripts/ollama/generate-commit-msg.sh`
- [ ] `.claude/scripts/ollama/format-pr-body.sh`
- [ ] `.claude/scripts/ollama/triage-ci-errors.sh`
- [ ] `.claude/scripts/ollama/format-tech-debt.sh`
- [ ] `.claude/scripts/ollama/generate-branch-name.sh`
- [ ] `.claude/scripts/ollama/common.sh` (shared functions)

### Phase 2: Implement Common Library

File: `.claude/scripts/ollama/common.sh`

```bash
#!/bin/bash
# Common functions for Ollama integration

OLLAMA_HOST="${OLLAMA_HOST:-http://localhost:11434}"
OLLAMA_MODEL="${OLLAMA_MODEL:-qwen2.5-coder:7b}"
OLLAMA_TIMEOUT="${OLLAMA_TIMEOUT:-30}"

call_ollama() {
    local prompt="$1"
    local temperature="${2:-0.3}"
    local max_tokens="${3:-200}"

    # Test if Ollama is available
    if ! curl -s --max-time 2 "$OLLAMA_HOST/api/tags" >/dev/null 2>&1; then
        echo "ERROR: Ollama not available" >&2
        return 1
    fi

    # Make the call
    curl -s --max-time "$OLLAMA_TIMEOUT" "$OLLAMA_HOST/api/generate" -d @- <<EOF | jq -r '.response'
{
    "model": "$OLLAMA_MODEL",
    "prompt": "$prompt",
    "stream": false,
    "options": {
        "temperature": $temperature,
        "num_predict": $max_tokens
    }
}
EOF
}

export -f call_ollama
```

**Checklist**:
- [ ] Create `common.sh`
- [ ] Test `call_ollama` function
- [ ] Add error handling for timeouts
- [ ] Add fallback mechanism

### Phase 3: Commit Message Generation

File: `.claude/scripts/ollama/generate-commit-msg.sh`

```bash
#!/bin/bash
# Generate commit message from staged changes

source "$(dirname "$0")/common.sh"

# Get staged diff
DIFF=$(git diff --cached --stat)
if [[ -z "$DIFF" ]]; then
    echo "No staged changes" >&2
    exit 1
fi

# Get full diff for context
FULL_DIFF=$(git diff --cached | head -100)

PROMPT="You are writing a git commit message. Follow conventional commits format.

Staged changes:
$DIFF

Diff preview:
$FULL_DIFF

Write a single-line commit message (max 72 chars):
<type>(<scope>): <description>

Types: feat, fix, refactor, test, docs, chore
Output ONLY the commit message, nothing else."

# Call Ollama
MSG=$(call_ollama "$PROMPT" 0.2 100)

# Validate output
if [[ ${#MSG} -gt 100 ]] || [[ -z "$MSG" ]]; then
    echo "ERROR: Invalid commit message from Ollama" >&2
    exit 1
fi

echo "$MSG"
```

**Checklist**:
- [ ] Create script
- [ ] Make executable: `chmod +x .claude/scripts/ollama/generate-commit-msg.sh`
- [ ] Test with current changes: `.claude/scripts/ollama/generate-commit-msg.sh`
- [ ] Validate output quality (10 test runs)
- [ ] Add to PM workflow documentation

### Phase 4: PR Description Formatting

File: `.claude/scripts/ollama/format-pr-body.sh`

```bash
#!/bin/bash
# Format PR description from issue context

source "$(dirname "$0")/common.sh"

ISSUE_NUM="$1"
if [[ -z "$ISSUE_NUM" ]]; then
    echo "Usage: $0 <issue-number>" >&2
    exit 1
fi

# Get issue data
ISSUE_JSON=$(gh issue view "$ISSUE_NUM" --json title,body,labels)
TITLE=$(echo "$ISSUE_JSON" | jq -r '.title')
BODY=$(echo "$ISSUE_JSON" | jq -r '.body')
LABELS=$(echo "$ISSUE_JSON" | jq -r '.labels[].name' | tr '\n' ' ')

# Get commit messages on current branch
COMMITS=$(git log main..HEAD --oneline)

PROMPT="Create a pull request description for issue #$ISSUE_NUM.

Issue Title: $TITLE
Labels: $LABELS

Issue Body:
$BODY

Commits on branch:
$COMMITS

Format as:
## Summary
[One sentence describing the change]

## Changes
- [Bullet point 1]
- [Bullet point 2]

## Testing
- [How to test]

## Closes
Closes #$ISSUE_NUM

Keep it concise and professional. Output ONLY the markdown, no extra commentary."

PR_BODY=$(call_ollama "$PROMPT" 0.5 500)

# Add footer
echo "$PR_BODY"
echo ""
echo "🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

**Checklist**:
- [ ] Create script
- [ ] Make executable
- [ ] Test with recent issue: `.claude/scripts/ollama/format-pr-body.sh 123`
- [ ] Validate markdown formatting
- [ ] Validate closes reference

### Phase 5: CI Error Triage

File: `.claude/scripts/ollama/triage-ci-errors.sh`

```bash
#!/bin/bash
# Triage CI errors and categorize

source "$(dirname "$0")/common.sh"

LOG_FILE="$1"
if [[ -z "$LOG_FILE" ]]; then
    # Read from stdin
    LOG_CONTENT=$(cat)
else
    LOG_CONTENT=$(cat "$LOG_FILE")
fi

# Limit log size
LOG_PREVIEW=$(echo "$LOG_CONTENT" | tail -200)

PROMPT="Analyze this CI/CD error log and provide a triage summary.

Log excerpt (last 200 lines):
$LOG_PREVIEW

Provide:
1. Error Category: [BUILD_FAILURE|TEST_FAILURE|LINT_ERROR|DEPLOYMENT_ERROR|INFRASTRUCTURE]
2. Primary Error: [One line description]
3. Likely Cause: [Brief analysis]
4. Files Affected: [List if identifiable]

Format as JSON:
{
  \"category\": \"...\",
  \"error\": \"...\",
  \"cause\": \"...\",
  \"files\": [...]
}

Output ONLY valid JSON."

call_ollama "$PROMPT" 0.3 300
```

**Checklist**:
- [ ] Create script
- [ ] Make executable
- [ ] Test with sample CI log
- [ ] Validate JSON output with `jq`
- [ ] Integrate with devops agent workflow

### Phase 6: Hook Integration

#### 6.1 Commit Message Hook

Modify `.claude/hooks/pre-bash` or create new hook:

```bash
# In pre-bash, detect git commit without -m flag
if [[ "$BASH_COMMAND" == "git commit" ]] && [[ ! "$BASH_COMMAND" =~ "-m" ]]; then
    # Generate message via Ollama
    MSG=$(.claude/scripts/ollama/generate-commit-msg.sh 2>/dev/null)
    if [[ $? -eq 0 ]] && [[ -n "$MSG" ]]; then
        echo "💡 Suggested commit message (from Ollama):"
        echo "   $MSG"
        echo ""
        echo "To use: git commit -m \"$MSG\""
    fi
fi
```

**Checklist**:
- [ ] Add Ollama suggestion to pre-bash hook
- [ ] Test: Stage changes and run `git status` (hook should trigger)
- [ ] Validate message quality
- [ ] Document in HOOKS.md

#### 6.2 PR Creation Hook

Modify `.claude/hooks/pre-create-pr` or PM workflow:

```bash
# Before gh pr create, suggest body
if [[ -f .claude/.current-issue ]]; then
    ISSUE_NUM=$(cat .claude/.current-issue)
    PR_BODY=$(.claude/scripts/ollama/format-pr-body.sh "$ISSUE_NUM" 2>/dev/null)
    if [[ $? -eq 0 ]] && [[ -n "$PR_BODY" ]]; then
        echo "$PR_BODY" > /tmp/pr-body-ollama.md
        echo "💡 Generated PR body at /tmp/pr-body-ollama.md"
    fi
fi
```

**Checklist**:
- [ ] Add Ollama PR body generation
- [ ] Test with current issue
- [ ] Validate output
- [ ] Update PM workflow to use generated body

### Phase 7: PM Command Updates

Update `.claude/commands/pm.md`:

```markdown
## COMMIT WORKFLOW (Updated)

# Option 1: Auto-generate via Ollama
MSG=$(.claude/scripts/ollama/generate-commit-msg.sh)
git commit -m "$MSG

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# Option 2: Manual message (fallback if Ollama fails)
git commit -m "..."

## PR CREATION (Updated)

# Generate PR body
BODY=$(.claude/scripts/ollama/format-pr-body.sh <issue-number>)

# Create PR with generated body
gh pr create --title "..." --body "$BODY"
```

**Checklist**:
- [ ] Update pm.md with Ollama workflow
- [ ] Add fallback instructions
- [ ] Document error handling
- [ ] Add examples

## Testing & Validation

### Quality Checks

Run these tests before considering implementation complete:

1. **Commit Messages** (20 samples)
   ```bash
   for i in {1..20}; do
       .claude/scripts/ollama/generate-commit-msg.sh
   done > ollama-commit-test.txt
   ```
   - [ ] All under 72 chars?
   - [ ] Follow conventional commits?
   - [ ] Accurate descriptions?
   - [ ] Acceptable quality (manual review)?

2. **PR Descriptions** (5 samples)
   ```bash
   for issue in 120 121 122 148 150; do
       .claude/scripts/ollama/format-pr-body.sh $issue > pr-$issue.md
   done
   ```
   - [ ] Valid markdown?
   - [ ] Includes all sections?
   - [ ] References issue correctly?
   - [ ] Professional tone?

3. **CI Triage** (3 samples)
   ```bash
   .claude/scripts/ollama/triage-ci-errors.sh < sample-error.log
   ```
   - [ ] Valid JSON output?
   - [ ] Correct categorization?
   - [ ] Useful analysis?

### Performance Benchmarks

- [ ] Commit message generation: < 5 seconds
- [ ] PR body generation: < 10 seconds
- [ ] CI triage: < 15 seconds
- [ ] Fallback on timeout: Works correctly

### Integration Tests

- [ ] Run full issue cycle using Ollama scripts
- [ ] Verify token usage reduction (check logs)
- [ ] No workflow disruption
- [ ] Fallback to Sonnet works when Ollama down

## Rollout Plan

### Week 1: Pilot
- [ ] Implement Phase 1-3 (scripts only, no hooks)
- [ ] Manual testing of all scripts
- [ ] Quality validation
- [ ] Performance benchmarking

### Week 2: Integration
- [ ] Add hooks (Phase 6)
- [ ] Update PM command (Phase 7)
- [ ] Run 2-3 issues using Ollama workflow
- [ ] Monitor quality and performance

### Week 3: Full Deployment
- [ ] Make Ollama scripts default in PM mode
- [ ] Add monitoring/logging
- [ ] Document results
- [ ] Measure actual token savings

## Success Metrics

Track these for 2 weeks post-implementation:

| Metric | Target | Actual |
|--------|--------|--------|
| Token reduction | >50% | ___ |
| Ollama call success rate | >95% | ___ |
| Commit msg quality (1-5) | >4.0 | ___ |
| PR description quality (1-5) | >4.0 | ___ |
| CI triage accuracy | >80% | ___ |
| Workflow disruption incidents | 0 | ___ |

## Rollback Plan

If quality/reliability issues arise:

1. Remove hooks immediately
2. Revert PM command changes
3. Keep scripts for manual use
4. Document issues
5. Plan improvements

**Rollback Trigger**:
- Quality score < 3.5 for 5 consecutive uses
- Success rate < 85% for any script
- Any workflow-blocking issue

## Appendix: Testing Ollama Now

Run these commands to test Ollama availability and quality:

```bash
# 1. Check if running
curl -s http://localhost:11434/api/tags | jq -r '.models[].name'

# 2. Test generation quality
curl -s http://localhost:11434/api/generate -d '{
  "model": "qwen2.5-coder:7b",
  "prompt": "Write a git commit message for: Added user authentication feature with JWT tokens",
  "stream": false,
  "options": {"temperature": 0.3, "num_predict": 50}
}' | jq -r '.response'

# 3. Test with actual diff
git diff --cached | head -50 > /tmp/test-diff.txt
curl -s http://localhost:11434/api/generate -d @- <<EOF | jq -r '.response'
{
  "model": "qwen2.5-coder:7b",
  "prompt": "Write a concise git commit message for:\n$(cat /tmp/test-diff.txt)",
  "stream": false,
  "options": {"temperature": 0.3}
}
EOF
```
