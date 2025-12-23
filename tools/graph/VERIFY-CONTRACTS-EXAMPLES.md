# Contract Verification Examples

This document shows real examples of contract violations and how to fix them.

## Check 1: Response Envelope Documentation

### Violation Example

```json
{
  "name": "PeopleController",
  "namespace": "Koinon.Api.Controllers",
  "route": "api/v1/people",
  "endpoints": [...],
  "patterns": {
    // ✗ MISSING: response_envelope not documented
    "idkey_routes": true,
    "problem_details": true
  }
}
```

### Fix

Update the `patterns` object to include `response_envelope`:

```json
{
  "patterns": {
    "response_envelope": true,  // ✓ Now documented
    "idkey_routes": true,
    "problem_details": true
  }
}
```

---

## Check 2: No Integer IDs in DTOs

### Violation Example

```csharp
namespace Koinon.Application.DTOs;

public class PersonDto
{
    public int Id { get; set; }  // ✗ FAIL - integer ID exposed
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

Graph representation:
```json
{
  "PersonDto": {
    "name": "PersonDto",
    "namespace": "Koinon.Application.DTOs",
    "properties": {
      "Id": "int",  // ✗ FAIL - should not be integer
      "FirstName": "string",
      "LastName": "string"
    }
  }
}
```

### Fix

Replace integer `Id` with `IdKey` string:

```csharp
namespace Koinon.Application.DTOs;

public class PersonDto
{
    public string IdKey { get; set; }  // ✓ PASS - string ID
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

Graph representation:
```json
{
  "PersonDto": {
    "name": "PersonDto",
    "namespace": "Koinon.Application.DTOs",
    "properties": {
      "IdKey": "string",  // ✓ PASS
      "FirstName": "string",
      "LastName": "string"
    }
  }
}
```

---

## Check 3: IdKey Routes

### Violation Example - Integer ID in Route

```csharp
[ApiController]
[Route("api/v1/people")]
public class PeopleController : ControllerBase
{
    [HttpGet("{id}")]  // ✗ FAIL - uses {id}
    public async Task<IActionResult> GetById(int id)
    {
        var person = await _service.GetByIdAsync(id);
        return Ok(person);
    }
}
```

Graph representation:
```json
{
  "endpoints": [
    {
      "name": "GetById",
      "method": "GET",
      "route": "{id}",  // ✗ FAIL - should be {idKey}
      "response_type": "PersonDto"
    }
  ]
}
```

### Fix - Use idKey Parameter

```csharp
[ApiController]
[Route("api/v1/people")]
public class PeopleController : ControllerBase
{
    [HttpGet("{idKey}")]  // ✓ PASS - uses {idKey}
    public async Task<IActionResult> GetById(string idKey)
    {
        var id = IdKeyHelper.Decode(idKey);
        var person = await _service.GetByIdAsync(id);
        return Ok(new { data = person });
    }
}
```

Graph representation:
```json
{
  "endpoints": [
    {
      "name": "GetById",
      "method": "GET",
      "route": "{idKey}",  // ✓ PASS
      "response_type": "PersonDto"
    }
  ]
}
```

---

## Check 4: Hook Wrapping

### Violation Example - Direct API Call

```typescript
// src/web/src/components/PersonCard.tsx
import { getPersonById } from '@/services/api/people';  // Direct import

interface Props {
  personIdKey: string;
}

export function PersonCard({ personIdKey }: Props) {
  const [person, setPerson] = useState<Person | null>(null);
  const [loading, setLoading] = useState(false);
  
  useEffect(() => {
    setLoading(true);
    getPersonById(personIdKey)  // ✗ FAIL - direct API call
      .then(setPerson)
      .finally(() => setLoading(false));
  }, [personIdKey]);
  
  if (loading) return <div>Loading...</div>;
  if (!person) return <div>Not found</div>;
  
  return (
    <div>
      <h3>{person.firstName} {person.lastName}</h3>
      <p>IdKey: {person.idKey}</p>
    </div>
  );
}
```

Graph representation:
```json
{
  "PersonCard": {
    "name": "PersonCard",
    "path": "components/PersonCard.tsx",
    "apiCallsDirectly": true,  // ✗ FAIL
    "hooksUsed": []
  }
}
```

### Fix - Extract to Hook

Create a custom hook:

```typescript
// src/web/src/hooks/usePerson.ts
import { useQuery } from '@tanstack/react-query';
import { getPersonById } from '@/services/api/people';
import type { Person } from '@/types';

export function usePerson(idKey: string) {
  return useQuery<Person>({
    queryKey: ['person', idKey],
    queryFn: () => getPersonById(idKey),
    enabled: Boolean(idKey),
  });
}
```

Update component to use hook:

```typescript
// src/web/src/components/PersonCard.tsx
import { usePerson } from '@/hooks/usePerson';  // Use hook

interface Props {
  personIdKey: string;
}

export function PersonCard({ personIdKey }: Props) {
  const { data: person, isLoading } = usePerson(personIdKey);  // ✓ PASS
  
  if (isLoading) return <div>Loading...</div>;
  if (!person) return <div>Not found</div>;
  
  return (
    <div>
      <h3>{person.firstName} {person.lastName}</h3>
      <p>IdKey: {person.idKey}</p>
    </div>
  );
}
```

Graph representation:
```json
{
  "PersonCard": {
    "name": "PersonCard",
    "path": "components/PersonCard.tsx",
    "apiCallsDirectly": false,  // ✓ PASS
    "hooksUsed": ["usePerson"]
  }
}
```

Benefits:
- ✓ Query caching across components
- ✓ Automatic error handling
- ✓ Easy to mock in tests
- ✓ Loading state management

---

## Check 5: Type Alignment

### Example (Future Implementation)

When frontend type generation is complete, this check will verify:

```typescript
// Backend DTO
class PersonDto {
  IdKey: string;
  FirstName: string;
  LastName: string;
}

// Expected frontend type
type Person = {
  idKey: string;
  firstName: string;
  lastName: string;
};
```

The verification will ensure that for each backend DTO, a corresponding frontend type exists with matching fields (adjusted for camelCase convention).

---

## Running Verifications

### Run all checks
```bash
python3 tools/graph/verify-contracts.py
```

### Run with custom graph file
```bash
python3 tools/graph/verify-contracts.py /path/to/custom-graph.json
```

### Check exit code in scripts
```bash
if python3 tools/graph/verify-contracts.py; then
  echo "All contracts verified"
else
  echo "Contract violations found"
  exit 1
fi
```

---

## Integration with Development Workflow

### In CI Pipeline

```yaml
- name: Verify API Contracts
  run: python3 tools/graph/verify-contracts.py
  
- name: Update Graph Baseline
  if: failure()
  run: |
    npm run graph:update
    git diff tools/graph/graph-baseline.json
```

### In Pre-commit Hook

```bash
#!/bin/bash
# .husky/pre-commit

echo "Verifying API contracts..."
python3 tools/graph/verify-contracts.py || exit 1
```

### In PR Validation

```bash
# Ensure no new violations introduced
python3 tools/graph/verify-contracts.py
baseline_violations=$?

# Allow intentional baseline updates
if [ $baseline_violations -ne 0 ]; then
  if git diff --name-only | grep -q "graph-baseline.json"; then
    echo "Baseline updated intentionally - proceeding"
  else
    echo "Contract violations without baseline update"
    exit 1
  fi
fi
```

