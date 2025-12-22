# Contract Verification Script

The `verify-contracts.py` script performs consistency checks on the architecture graph baseline to ensure API contracts, DTO design, and component architecture remain aligned across layers.

## Quick Start

```bash
# Run verification against the baseline
python3 tools/graph/verify-contracts.py

# Or specify a custom graph file
python3 tools/graph/verify-contracts.py path/to/graph.json
```

**Exit codes:**
- `0` - All checks passed
- `1` - One or more blocking checks failed
- `2` - Script error (missing file, invalid JSON, etc.)

## The 5 Contract Checks

### Check 1: Response Envelope Documentation
**Status:** Blocking failure  
**What it checks:** Controllers declare their response envelope pattern

Controllers must document whether they use:
- **Envelope pattern:** `{ data: T }` responses
- **Direct pattern:** Raw entity/DTO responses

Each controller must have a `response_envelope` boolean in its `patterns` object.

**Example:**
```json
{
  "name": "PeopleController",
  "patterns": {
    "response_envelope": true,  // Must be documented
    "idkey_routes": true,
    "problem_details": true
  }
}
```

**When it fails:**
- Controller is missing `patterns.response_envelope` documentation
- Indicates gaps in API contract documentation

**How to fix:**
1. Update `tools/graph/graph-baseline.json` in the controller's `patterns` object
2. Or regenerate baseline: `npm run graph:update`

---

### Check 2: No Integer IDs in DTOs
**Status:** Blocking failure  
**What it checks:** DTOs never expose integer ID properties

DTOs should use `IdKey` (string) for ID exposure, never `int Id`.

**Why:** The API security model encodes integer IDs as strings using IdKey. Exposing raw integer IDs breaks encapsulation and can leak information about system scale.

**Pattern violation:**
```csharp
// WRONG - exposes integer ID
public class PersonDto
{
    public int Id { get; set; }  // ✗ FAIL
    public string Name { get; set; }
}

// CORRECT - uses IdKey
public class PersonDto
{
    public string IdKey { get; set; }  // ✓ PASS
    public string Name { get; set; }
}
```

**When it fails:**
- Any DTO has a property named `Id` with type containing `int`

**How to fix:**
1. Remove the integer `Id` property from the DTO
2. Use `IdKey` (string) instead
3. Update graph baseline: `npm run graph:update`

---

### Check 3: IdKey Routes
**Status:** Blocking failure  
**What it checks:** API routes use `{idKey}` not `{id}`

All parameterized routes must use `{idKey}` in the route pattern.

**Pattern violation:**
```
GET /api/v1/people/{id}        // ✗ FAIL - uses {id}
GET /api/v1/people/{idKey}     // ✓ PASS - uses {idKey}
```

**When it fails:**
- Any endpoint route contains `{id}`

**How to fix:**
1. Update controller route attributes:
   ```csharp
   [HttpGet("{idKey}")]
   public async Task<IActionResult> GetById(string idKey) { ... }
   ```
2. Update graph baseline: `npm run graph:update`

**Note:** Base routes like `api/v1/people` don't need parameters and won't trigger this check.

---

### Check 4: Hook Wrapping
**Status:** Blocking failure  
**What it checks:** Components use hooks to wrap API calls

React components must NOT directly import or call `fetch`, `apiClient`, or API functions. All API communication goes through custom hooks.

**Pattern violation:**
```typescript
// WRONG - direct API call
import { getPersonById } from '@/services/api/people';

export function PersonCard({ personIdKey }: Props) {
  const [person, setPerson] = useState(null);
  
  useEffect(() => {
    getPersonById(personIdKey).then(setPerson);  // ✗ FAIL
  }, [personIdKey]);
}

// CORRECT - hook wrapper
import { usePersonById } from '@/hooks/usePeople';

export function PersonCard({ personIdKey }: Props) {
  const { data: person } = usePersonById(personIdKey);  // ✓ PASS
}
```

**When it fails:**
- A component has `apiCallsDirectly: true` in the graph

**How to fix:**
1. Create a custom hook in `src/web/src/hooks/` that wraps the API call:
   ```typescript
   // hooks/usePeople.ts
   import { useQuery } from '@tanstack/react-query';
   import { getPersonById } from '@/services/api/people';
   
   export function usePersonById(idKey: string) {
     return useQuery({
       queryKey: ['people', idKey],
       queryFn: () => getPersonById(idKey),
     });
   }
   ```
2. Update component to use hook instead of direct API call
3. Update graph baseline: `npm run graph:update`

**Why this matters:**
- **Testability:** Hooks can be mocked in tests
- **Caching:** TanStack Query provides automatic caching across components
- **Error handling:** Centralized error handling in hooks
- **Performance:** Prevents duplicate requests to the same data

---

### Check 5: Type Alignment
**Status:** Informational (not yet enforced)  
**What it checks:** Frontend types match backend DTOs

This check validates that TypeScript types exist for each backend DTO. Implementation pending completion of frontend type generation system.

**Future validation:**
- Backend DTO `PersonDto` → Frontend type `Person` or `PersonResponse`
- Mismatch warnings when frontend types are missing
- Type signature alignment between layers

---

## Usage in CI/CD

The contract verification can be integrated into CI workflows:

```bash
# Fail build if contracts are violated
if ! python3 tools/graph/verify-contracts.py; then
  echo "Contract verification failed"
  exit 1
fi
```

## Debugging Violations

### Finding the problematic file

Most violations include the component or controller name. To locate the source:

```bash
# Find a controller
grep -r "class MyController" src/Koinon.Api/Controllers/

# Find a DTO
grep -r "class MyDto" src/Koinon.Application/DTOs/

# Find a component
grep -r "export function MyComponent" src/web/src/
```

### Updating the graph baseline

After fixing violations:

```bash
# Regenerate baseline to capture fixes
npm run graph:update

# Verify no regressions
npm run graph:validate
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All contract checks passed |
| 1 | One or more blocking violations detected |
| 2 | Script error (missing file, corrupt JSON, etc.) |

## Related Documentation

- **Architecture:** See `CLAUDE.md` section "Graph Baseline System"
- **Graph Schema:** See `schema.json` for complete data structure definition
- **Graph Generation:** See `GENERATOR.md` for how baselines are created
- **CI Integration:** See `.github/workflows/graph-validate.yml`

## Troubleshooting

### "Graph file not found"
Make sure you're running from the repository root or provide the correct path:
```bash
python3 tools/graph/verify-contracts.py tools/graph/graph-baseline.json
```

### "Invalid JSON in graph"
The graph file is corrupted. Regenerate it:
```bash
npm run graph:update
```

### Script exits with code 2
Check that all required sections exist in the graph:
- `controllers` - API controllers
- `dtos` - Data transfer objects
- `components` - React components
- `hooks` - React hooks

These are generated by `npm run graph:update`.

## Implementation Details

The script:
1. Loads `graph-baseline.json`
2. Validates JSON structure
3. Runs 5 contract verification checks
4. Reports violations with specific details
5. Exits with appropriate code

**Performance:** Typical run completes in <100ms on current baseline (~77 DTOs, 22 controllers).

## Future Enhancements

Planned additions:
- [ ] Check 5: Type alignment enforcement when frontend types available
- [ ] Configuration file for customizing severity levels
- [ ] JSON output mode for CI integration
- [ ] Auto-fix mode for common violations
- [ ] Historical tracking of contract violations over time
