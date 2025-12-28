# Sprint 18: Codebase Graph Model Implementation Plan

**Sprint ID:** Sprint-18
**Title:** Architecture Graph Model for Agent Consistency
**Duration:** 4 weeks

---

## Goal
Create a machine-readable graph model that ensures:
1. Consistent API structure for every endpoint
2. Uniform frontend-to-API interaction patterns
3. Code standard enforcement as features are added
4. 100% build success after every PR

## Approach
**Script-based AST parsing** (Python + TypeScript) generating a JSON graph that:
- Hooks can query for impact analysis
- CI validates as blocking checks
- Agents use as the "absolute reference point"

---

## Phase 1: Graph Schema & Generation (Week 1)

### 1.1 Create Graph Schema
**File:** `tools/graph/schema.json`

```json
{
  "nodes": {
    "entities": [],      // Domain entities with properties
    "dtos": [],          // DTOs linked to entities
    "services": [],      // Services with method signatures
    "controllers": [],   // Controllers with endpoints
    "hooks": [],         // Frontend hooks with API bindings
    "components": [],    // React components with hook usage
    "work_units": []     // WU dependencies
  },
  "edges": {
    "entity_to_dto": [],
    "dto_to_service": [],
    "service_to_controller": [],
    "file_dependencies": [],
    "agent_scopes": []
  },
  "patterns": {
    "reference_controller": "PeopleController",
    "response_envelope": "{ data: T }",
    "error_format": "ProblemDetails"
  },
  "inconsistencies": []
}
```

### 1.2 Backend Graph Generator
**File:** `tools/graph/generate-backend.py`

Parses:
- `src/Koinon.Domain/Entities/*.cs` → Extract entities, properties, navigation
- `src/Koinon.Application/DTOs/*.cs` → Extract DTOs, link to entities
- `src/Koinon.Application/Services/*.cs` → Extract services, methods
- `src/Koinon.Api/Controllers/*.cs` → Extract controllers, endpoints, patterns
- `src/Koinon.Infrastructure/Data/Configurations/*.cs` → Extract relationships

Output: Entities, DTOs, Services, Controllers, and their relationships.

**Key patterns to detect:**
- Response envelope usage (`new { data = ... }`)
- IdKey vs integer ID in routes
- ProblemDetails for errors
- Result<T> pattern in services
- Async methods with CancellationToken

### 1.3 Frontend Graph Generator
**File:** `tools/graph/generate-frontend.ts`

Parses:
- `src/web/src/services/api/types.ts` → Extract TypeScript types
- `src/web/src/services/api/*.ts` → Extract API functions
- `src/web/src/hooks/*.ts` → Extract hooks, link to API
- `src/web/src/components/**/*.tsx` → Extract component dependencies
- `src/web/src/pages/**/*.tsx` → Extract page compositions

Output: Types, API services, Hooks, Components, Pages, and their relationships.

**Key patterns to detect:**
- Direct fetch calls in components (anti-pattern)
- Hook wrapping of all API calls
- Type imports from types.ts
- Zod validation usage

### 1.4 Graph Merge & Output
**File:** `tools/graph/merge-graph.py`

Combines backend + frontend graphs into:
- `.claude/graph-cache.json` (hook-accessible, gitignored)
- `tools/graph/api-graph.json` (committed for CI baseline)

---

## Phase 2: Hook Integration (Week 2)

### 2.1 Graph Query Library
**File:** `.claude/hooks/lib/graph-query.sh`

```bash
# Query functions for hooks:
# - get_dependents(file) → files that depend on this file
# - get_affected_wus(file) → work units affected
# - validate_agent_scope(file, agent) → can agent modify?
# - get_pattern_template(type) → canonical pattern for type
```

### 2.2 Impact Analysis Hook
**File:** `.claude/hooks/graph-impact-check`

- Trigger: `pre-write` on any source file
- Action: Query graph for downstream impact
- Output: Inform agent of affected files/WUs

### 2.3 Enhanced Gate-Spawn
**Modify:** `.claude/hooks/gate-spawn`

Replace hardcoded case statements with graph queries:
```bash
ALLOWED=$(query_graph "agents.$CURRENT_AGENT.can_spawn")
```

### 2.4 Work Unit Independence Validation
**Modify:** `.claude/hooks/pre-issue-pick`

Add code-level independence check:
```bash
# Check if WU files overlap with in-progress WU files
OVERLAP=$(query_graph "check_wu_overlap $WU_ID $CURRENT_WU")
```

---

## Phase 3: CI Validation (Week 3)

### 3.1 Graph Generation Workflow
**File:** `.github/workflows/graph-validate.yml`

```yaml
name: Graph Validation
on: [push, pull_request]
jobs:
  validate:
    steps:
      - Generate graph from code
      - Compare to baseline (tools/graph/api-graph.json)
      - Fail if inconsistencies found
      - Fail if drift detected without explicit update
```

### 3.2 Consistency Checks (BLOCKING)

| Check | Description | Exit on Fail |
|-------|-------------|--------------|
| Response envelope | All controllers use `{ data: T }` | Yes |
| No integer IDs | DTOs never expose `int Id` | Yes |
| IdKey in routes | Routes use `{idKey}` not `{id}` | Yes |
| Hook wrapping | No direct fetch in components | Yes |
| Type alignment | Frontend types match backend DTOs | Yes |

### 3.3 Contract Verification Script
**File:** `tools/graph/verify-contracts.py`

Compares:
- Backend DTOs (`src/Koinon.Application/DTOs/`)
- Frontend types (`src/web/src/services/api/types.ts`)

Fails if:
- Missing frontend type for backend DTO
- Property name mismatch
- Property type mismatch

---

## Phase 4: Agent Query Interface (Week 4)

### 4.1 Extend koinon-dev MCP
**Modify:** `tools/mcp-koinon-dev/src/index.ts`

Add tools:
```typescript
query_api_graph(query, params)
  - get_controller_pattern(entity)
  - get_entity_chain(entity)
  - list_inconsistencies()
  - validate_new_controller(name)

get_implementation_template(type, entity)
  - Returns canonical code template

get_impact_analysis(file_path)
  - Returns affected files and work units
```

### 4.2 Agent Documentation
**Modify:** `koinon-rms/CLAUDE.md`

Add section:
```markdown
## Graph Model Queries

Before implementing a new feature:
mcp__koinon-dev__query_api_graph({
  query: "get_entity_chain",
  entityName: "Event"
})

Before modifying a file:
mcp__koinon-dev__get_impact_analysis({
  file_path: "src/Koinon.Domain/Entities/Person.cs"
})
```

---

## Phase 5: Fix Existing Inconsistencies (Week 4)

**Total: 37 violations across 7 files** (identified via Gemini codebase analysis)

### 5.1 GivingController Fixes (22 violations - HIGH)
**File:** `src/Koinon.Api/Controllers/GivingController.cs`

Response Envelope Issues:
- Lines 39, 67, 130, 158, 292, 363: Bare DTO/list returns → wrap in `{ data: ... }`
- Lines 224, 260: Non-standard `{ message: ... }` → use `{ data: ... }`

Error Format Issues (14 instances):
- Lines 62, 125, 153, 216, 219, 252, 255, 287, 324, 327, 358, 395, 398, 431, 434
- Change `NotFound/BadRequest(new { error = ... })` → use `ProblemDetails`

### 5.2 Parameter Naming Standardization (15 violations - MEDIUM)

**FamiliesController** (`src/Koinon.Api/Controllers/FamiliesController.cs`)
- Lines 25-26, 37-38: `searchTerm` → `query`, `campusIdKey` → `campusId`

**PublicGroupsController** (`src/Koinon.Api/Controllers/PublicGroupsController.cs`)
- Lines 24, 38: `searchTerm` → `query`
- Lines 26, 40: `campusIdKey` → `campusId`

**AnalyticsController** (`src/Koinon.Api/Controllers/AnalyticsController.cs`)
- Lines 27, 40, 67, 81, 110, 122, 145, 154, 171, 182: `campusIdKey` → `campusId`

**PagerController** (`src/Koinon.Api/Controllers/PagerController.cs`)
- Lines 114, 125: `searchTerm` → `query`

### 5.3 FilesController Error Format (2 violations - HIGH)
**File:** `src/Koinon.Api/Controllers/FilesController.cs`
- Line 51: `BadRequest(new { error = "..." })` → use `ProblemDetails`
- Line 79: `BadRequest(new { error = "..." })` → use `ProblemDetails`

### 5.4 Frontend ApiError Type Mismatch (1 violation - MEDIUM)
**File:** `src/web/src/services/api/types.ts`
- Lines 36-43: Update `ApiError` type to match `ProblemDetails` structure

### Summary by Severity

| Severity | Count | Files |
|----------|-------|-------|
| HIGH | 24 | GivingController (22), FilesController (2) |
| MEDIUM | 13 | Families, PublicGroups, Analytics, Pager, types.ts |

---

## Critical Files

### Backend Graph Sources
- `src/Koinon.Domain/Entities/*.cs`
- `src/Koinon.Application/DTOs/*.cs`
- `src/Koinon.Application/Services/*.cs`
- `src/Koinon.Api/Controllers/*.cs`
- `src/Koinon.Infrastructure/Data/Configurations/*.cs`

### Frontend Graph Sources
- `src/web/src/services/api/types.ts`
- `src/web/src/services/api/*.ts`
- `src/web/src/hooks/*.ts`

### Reference Implementations
- `src/Koinon.Api/Controllers/PeopleController.cs` (canonical controller)
- `src/web/src/hooks/usePeople.ts` (canonical hook)

### Files to Create
- `tools/graph/schema.json`
- `tools/graph/generate-backend.py`
- `tools/graph/generate-frontend.ts`
- `tools/graph/merge-graph.py`
- `tools/graph/verify-contracts.py`
- `.claude/hooks/lib/graph-query.sh`
- `.claude/hooks/graph-impact-check`
- `.github/workflows/graph-validate.yml`

### Files to Modify
- `tools/mcp-koinon-dev/src/index.ts` (add graph query tools)
- `.claude/hooks/gate-spawn` (use graph for hierarchy)
- `.claude/hooks/pre-issue-pick` (add WU overlap check)
- `koinon-rms/CLAUDE.md` (document graph usage)
- `src/Koinon.Api/Controllers/GivingController.cs` (22 violations)
- `src/Koinon.Api/Controllers/FamiliesController.cs` (2 violations)
- `src/Koinon.Api/Controllers/PublicGroupsController.cs` (2 violations)
- `src/Koinon.Api/Controllers/AnalyticsController.cs` (10 violations)
- `src/Koinon.Api/Controllers/PagerController.cs` (1 violation)
- `src/Koinon.Api/Controllers/FilesController.cs` (2 violations)
- `src/web/src/services/api/types.ts` (1 violation)

---

## Phase 6: Frontend Error Handling Migration (Week 4)

**Issue:** #288

### 6.1 ProblemDetails Type Definition
**File:** `src/web/src/services/api/types.ts`

```typescript
// RFC 7807 compliant
interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
}
```

### 6.2 API Client Error Interceptor
**File:** `src/web/src/services/api/client.ts`
- Parse `ProblemDetails` format from error responses
- Extract `detail` for user-facing messages
- Extract `status` for error classification

### 6.3 Error Display Updates
- Update toast/notification logic to use `detail` instead of `error.message`
- Update TanStack Query error callbacks
- Update error boundary components

---

## Phase 7: Graph Baseline Documentation (Week 4)

**Issue:** #289

### 7.1 Update Commands
- Add `npm run graph:update` command
- Document in CONTRIBUTING.md

### 7.2 PR Template
- Add checkbox: "[ ] Updates graph baseline (if applicable)"

### 7.3 CI Enhancement
- Detect "baseline update required" scenarios
- Skip drift check if PR explicitly updates baseline

---

## Phase 8: Generator Test Coverage (Weeks 2-4)

**Issue:** #290

### 8.1 Test Structure
```
tools/graph/
├── tests/
│   ├── test_generate_backend.py
│   ├── test_generate_frontend.ts
│   └── test_merge_graph.py
├── fixtures/
│   ├── valid/      # Canonical implementations
│   ├── invalid/    # Intentional violations
│   └── edge-cases/ # Empty, syntax errors
```

### 8.2 Coverage Requirements
- 80% line coverage minimum
- 100% coverage for pattern detection functions

---

## Phase 9: Local Pre-Push Validation (Week 3)

**Issue:** #291

### 9.1 Validation Scripts
```bash
scripts/
├── validate-local.sh   # Full CI-equivalent validation
└── validate-staged.sh  # Quick check for staged files only
```

### 9.2 Git Hooks
- **Pre-commit:** TypeScript typecheck, ESLint, dotnet build
- **Pre-push:** Full test suites, graph validation, contract verification

### 9.3 Token Savings
| Scenario | CI Round-trip | Local Check | Savings |
|----------|---------------|-------------|---------|
| Type error | ~2000 tokens | ~50 tokens | 97% |
| Test failure | ~3000 tokens | ~100 tokens | 97% |
| Pattern violation | ~1500 tokens | ~30 tokens | 98% |

---

## Success Criteria

1. **Agent guidance**: Any agent can query `get_entity_chain("Event")` and know exactly what to create
2. **100% build success**: Graph validation in CI catches all pattern violations before merge
3. **No drift**: Graph regenerates on every PR; drift from baseline fails CI
4. **Impact visibility**: Before modifying any file, agent sees downstream effects
5. **Consistency enforced**: All 37 violations fixed; no new ones allowed
6. **Frontend compatibility**: Error handling works with ProblemDetails format
7. **Developer experience**: Clear baseline update process documented
8. **Reliable tooling**: Generator tests prevent regressions
9. **Fast feedback**: Local validation catches 90%+ of issues before push

---

## Estimated Timeline

| Week | Focus | Deliverables |
|------|-------|-------------|
| 1 | Graph generation | Backend + frontend parsers, initial tests |
| 2 | Hook integration | Impact analysis, gate-spawn graph-based, more tests |
| 3 | CI validation | Blocking checks, contract verification |
| 4 | Polish + fixes | MCP tools, fix 37 violations, frontend migration, docs |

Total: ~4 weeks for full stack implementation.

---

## GitHub Issues Summary

| # | Title | Phase | Priority |
|---|-------|-------|----------|
| 283 | Graph schema + generation scripts | 1 | P0 |
| 284 | Hook integration for graph queries | 2 | P0 |
| 285 | CI validation workflow | 3 | P0 |
| 286 | MCP agent query interface | 4 | P1 |
| 287 | Fix existing inconsistencies (37 violations) | 5 | P0 |
| 288 | Frontend error handling migration | 6 | P0 |
| 289 | Graph baseline documentation | 7 | P1 |
| 290 | Generator test coverage | 8 | P1 |
| 291 | Local pre-push validation | 9 | P0 |
