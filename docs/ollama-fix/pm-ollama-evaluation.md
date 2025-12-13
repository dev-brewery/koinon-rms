# PM Command Ollama Integration Evaluation

**Date:** 2025-12-12
**Evaluator:** Claude Sonnet 4.5
**Context:** Token optimization for autonomous `/pm` mode

## Executive Summary

The `/pm` command currently uses Claude Sonnet for ALL operations, including many text generation tasks that could be delegated to the local Ollama server (qwen2.5-coder:7b) for **significant token savings** (estimated 40-60% reduction).

## Current State Analysis

### Available AI Resources

| Resource | Model | Context | Cost | Use Case |
|----------|-------|---------|------|----------|
| **Claude Sonnet** | claude-sonnet-4-5 | 200K tokens | $$$$$ | Main PM orchestration |
| **Gemini Flash** | gemini-1.5-flash | 1M+ tokens | $ | Large-scale analysis |
| **Ollama** | qwen2.5-coder:7b | ~32K tokens | FREE | Text generation |

### Configuration Status

✅ **Gemini**: Fully configured, documented in `.claude/agents/gemini-context.md`
⚠️ **Ollama**: MCP configured at `.claude/.mcp.json:36-39` but NOT actively used
❌ **Integration**: No systematic delegation strategy in `/pm` workflow

### Ollama MCP Configuration

```json
{
  "ollama": {
    "command": "/home/mbrewer/.claude/scripts/ollama-mcp.sh",
    "args": []
  }
}
```

The wrapper script exists and connects to:
- Windows Node.js MCP server at `/mnt/g/repos/wsl-mcp-ollama/dist/index.js`
- Ollama API at `http://localhost:11434`

**Problem**: `ListMcpResourcesTool` returns empty array - MCP tools may not be registered or server not running.

## Token Burn Analysis: Current `/pm` Workflow

### High-Frequency Operations (Per Issue Cycle)

| Operation | Current | Tokens/Op | Frequency | Can Use Ollama? |
|-----------|---------|-----------|-----------|----------------|
| **Commit message generation** | Sonnet | ~500 | 3-5/issue | ✅ YES |
| **PR description writing** | Sonnet | ~1000 | 1/issue | ✅ YES |
| **Issue assignment** | Sonnet | ~200 | 1/issue | ❌ No (needs GitHub) |
| **Code review summary** | code-critic agent | ~5000 | 1/issue | ⚠️ Partial |
| **CI error analysis** | devops agent | ~3000 | 0.3/issue | ✅ YES (triage) |
| **Branch naming** | Sonnet | ~100 | 1/issue | ✅ YES |
| **Tech debt issue creation** | Sonnet | ~800 | 0.2/issue | ✅ YES |

**Estimated per-issue token usage**: ~10,600 tokens
**Issues per sprint**: 6-10
**Sprints per week** (autonomous): 2-3

### Low-Frequency Operations (Per Sprint)

| Operation | Current | Tokens/Op | Frequency | Can Use Ollama? |
|-----------|---------|-----------|-----------|----------------|
| **Sprint planning research** | Gemini | ~50K | 1/sprint | ✅ Already optimized |
| **Milestone creation** | Sonnet | ~500 | 1/sprint | ❌ No (needs GitHub) |
| **Issue creation (bulk)** | Sonnet | ~800 each | 8/sprint | ⚠️ Partial (formatting) |
| **Sprint summary** | Sonnet | ~2000 | 1/sprint | ✅ YES |

## Recommended Ollama Delegation Points

### Priority 1: High-Impact, Low-Risk

1. **Commit Message Generation**
   - **Current**: PM generates inline
   - **Proposed**: Ollama hook in `pre-bash` for `git commit`
   - **Token Savings**: ~500 per commit × 4 commits/issue = 2000/issue
   - **Implementation**: `.claude/hooks/ollama/generate-commit-msg.sh`

2. **PR Description Formatting**
   - **Current**: PM writes full markdown
   - **Proposed**: Ollama formats structured data into PR template
   - **Token Savings**: ~800 per issue
   - **Implementation**: Script called before `gh pr create`

3. **CI Error Triage**
   - **Current**: devops agent reads full logs
   - **Proposed**: Ollama pre-filters errors, devops reviews summary
   - **Token Savings**: ~2000 per CI failure (70% of cases)
   - **Implementation**: `.claude/scripts/ollama-ci-triage.sh`

**Total Priority 1 Savings**: ~4,800 tokens/issue × 8 issues/sprint = **38,400 tokens/sprint**

### Priority 2: Medium-Impact

4. **Tech Debt Issue Bodies**
   - **Current**: PM writes full issue markdown
   - **Proposed**: Ollama generates from structured template
   - **Token Savings**: ~600 per tech debt issue
   - **Implementation**: Callable script in PM workflow

5. **Branch Name Generation**
   - **Current**: PM generates adhoc
   - **Proposed**: Ollama generates from issue title
   - **Token Savings**: ~100 per issue
   - **Implementation**: Hook in `validate-branch`

6. **Code Review Summaries**
   - **Current**: code-critic writes full review
   - **Proposed**: Ollama generates summary from structured findings
   - **Token Savings**: ~1500 per review (if critic provides structured data)
   - **Implementation**: Modify code-critic to output JSON, Ollama formats

**Total Priority 2 Savings**: ~2,200 tokens/issue = **17,600 tokens/sprint**

### Priority 3: Lower-Impact

7. **Sprint Issue Descriptions** (during planning)
   - Use Ollama to format issue bodies from Gemini research
   - Savings: ~400 per issue × 8 = 3,200/sprint

8. **Documentation Formatting**
   - Ollama converts structured sprint data to markdown reports
   - Savings: ~1,000/sprint

## Implementation Strategy

### Phase 1: Verification (Immediate)

```bash
# 1. Test Ollama is running
curl http://localhost:11434/api/tags

# 2. Test MCP server
/home/mbrewer/.claude/scripts/ollama-mcp.sh

# 3. Check available tools
# (From Claude Code): Show mcp__ollama__* tools
```

**Blocker**: MCP returns no resources. Need to:
1. Verify MCP server at `/mnt/g/repos/wsl-mcp-ollama/` is built
2. Check server exports tools (not just resources)
3. May need to rebuild server or use direct API calls

### Phase 2: Direct API Integration (If MCP Blocked)

If MCP tools aren't available, bypass MCP and call Ollama API directly:

```bash
# Example: Generate commit message
generate_commit_msg() {
    local diff="$1"
    curl -s http://localhost:11434/api/generate -d '{
        "model": "qwen2.5-coder:7b",
        "prompt": "Write a concise git commit message for:\n'"$diff"'",
        "stream": false
    }' | jq -r '.response'
}
```

**Scripts to Create**:
1. `.claude/scripts/ollama/generate-commit-msg.sh` - Takes diff, returns message
2. `.claude/scripts/ollama/format-pr-body.sh` - Takes structured data, returns markdown
3. `.claude/scripts/ollama/triage-ci-errors.sh` - Takes log, returns summary
4. `.claude/scripts/ollama/format-tech-debt.sh` - Takes template vars, returns issue body

### Phase 3: Hook Integration

**Modify these hooks to call Ollama scripts:**

1. `pre-bash` (for `git commit`) → Call `generate-commit-msg.sh` if no `-m` flag
2. `pre-create-pr` → Call `format-pr-body.sh` before `gh pr create`
3. CI failure detection → Call `triage-ci-errors.sh` before spawning devops

### Phase 4: PM Command Updates

Update `.claude/commands/pm.md` to reference Ollama helper scripts:

```markdown
## COMMIT WORKFLOW

# Generate message via Ollama
MSG=$(.claude/scripts/ollama/generate-commit-msg.sh)
git commit -m "$MSG ..."

## PR CREATION

# Format PR body via Ollama
BODY=$(.claude/scripts/ollama/format-pr-body.sh <issue-number>)
gh pr create --body "$BODY"
```

## Token Savings Projection

### Conservative Estimate (Priority 1 + 2 Only)

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| Tokens/issue | 10,600 | 4,400 | 58% |
| Tokens/sprint (8 issues) | 84,800 | 35,200 | 58% |
| Tokens/week (2.5 sprints) | 212,000 | 88,000 | **58%** |

**Annual Impact** (assuming 50 work weeks):
- **Before**: 10.6M tokens/year
- **After**: 4.4M tokens/year
- **Savings**: 6.2M tokens (~$124 at $0.02/1K tokens for Sonnet)

### Aggressive Estimate (All Priorities)

With full delegation including documentation and formatting:
- **Savings**: 65-70% token reduction
- **Annual**: ~7.5M tokens saved

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Ollama quality lower than Sonnet | High | Medium | Use for templated tasks only |
| Ollama server downtime | Low | High | Fallback to Sonnet if curl fails |
| MCP server issues | Medium | Medium | Use direct API calls instead |
| Longer latency | Medium | Low | Acceptable for non-critical tasks |
| Context loss in delegation | Low | High | Only delegate self-contained tasks |

## Constraints & Guardrails

### Tasks That MUST Use Sonnet
1. **Orchestration logic** - PM decision-making
2. **GitHub operations** - Issue/PR creation (needs MCP)
3. **Code generation** - Actual implementation
4. **Code review** - Quality assessment
5. **Architectural decisions** - Requires reasoning

### Tasks Safe for Ollama
1. **Formatting** - Converting data to templates
2. **Summarization** - Condensing structured data
3. **Text generation** - Commit messages, descriptions
4. **Triage** - Initial error categorization
5. **Naming** - Branch names, variable suggestions

### Quality Checks

All Ollama output should be:
1. **Validated** - Check for template compliance
2. **Bounded** - Max length limits
3. **Fallback-ready** - Revert to Sonnet on error
4. **Logged** - Track quality metrics

## Next Steps

### Immediate (Before Implementing)

1. ✅ **Verify Ollama is running**
   ```bash
   curl http://localhost:11434/api/tags
   ```

2. ✅ **Test Ollama quality**
   ```bash
   # Test commit message generation
   git diff | curl -s http://localhost:11434/api/generate -d @- | jq -r '.response'
   ```

3. ✅ **Check MCP server status**
   ```bash
   ls -la /mnt/g/repos/wsl-mcp-ollama/dist/
   node /mnt/g/repos/wsl-mcp-ollama/dist/index.js --help
   ```

### Implementation Order

1. **Phase 1**: Direct API scripts (bypass MCP issues)
2. **Phase 2**: Commit message generation (highest frequency)
3. **Phase 3**: PR description formatting
4. **Phase 4**: CI triage integration
5. **Phase 5**: Code review summaries (if critic provides JSON)

## Monitoring & Iteration

Track these metrics post-implementation:

| Metric | Target |
|--------|--------|
| Token reduction | >50% |
| Ollama call success rate | >95% |
| Commit message quality | Manual review first 50 |
| PR description quality | Manual review first 20 |
| CI triage accuracy | >80% |
| Fallback invocations | <5% |

## Conclusion

**Recommendation**: Implement Priority 1 + 2 delegation immediately.

**Expected Outcome**:
- 58% token reduction for autonomous PM operations
- Minimal quality impact (templated tasks only)
- $100-150/year cost savings at current usage

**Critical Dependencies**:
1. Verify Ollama server is running
2. Build fallback logic for reliability
3. Update PM command documentation

**Time to Implement**: 4-6 hours for complete Priority 1+2 integration

---

## Appendix A: Example Ollama Scripts

### generate-commit-msg.sh

```bash
#!/bin/bash
# Generate commit message from git diff

DIFF=$(git diff --cached)
if [[ -z "$DIFF" ]]; then
    echo "No staged changes"
    exit 1
fi

# Call Ollama API
PROMPT="Write a concise git commit message (max 50 chars) for these changes:\n\n$DIFF\n\nFormat: <type>: <description>"

curl -s http://localhost:11434/api/generate -d @- <<EOF | jq -r '.response' | head -1
{
    "model": "qwen2.5-coder:7b",
    "prompt": "$PROMPT",
    "stream": false,
    "options": {
        "temperature": 0.3,
        "num_predict": 50
    }
}
EOF
```

### format-pr-body.sh

```bash
#!/bin/bash
# Format PR body from issue context

ISSUE_NUM=$1
ISSUE_DATA=$(gh issue view $ISSUE_NUM --json title,body,labels)

TITLE=$(echo "$ISSUE_DATA" | jq -r '.title')
BODY=$(echo "$ISSUE_DATA" | jq -r '.body')

PROMPT="Create a PR description for issue #$ISSUE_NUM: $TITLE\n\nIssue details:\n$BODY\n\nFormat as:\n## Summary\n[brief]\n## Changes\n- [list]\n## Testing\n[steps]"

curl -s http://localhost:11434/api/generate -d @- <<EOF | jq -r '.response'
{
    "model": "qwen2.5-coder:7b",
    "prompt": "$PROMPT",
    "stream": false,
    "options": {
        "temperature": 0.5
    }
}
EOF
```

## Appendix B: Current Ollama Detection Script

Found in analytics logs - PM was blocked from creating these scripts:
- `/home/mbrewer/.claude/scripts/ollama-mcp.sh` ✅ EXISTS
- `docs/scripts/ollama-commit-msg.sh` ❌ BLOCKED
- `docs/scripts/ollama-ci-errors.sh` ❌ BLOCKED
- `docs/scripts/ollama-quick-review.sh` ❌ BLOCKED

**Action**: Move these from `docs/` to `.claude/scripts/ollama/` for proper integration.
