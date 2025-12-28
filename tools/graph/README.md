# API Graph Validation System

The API graph is a comprehensive map of your system's architecture that connects entities, DTOs, API endpoints, and frontend components. It enables validation of design contracts and prevents drift between layers.

## Overview

The graph validation system generates baseline snapshots of your architecture and detects when changes require updates. This ensures:

- **Type safety** across layers (Entity → DTO → API → Component)
- **API contract consistency** (request/response types match controllers)
- **No orphaned DTOs** (all DTOs used in endpoints)
- **No orphaned components** (all components used in pages)
- **Missing implementations** caught early

## System Architecture

```
Source Code Layer          Generator                 Artifacts
─────────────────────────────────────────────────────────────

Domain Entities     ──┐
Application DTOs    ──┼─→ generate-backend.py ──→ backend-graph.json
API Controllers     ──┘

React Components    ──┐
Frontend Pages      ──┼─→ generate-frontend.ts ──→ frontend-graph.json
Hooks/Utils         ──┘

──────────────────────────┐
                          ├─→ merge-graph.py ──────→ graph.json (unified)
──────────────────────────┘
                          ├─→ verify-contracts.py ──→ graph-baseline.json
                                                       (approved snapshot)
```

## Commands

### graph:validate

Validates current architecture against the approved baseline without modifying anything.

```bash
npm run graph:validate
```

**Output:**
- ✓ Graph matches baseline: No changes detected, merge safe
- ✗ Graph drift detected: Review changes before merging

**Exit codes:**
- 0: No drift, CI passes
- 1: Drift detected, must be approved

### graph:update

Regenerates the baseline from current source code.

```bash
npm run graph:update
```

**When to use:**
1. Adding a new entity type
2. Renaming entity fields or parameters
3. Creating new API endpoints
4. Reorganizing component structure
5. Major structural refactoring

## CLI Arguments

Both `generate-frontend.js` and `merge-graph.py` support custom path arguments for advanced usage (especially useful for testing with fixture directories).

### generate-frontend.js

Parses TypeScript/React source files from a custom directory and outputs to a custom location.

**Arguments:**
- `--src-dir <path>` - Source directory containing `src/web/src` structure (default: `src/web/src`)
- `--output-dir <path>` - Output directory for generated `frontend-graph.json` (default: `tools/graph`)

**Examples:**

Default behavior (no arguments):
```bash
node tools/graph/generate-frontend.js
# Reads from: src/web/src/
# Writes to: tools/graph/frontend-graph.json
```

Custom source directory for testing:
```bash
node tools/graph/generate-frontend.js --src-dir tests/fixtures/web
# Reads from: tests/fixtures/web/
# Writes to: tools/graph/frontend-graph.json
```

Custom output directory:
```bash
node tools/graph/generate-frontend.js --output-dir tests/output
# Reads from: src/web/src/
# Writes to: tests/output/frontend-graph.json
```

Both arguments together:
```bash
node tools/graph/generate-frontend.js --src-dir tests/fixtures/web --output-dir tests/output
# Reads from: tests/fixtures/web/
# Writes to: tests/output/frontend-graph.json
```

### merge-graph.py

Combines backend and frontend graphs, detecting cross-layer inconsistencies.

**Arguments:**
- `--backend <path>` or `-b <path>` - Backend graph input file (default: `tools/graph/backend-graph.json`)
- `--frontend <path>` or `-f <path>` - Frontend graph input file (default: `tools/graph/frontend-graph.json`)
- `--output <path>` or `-o <path>` - Output file path (default: `tools/graph/graph-baseline.json`)

**Examples:**

Default behavior (no arguments):
```bash
python3 tools/graph/merge-graph.py
# Reads: tools/graph/backend-graph.json + tools/graph/frontend-graph.json
# Writes: tools/graph/graph-baseline.json
```

Custom backend and frontend for testing:
```bash
python3 tools/graph/merge-graph.py \
  --backend tests/fixtures/backend-graph.json \
  --frontend tests/fixtures/frontend-graph.json
# Reads: tests/fixtures/backend-graph.json + tests/fixtures/frontend-graph.json
# Writes: tools/graph/graph-baseline.json
```

Custom output path:
```bash
python3 tools/graph/merge-graph.py --output tests/output/merged-graph.json
# Reads: tools/graph/backend-graph.json + tools/graph/frontend-graph.json
# Writes: tests/output/merged-graph.json
```

All arguments together (full test isolation):
```bash
python3 tools/graph/merge-graph.py \
  --backend tests/fixtures/backend-graph.json \
  --frontend tests/fixtures/frontend-graph.json \
  --output tests/output/graph-baseline.json
# Reads: tests/fixtures/backend-graph.json + tests/fixtures/frontend-graph.json
# Writes: tests/output/graph-baseline.json
```

### Test Isolation Pattern

These arguments enable test isolation with fixture directories:

```bash
# Setup fixture directories
mkdir -p tests/fixtures tests/output

# Copy fixture files
cp tools/graph/backend-graph.json tests/fixtures/

# Run generator with fixtures
node tools/graph/generate-frontend.js --src-dir tests/fixtures/web --output-dir tests/output
python3 tools/graph/merge-graph.py \
  --backend tests/fixtures/backend-graph.json \
  --frontend tests/output/frontend-graph.json \
  --output tests/output/graph-baseline.json

# Cleanup
rm -rf tests/output
```

This pattern ensures:
- No modification of production files
- Repeatable test runs
- Safe experimentation with new graph structures

## Common Scenarios

### Scenario 1: Adding a New Entity Type

When you create `Attendance.cs` entity:

```bash
# 1. Create entity, DTO, controller
# 2. Run graph update
npm run graph:update

# 3. Verify diff shows new endpoints
git diff tools/graph/graph-baseline.json | grep -A 5 'Attendance'

# 4. Commit
git add -A && git commit -m "feat(attendance): add attendance tracking with graph update"
```

### Scenario 2: Renaming a Parameter or Field

```bash
# 1. Make the rename
# 2. Run graph update
npm run graph:update

# 3. Verify the diff
git diff tools/graph/graph-baseline.json | grep -C 3 'GivenName'

# 4. Commit
git add -A && git commit -m "refactor(person): rename FirstName to GivenName"
```

### Scenario 3: Adding New API Endpoint

```bash
# 1. Add endpoint method
# 2. Run update
npm run graph:update

# 3. Verify new endpoint appears in graph
git diff tools/graph/graph-baseline.json | grep -A 10 '"batch"'

# 4. Commit
git add -A && git commit -m "feat(person): add batch creation endpoint"
```

### Scenario 4: Reorganizing Component Structure

```bash
# 1. Move files (git mv)
# 2. Update imports throughout the codebase
# 3. Run graph update
npm run graph:update

# 4. Verify structure is captured
git diff tools/graph/graph-baseline.json | grep -A 3 'features/people'

# 5. Commit
git add -A && git commit -m "refactor: reorganize person components to features layout"
```

## Graph Structure

### graph-baseline.json Format

```json
{
  "version": "1.0",
  "generated_at": "2024-12-22T15:30:00Z",
  "entities": {
    "Person": {
      "namespace": "Koinon.Domain.Entities",
      "table": "person",
      "fields": {
        "id": "int",
        "guid": "Guid",
        "first_name": "string"
      }
    }
  },
  "dtos": {
    "PersonDto": {
      "namespace": "Koinon.Application.DTOs",
      "properties": {
        "id": "string",
        "firstName": "string"
      }
    }
  }
}
```

## Troubleshooting

### "Scripts not found" Error

If you see "Command not found: python3" or similar, the graph tools haven't been set up yet (Sprint 18).

### Graph Drift Detected But I Made No Changes

This can happen if:
1. Git hooks modified files
2. Database schema changed
3. IDE auto-formatted code

### Which Changes Require Graph Baseline Update?

**Always update baseline when:**
- Adding new entities
- Adding new DTOs
- Adding API endpoints
- Renaming entity/DTO fields
- Reorganizing component structure

**Don't need update when:**
- Changing implementation details
- Adding private methods
- Changing comments/documentation
- Formatting/whitespace changes
- Adding tests

## Integration with CI

The CI workflow (`graph-validate.yml`) automatically:
1. Detects baseline changes
2. Validates graph consistency
3. Allows baseline skipping via label

## Running Tests

The backend graph generator includes comprehensive unit tests using pytest.

### Setup Test Environment

```bash
# Install test dependencies
pip3 install -r tools/graph/requirements-test.txt
```

### Run Tests

```bash
# Run all tests
pytest tools/graph/tests/

# Run with verbose output
pytest tools/graph/tests/ -v

# Run with coverage report
pytest tools/graph/tests/ --cov=tools/graph --cov-report=html

# Run specific test class
pytest tools/graph/tests/test_generate_backend.py::TestCSharpParserExtractProperties -v

# Run specific test method
pytest tools/graph/tests/test_generate_backend.py::TestCSharpParserExtractProperties::test_extract_properties_simple -v
```

### Test Structure

```
tools/graph/tests/
├── __init__.py             # Package init
├── conftest.py             # Pytest fixtures
└── test_generate_backend.py # CSharpParser and BackendGraphGenerator tests
```

Tests use fixtures from `tools/graph/fixtures/`:
- `valid/` - Valid C# files following project conventions
- `invalid/` - Files violating conventions (for negative testing)
- `edge-cases/` - Unusual but valid formatting

## Related Documentation

- **Architecture:** See `CLAUDE.md` section "Graph Baseline"
- **Contributing:** See `CONTRIBUTING.md` section "Graph Baseline Updates"
- **CI Validation:** See `.github/workflows/graph-validate.yml`
