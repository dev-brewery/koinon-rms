# Contract Verification Quick Reference

## Run Verification

```bash
python3 tools/graph/verify-contracts.py
```

## Exit Codes

- **0** = All checks passed ✓
- **1** = Violations found ✗
- **2** = Script error

## The 5 Checks

### 1. Response Envelope Documentation
Controllers must document: `patterns: { response_envelope: true/false }`

**Fix:** Add missing documentation to controller in graph baseline
```bash
npm run graph:update  # Regenerate baseline
```

### 2. No Integer IDs in DTOs
DTOs must not have `int Id` property. Use `string IdKey` instead.

**Fix:**
```csharp
// Change
public int Id { get; set; }

// To
public string IdKey { get; set; }
```

### 3. IdKey Routes
Routes must use `{idKey}` not `{id}`

**Fix:**
```csharp
// Change
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)

// To
[HttpGet("{idKey}")]
public async Task<IActionResult> GetById(string idKey)
```

### 4. Hook Wrapping
Components must not call API functions directly. Use custom hooks.

**Fix:**
```typescript
// Create hook in src/web/src/hooks/
import { useQuery } from '@tanstack/react-query';
import { getPersonById } from '@/services/api/people';

export function usePerson(idKey: string) {
  return useQuery({
    queryKey: ['person', idKey],
    queryFn: () => getPersonById(idKey),
  });
}

// Use in component
const { data: person } = usePerson(idKey);
```

### 5. Type Alignment
(Informational - enforced when frontend types available)

## Workflow

1. **Make changes** (add controller, DTO, component, etc.)
2. **Verify** → `python3 tools/graph/verify-contracts.py`
3. **If fails:**
   - Review violation (see VERIFY-CONTRACTS.md)
   - Fix source code
   - Regenerate baseline → `npm run graph:update`
4. **If passes:**
   - Commit baseline update
   - PR ready

## Common Violations

| Violation | File | How to Fix |
|-----------|------|-----------|
| "response_envelope missing" | `graph-baseline.json` | Run `npm run graph:update` |
| "integer ID" | `*Dto.cs` | Replace `int Id` with `string IdKey` |
| "route uses {id}" | `*Controller.cs` | Change `{id}` to `{idKey}` |
| "direct API calls" | `*.tsx` component | Extract to hook in `hooks/` |

## Integration

### npm scripts (in package.json)
```json
{
  "scripts": {
    "graph:validate": "python3 tools/graph/verify-contracts.py"
  }
}
```

### CI (GitHub Actions)
```yaml
- name: Verify Contracts
  run: npm run graph:validate
```

### Pre-commit
```bash
python3 tools/graph/verify-contracts.py || exit 1
```

## Files

- **Script:** `tools/graph/verify-contracts.py` (executable Python 3)
- **Schema:** `tools/graph/schema.json` (data structure definition)
- **Baseline:** `tools/graph/graph-baseline.json` (snapshot to verify against)
- **Docs:** `tools/graph/VERIFY-CONTRACTS.md` (detailed guide)
- **Examples:** `tools/graph/VERIFY-CONTRACTS-EXAMPLES.md` (fix examples)

## More Information

- Full guide: `VERIFY-CONTRACTS.md`
- Examples: `VERIFY-CONTRACTS-EXAMPLES.md`
- System overview: `VERIFICATION-SYSTEM.md`
