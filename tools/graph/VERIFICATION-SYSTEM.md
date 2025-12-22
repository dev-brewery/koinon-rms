# Architecture Contract Verification System

The verification system ensures consistency and correctness across the Koinon RMS architecture graph through automated contract checks.

## Overview

```
Source Code (C# + TypeScript)
         ↓
graph-baseline.json (Architecture snapshot)
         ↓
verify-contracts.py (Contract verification)
         ↓
PASS/FAIL report with violation details
```

## Quick Start

```bash
# Verify contracts on current baseline
python3 tools/graph/verify-contracts.py

# Expected output if all contracts pass:
# ✓ VERIFICATION PASSED
# Exit code: 0

# If contracts fail:
# ✗ VERIFICATION FAILED
# Exit code: 1
```

## The 5 Verification Checks

| Check | Purpose | Severity | When It Triggers |
|-------|---------|----------|------------------|
| **1. Response Envelope** | Controllers document response style | FAIL | Missing `patterns.response_envelope` |
| **2. No Integer IDs** | DTOs don't expose integer IDs | FAIL | DTO has `int Id` property |
| **3. IdKey Routes** | Routes use `{idKey}` not `{id}` | FAIL | Route contains `{id}` |
| **4. Hook Wrapping** | Components use hooks for API calls | FAIL | Component has `apiCallsDirectly: true` |
| **5. Type Alignment** | Frontend types match backend DTOs | INFO | Generated DTO count mismatch |

## Contract Details

### Check 1: Response Envelope Documentation

**What it verifies:**
- Controllers document whether they use response envelopes
- Envelopes wrap responses: `{ data: T }` instead of raw `T`

**Why it matters:**
- Consistent API response format across all endpoints
- Clear documentation of response structure

**Passes when:**
All controllers have `response_envelope: true|false` in their `patterns` object.

**Fails when:**
Controller is missing `patterns.response_envelope` documentation.

**Example:**
```json
{
  "name": "PeopleController",
  "patterns": {
    "response_envelope": true,  // ✓ PASS
    "idkey_routes": true,
    "problem_details": true
  }
}
```

---

### Check 2: No Integer IDs in DTOs

**What it verifies:**
- DTOs never expose integer `Id` properties
- All IDs should be strings (IdKey)

**Why it matters:**
- API security model depends on encoded string IDs
- Prevents accidental exposure of internal ID sequences

**Passes when:**
No DTO has a property named `Id` with integer type.

**Fails when:**
Any DTO has `"Id": "int"` or `"Id": "int?"` in properties.

**Example:**
```csharp
// ✗ FAIL
public class PersonDto {
    public int Id { get; set; }
}

// ✓ PASS
public class PersonDto {
    public string IdKey { get; set; }
}
```

---

### Check 3: IdKey Routes

**What it verifies:**
- API routes use `{idKey}` parameter, not `{id}`
- Consistent with IdKey security model

**Why it matters:**
- Routes accept encoded string IDs only
- Prevents exposure of integer ID patterns
- Aligns with controller parameter names

**Passes when:**
No endpoint route contains `{id}`.

**Fails when:**
Any endpoint route has `{id}` instead of `{idKey}`.

**Example:**
```
// ✗ FAIL
GET /api/v1/people/{id}

// ✓ PASS
GET /api/v1/people/{idKey}
```

---

### Check 4: Hook Wrapping

**What it verifies:**
- React components don't directly call fetch/apiClient
- All API calls go through custom hooks
- Hooks manage caching and error handling

**Why it matters:**
- **Testability:** Mock hooks instead of API layer
- **Caching:** TanStack Query prevents duplicate requests
- **Error handling:** Centralized error strategies
- **Type safety:** Hooks provide typed responses

**Passes when:**
No component has `apiCallsDirectly: true`.

**Fails when:**
A component directly imports and calls API functions instead of using hooks.

**Example:**
```typescript
// ✗ FAIL
import { getPersonById } from '@/services/api/people';
getPersonById(idKey).then(setPerson);

// ✓ PASS
import { usePerson } from '@/hooks/usePerson';
const { data: person } = usePerson(idKey);
```

---

### Check 5: Type Alignment

**What it verifies:**
- Frontend TypeScript types correspond to backend DTOs
- Field names and types align across layers

**Why it matters:**
- Prevents type mismatches between API and UI
- Ensures data contracts are maintained
- Catches missing or renamed fields

**Status:**
Currently informational. Full enforcement pending completion of frontend type generation system.

**Example (future):**
```
Backend DTO: PersonDto
  - IdKey: string
  - FirstName: string
  
Frontend Type: Person
  - idKey: string
  - firstName: string
  
Result: ALIGNED ✓
```

---

## Exit Codes

| Code | Meaning | Action |
|------|---------|--------|
| 0 | All checks passed | No action needed |
| 1 | Contract violations found | Review violations and fix |
| 2 | Script error | Check input file and syntax |

## Usage Patterns

### Manual Verification

```bash
# From project root
python3 tools/graph/verify-contracts.py

# With custom graph file
python3 tools/graph/verify-contracts.py path/to/graph.json
```

### In CI Pipeline

```yaml
name: Verify Contracts

on: [push, pull_request]

jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Verify API Contracts
        run: python3 tools/graph/verify-contracts.py
        
      - name: Report Violations
        if: failure()
        run: |
          echo "Contract violations detected"
          echo "See VERIFY-CONTRACTS.md for fixes"
          exit 1
```

### In Pre-commit Hook

```bash
#!/bin/bash
# .husky/pre-commit

echo "Checking API contracts..."
if ! python3 tools/graph/verify-contracts.py; then
  echo "Fix contract violations before committing"
  exit 1
fi
```

### In Development Workflow

After making architectural changes:

```bash
# 1. Make changes (add DTO, controller, component, etc)
# 2. Regenerate graph baseline
npm run graph:update

# 3. Verify contracts
python3 tools/graph/verify-contracts.py

# 4. If violations found, fix them (see VERIFY-CONTRACTS.md)
# 5. Commit
git add tools/graph/graph-baseline.json
git commit -m "chore: update architecture graph"
```

## Implementation Details

The script is written in Python 3 and:
- Loads `graph-baseline.json`
- Validates JSON structure
- Runs 5 independent contract checks
- Reports violations with line/item details
- Exits with standard exit codes

**Performance:** ~50ms on baseline with 77 DTOs and 22 controllers.

**Dependencies:** Python 3.6+ (stdlib only, no external packages)

## Troubleshooting

### Script not found

Make sure you're in the project root:
```bash
cd /path/to/koinon-rms
python3 tools/graph/verify-contracts.py
```

### "Graph file not found"

Verify the file exists:
```bash
ls -l tools/graph/graph-baseline.json
```

If missing, regenerate:
```bash
npm run graph:update
```

### "Invalid JSON in graph"

The baseline file is corrupted. Regenerate:
```bash
npm run graph:update
```

### False positives / "Fixed but still failing"

Regenerate the baseline after fixes:
```bash
npm run graph:update
python3 tools/graph/verify-contracts.py  # Should pass now
```

## Related Documentation

- **Detailed Check Docs:** See `VERIFY-CONTRACTS.md`
- **Fix Examples:** See `VERIFY-CONTRACTS-EXAMPLES.md`
- **Graph System:** See `README.md` in same directory
- **Schema:** See `schema.json` for graph structure definition

## Future Enhancements

Planned improvements:
- [ ] JSON output mode for CI machine parsing
- [ ] Configuration file for severity customization
- [ ] Auto-fix mode for common violations
- [ ] Check 5 enforcement when frontend types generated
- [ ] Performance metrics dashboard
- [ ] Historical violation tracking

