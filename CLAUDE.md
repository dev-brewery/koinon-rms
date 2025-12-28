# CLAUDE.md - Koinon RMS Project Intelligence

You are an expert software architect working on **Koinon RMS**, a greenfield Church Management System using .NET 8 and React with clean architecture.

## MAXIMUS Agent Detection

> **If your prompt contains "MAXIMUS" or "Read your instructions:"**, you are a MAXIMUS autonomous agent.
> **SKIP ALL** session verification, hooks, scripts, and PM mode instructions in this file.
> Follow ONLY your MAXIMUS agent instructions file.

## Session Start (Required)

> **MAXIMUS agents: SKIP this section entirely.**

```bash
mcp__memory__read_graph                    # 1. Load context
.claude/scripts/confirm-memory-check.sh   # 2. Confirm (MAXIMUS: skip)
.claude/scripts/verify-session.sh         # 3. Verify (MAXIMUS: skip)
```

Sessions expire after 4 hours. Hooks enforce verification before code changes. *(MAXIMUS agents: ignore hooks)*

## Project Identity

- **Name:** Koinon RMS
- **Tech:** .NET 8, EF Core, PostgreSQL, Redis, React 18, TypeScript, Vite
- **MVP Focus:** Performance-critical check-in kiosks (<200ms online, <50ms offline)

## Repository Structure

```
koinon-rms/
├── src/
│   ├── Koinon.Domain/         # Entities, interfaces (no dependencies)
│   ├── Koinon.Application/    # Use cases, DTOs, validators
│   ├── Koinon.Infrastructure/ # EF Core, Redis
│   ├── Koinon.Api/            # Web API
│   └── web/                   # React frontend
├── tests/                     # Test projects
├── docs/reference/            # entity-mappings, api-contracts, work-breakdown
└── .claude/                   # Hooks, agents, scripts
```

## Clean Architecture

```
API → Application → Domain
         ↓
    Infrastructure
```

| Layer | Responsibility |
|-------|----------------|
| Domain | Entities, interfaces (no dependencies) |
| Application | Use cases, DTOs, validation (depends on Domain) |
| Infrastructure | EF Core, Redis, external APIs |
| Api | Controllers, middleware, auth |

## Entity Design

```csharp
public abstract class Entity : IEntity, IAuditable
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string IdKey => IdKeyHelper.Encode(Id);  // Use in URLs
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
}
```

## Database Conventions

| Aspect | Convention | Example |
|--------|------------|---------|
| Tables | snake_case | `group_member` |
| Columns | snake_case | `first_name` |
| C# properties | PascalCase | `FirstName` |
| Primary keys | `id` | int identity |
| Foreign keys | `{entity}_id` | `person_id` |

## API Design

- Base path: `/api/v1/`
- Use `IdKey` in URLs (never integer IDs)
- See `docs/reference/api-contracts.md`

## Development Commands

```bash
# Infrastructure
docker-compose up -d                    # Start postgres + redis
docker-compose down -v                  # Stop and delete data

# Backend
dotnet build
dotnet test
dotnet watch run --project src/Koinon.Api

# Migrations
dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Frontend (from src/web)
npm install && npm run dev
```

## Coding Standards

**C#:** File-scoped namespaces, primary constructors, records for DTOs, PascalCase files
**TypeScript:** Strict mode, no `any`, functional components, TanStack Query

## Anti-Patterns

- Never expose integer IDs (use IdKey)
- No business logic in controllers
- No synchronous DB calls
- No N+1 queries
- No `any` in TypeScript
- No class components

## PM Role (Autonomous Mode)

> **MAXIMUS agents: SKIP this entire section. You are NOT the PM.**

When running as PM (`/pm` command):
- **Delegate code changes to agents** (enforced by hooks) *(MAXIMUS: ignore)*
- Run code-critic after implementations
- Complete session verification before starting *(MAXIMUS: skip)*

### Agent Architecture

```
PM (Haiku) - Long-running dispatcher
 │
 ├── spawns Plan (Opus) → analyzes issue, returns structured JSON plan → terminates
 │
 ├── spawns entity (Sonnet) → implements domain entities → terminates
 ├── spawns data-layer (Sonnet) → implements EF Core/repos → terminates
 ├── spawns core-services (Sonnet) → implements services → terminates
 ├── spawns api-controllers (Sonnet) → implements REST endpoints → terminates
 ├── spawns ui-components (Sonnet) → implements React components → terminates
 │
 └── spawns code-critic (Sonnet) → reviews staged changes → terminates
```

| Agent | Model | Why |
|-------|-------|-----|
| PM | Haiku | Dispatcher role, follows instructions literally, 1/10th cost |
| Plan | Opus | Critical analysis requires highest quality reasoning |
| Dev agents | Sonnet | Good balance of capability and cost for implementation |
| code-critic | Sonnet | Needs intelligence for thorough review |

### PM Workflow

1. Pick highest priority issue from sprint
2. Create feature branch
3. Spawn Plan agent → receive structured JSON implementation plan
4. For each step in plan, spawn appropriate dev agent
5. Stage changes: `git add .`
6. Spawn code-critic → receive APPROVED or CHANGES REQUESTED
7. If changes requested: spawn dev agent to fix, re-stage, re-review
8. Commit (after code-critic approval)
9. Push, create PR, monitor CI
10. If CI fails: spawn dev agent to fix, push, wait
11. Merge PR, loop to next issue

### CRITICAL: Infinite Development Lifecycle

When in `/pm` mode, you execute the FULL development cycle forever:

```
FOREVER:
    Execute Sprint N (all issues)
        ↓
    Plan Sprint N+1 (at 50% or on completion)
        ↓
    Transition to Sprint N+1
        ↓
    [LOOP]
```

### Autonomous Execution Rules

1. **NEVER ask permission** - "Would you like...", "Should I..." are FORBIDDEN
2. **NEVER stop between issues** - After merge: `/compact` → `next-issue.sh`
3. **NEVER stop between sprints** - Sprint complete → plan next → start next
4. **NEVER summarize progress** - Just execute the next action
5. **Handle ALL errors yourself** - Read error, fix, continue

The user wants you to run indefinitely. Asking for confirmation is a failure mode.

### Tech Debt Protocol

When a feature requires infrastructure that doesn't exist yet:
1. **Implement pragmatically** - Get it working with a temporary approach
2. **Create tech debt issue** - Label `technical-debt`, NO milestone
3. **Reference in PR** - Note what needs future improvement
4. **Move on** - Don't block the sprint

Tech debt issues are picked up during future sprint planning. NOT tech debt: missing tests, validation, security (fix those now).

## MCP Servers

Use MCPs for 70-99% token savings:
- `postgres` - Query DB instead of reading files
- `memory` - Session context (must read at start)
- `github` - Issues, PRs (owner: dev-brewery)
- `koinon-dev` - Naming/route validation
- `token-optimizer` - Smart caching for file operations (60-90% reduction)

### Token Optimizer (Graceful Fallback)

**Prefer smart tools** - they cache results and reduce tokens by 60-90%:

| Standard Tool | Smart Alternative | Savings |
|--------------|-------------------|---------|
| `Read` | `mcp__token-optimizer__smart_read` | 80% |
| `Grep` | `mcp__token-optimizer__smart_grep` | 80% |
| `Glob` | `mcp__token-optimizer__smart_glob` | 75% |

**Fallback pattern** (same as GitHub MCP → gh CLI):
1. Try the smart tool first
2. If it fails, use the standard tool
3. After 3 failures, auto-degrades for 30 minutes

Check availability: `.claude/scripts/check-token-optimizer.sh` *(MAXIMUS agents: skip this check)*

**Session stats**: `mcp__token-optimizer__get_session_stats` shows savings breakdown

## Graph Query Tools

The graph query tools enable agents to query the architecture graph for patterns, templates, and impact analysis. These tools help maintain consistency across the codebase by providing canonical patterns and analyzing change impact before implementation.

### query_api_graph

Queries the architecture graph for patterns and entity chains.

**Operations:**

| Operation | Description | Required Args |
|-----------|-------------|---------------|
| `get_controller_pattern` | Returns canonical pattern for controller | entityName |
| `get_entity_chain` | Returns Entity->DTO->Service->Controller chain | entityName |
| `list_inconsistencies` | Returns all pattern violations | none |
| `validate_new_controller` | Validates proposed controller name | entityName |

**Example:**
```typescript
mcp__koinon-dev__query_api_graph({
  query: "get_entity_chain",
  entityName: "Person"
})
// Returns: full chain with file paths
```

### get_implementation_template

Returns code templates following project conventions.

**Types:** entity, dto, service, controller

**Example:**
```typescript
mcp__koinon-dev__get_implementation_template({
  type: "controller",
  entityName: "Event"
})
// Returns: template code, file path, conventions to follow
```

### get_impact_analysis

Analyzes what files and work units would be affected by changes to a file.

**Example:**
```typescript
mcp__koinon-dev__get_impact_analysis({
  file_path: "src/Koinon.Domain/Entities/Person.cs"
})
// Returns: affected files, affected work units, impact summary
```

### When to Use Graph Queries

- **Before creating new features:** Use `get_entity_chain` to see the complete pattern for similar entities
- **Before modifying entities:** Use `get_impact_analysis` to understand the scope of changes
- **During code review:** Use `list_inconsistencies` to find pattern violations
- **When scaffolding:** Use `get_implementation_template` for the correct starting point

## Quick Reference

```
PostgreSQL: localhost:5432 (koinon/koinon)
Redis: localhost:6379
API: localhost:5000
Web: localhost:5173
```

## Key Documentation

| Document | When to Use |
|----------|-------------|
| `docs/reference/entity-mappings.md` | Implementing entities |
| `docs/reference/api-contracts.md` | Implementing API endpoints |
| `docs/reference/work-breakdown.md` | Work unit specs |
| `.claude/GUARDRAILS.md` | Safety rules *(MAXIMUS agents: skip)* |
| `.claude/HOOKS.md` | Hook reference *(MAXIMUS agents: skip)* |

## Graph Baseline System

The API graph is a contract between architectural layers that validates type safety and design consistency across:
- Domain Entities (C#)
- Application DTOs (C#)
- API Endpoints (C#)
- Frontend Components (TypeScript/React)

### Overview

The graph baseline (`tools/graph/graph-baseline.json`) tracks your architecture's structure. When code changes, agents must validate that the graph still matches reality.

### Commands

```bash
# Validate current code against baseline
npm run graph:validate

# Regenerate baseline after structural changes
npm run graph:update
```

### When Agents Should Update Baseline

Update `tools/graph/graph-baseline.json` when:
- Adding new entity types
- Adding new DTOs
- Adding API endpoints
- Renaming entity/DTO fields
- Adding new components
- Reorganizing component structure

Do NOT update for implementation details (method bodies, comments, tests).

### MCP Query for Baseline Check

Before updating baseline, agents should verify current state:

```bash
# Check if graph file exists
[ -f tools/graph/graph-baseline.json ] && echo "Baseline exists" || echo "No baseline yet"

# View baseline structure
jq '.entities | keys' tools/graph/graph-baseline.json
jq '.endpoints | keys' tools/graph/graph-baseline.json
```

### Workflow for Adding New Entity

1. Create entity: `src/Koinon.Domain/Entities/YourEntity.cs`
2. Create DTO: `src/Koinon.Application/DTOs/YourDto.cs`
3. Create controller: `src/Koinon.Api/Controllers/YourController.cs`
4. Update baseline: `npm run graph:update`
5. Commit both code and `tools/graph/graph-baseline.json`

### CI Validation

CI automatically detects when graph baseline needs updating:
- Monitors changes in `src/Koinon.Domain/Entities/`
- Monitors changes in `src/Koinon.Application/DTOs/`
- Monitors changes in `src/Koinon.Api/Controllers/`
- Monitors changes in `src/web/**`

If structural changes are detected without baseline update, CI labels PR with `baseline-update-required`.

### Documentation

- **Full guide:** `tools/graph/README.md`
- **Contributing guidelines:** `CONTRIBUTING.md` section "Graph Baseline Updates"
- **CI workflow:** `.github/workflows/graph-validate.yml`


