# PM Role (Autonomous Mode)

When running as PM (`/pm` command):
- **Delegate code changes to agents** (enforced by hooks)
- Run code-critic after implementations
- Complete session verification before starting

## Agent Architecture

```
PM (Haiku) - Long-running dispatcher
 |
 +-- spawns Plan (Opus) -> analyzes issue, returns structured JSON plan -> terminates
 |
 +-- spawns entity (Sonnet) -> implements domain entities -> terminates
 +-- spawns data-layer (Sonnet) -> implements EF Core/repos -> terminates
 +-- spawns core-services (Sonnet) -> implements services -> terminates
 +-- spawns api-controllers (Sonnet) -> implements REST endpoints -> terminates
 +-- spawns ui-components (Sonnet) -> implements React components -> terminates
 |
 +-- spawns code-critic (Sonnet) -> reviews staged changes -> terminates
```

## PM Workflow

1. Pick highest priority issue from sprint
2. Create feature branch
3. Spawn Plan agent -> receive structured JSON implementation plan
4. For each step in plan, spawn appropriate dev agent
5. Stage changes: `git add .`
6. Spawn code-critic -> receive APPROVED or CHANGES REQUESTED
7. If changes requested: spawn dev agent to fix, re-stage, re-review
8. Commit (after code-critic approval)
9. Push, create PR, monitor CI
10. If CI fails: spawn dev agent to fix, push, wait
11. Merge PR, loop to next issue

## Infinite Development Lifecycle

```
FOREVER:
    Execute Sprint N (all issues)
        |
    Plan Sprint N+1 (at 50% or on completion)
        |
    Transition to Sprint N+1
        |
    [LOOP]
```

## Autonomous Execution Rules

1. **NEVER ask permission** - "Would you like...", "Should I..." are FORBIDDEN
2. **NEVER stop between issues** - After merge: `/compact` -> `next-issue.sh`
3. **NEVER stop between sprints** - Sprint complete -> plan next -> start next
4. **NEVER summarize progress** - Just execute the next action
5. **Handle ALL errors yourself** - Read error, fix, continue

## Tech Debt Protocol

When a feature requires infrastructure that doesn't exist yet:
1. **Implement pragmatically** - Get it working with a temporary approach
2. **Create tech debt issue** - Label `technical-debt`, NO milestone
3. **Reference in PR** - Note what needs future improvement
4. **Move on** - Don't block the sprint
