# Graph Schema Quick Reference

## What Gets Tracked

The graph validates **structural changes** - when you add or modify:

- **Entities** (new domain models)
- **DTOs** (data contracts)
- **Services** (business logic interfaces)
- **Controllers** (API endpoints)
- **React Hooks** (data fetching)
- **API Functions** (frontend HTTP calls)
- **Components** (UI elements)

## When to Update Baseline

Run `npm run graph:update` after:

```
✓ Adding new entity (Person.cs)
✓ Adding new DTO (PersonDto.cs)
✓ Adding new controller endpoint (POST /api/v1/people)
✓ Renaming entity field (FirstName → GivenName)
✓ Adding new React hook (usePersons.ts)
✓ Adding new API function (searchPersons())
✓ Moving components to new feature folder
```

## When to Skip Update

Don't update baseline for:

```
✗ Implementation changes in method bodies
✗ Adding private methods
✗ Adding comments/documentation
✗ Formatting/whitespace changes
✗ Adding unit tests
✗ Modifying CSS styles
```

## Validation Commands

```bash
# Check for drift without updating
npm run graph:validate

# Update baseline to match current code
npm run graph:update

# View what changed
git diff tools/graph/graph-baseline.json
```

## Common Node Identifiers

Used in edges and validation:

```
Entity:Person              # Domain entity
DTO:PersonDto             # Data transfer object
Service:IPersonService    # Application service
Controller:PeopleController # API controller
Hook:usePerson            # React hook
ApiFunction:getPersonByIdKey # Frontend API call
Component:PersonDetail    # React component
```

## Edge Types Explained

| Edge Type | Example | Meaning |
|-----------|---------|---------|
| `entity_to_dto` | Person → PersonDto | Entity is serialized as DTO |
| `dto_to_service` | PersonDto → PersonService | Service method uses DTO |
| `service_to_controller` | PersonService → PeopleController | Controller injects service |
| `controller_to_dto` | PeopleController → PersonDto | Controller endpoint response |
| `hook_to_api` | usePerson → getPersonByIdKey | Hook calls API function |
| `component_to_hook` | PersonDetail → usePerson | Component uses hook |

## Namespace Checklist

**C# Side:**
- Entities: `Koinon.Domain.Entities`
- DTOs: `Koinon.Application.DTOs` (or `.Requests`/`.Responses`)
- Services: `Koinon.Application.Interfaces` (interfaces)
- Controllers: `Koinon.Api.Controllers`

**TypeScript Side:**
- Hooks: `src/web/src/hooks/use*.ts`
- API functions: `src/web/src/services/api/*.ts`
- Components: `src/web/src/components/` or `src/web/src/features/`

## Routing Pattern Rules

**Controllers:**
```
Base route: api/v1/resource
├── GET /api/v1/people         (list)
├── GET /api/v1/people/{idKey} (get by ID)
├── POST /api/v1/people        (create)
├── PUT /api/v1/people/{idKey} (update)
└── DELETE /api/v1/people/{idKey} (delete)
```

**Never use:**
```
✗ /api/v1/people/{id}          (use idKey not id)
✗ /api/v1/Person               (lowercase resource names)
✗ /api/v1/getPerson            (use HTTP methods, not action names)
```

## Type Mappings

**C# Entity → DTO:**
```csharp
public class Person : Entity
{
    public string FirstName { get; set; }          // Property
    public virtual ICollection<Address> { ... }    // Navigation
}

public class PersonDto
{
    public string Id { get; set; }                 // IdKey (string!)
    public string FirstName { get; set; }          // Property (camelCase in DTO)
}
```

**Frontend Request/Response:**
```typescript
// Request
interface PersonSearchParams {
  page?: number;
  pageSize?: number;
}

// Response
interface PersonDto {
  id: string;        // IdKey
  firstName: string;
  lastName: string;
}
```

## Validation Errors and Fixes

### "Entity not found in DTOs"
**Error:** Entity defined but no corresponding DTO
**Fix:** Create DTO class in `Koinon.Application.DTOs`

### "DTO not used in endpoints"
**Error:** DTO defined but no controller uses it
**Fix:** Add endpoint that returns/accepts DTO

### "Endpoint missing in graph"
**Error:** Controller endpoint not captured
**Fix:** Ensure endpoint has `[HttpGet]`, `[HttpPost]`, etc.

### "Component uses undefined hook"
**Error:** Component imports hook that's not in graph
**Fix:** Ensure hook is exported from `hooks/` directory

## Schema File Locations

```
tools/graph/
├── schema.json                    # JSON Schema definition
├── SCHEMA.md                      # Detailed documentation
├── QUICK-REFERENCE.md             # This file
├── graph-baseline.json            # Approved snapshot (git tracked)
├── graph.json                     # Current merged graph (not tracked)
├── backend-graph.json             # Generated from C# (not tracked)
└── frontend-graph.json            # Generated from TypeScript (not tracked)
```

## Advanced: Custom Validation

Generators validate against schema:

```bash
# Python: Validate backend graph
python3 tools/graph/validate.py tools/graph/backend-graph.json tools/graph/schema.json

# TypeScript: Validate frontend graph
npx ts-node tools/graph/validate.ts tools/graph/frontend-graph.json tools/graph/schema.json
```

## Tips for Graph Maintenance

1. **Run after structural changes** - Don't wait for CI failures
2. **Review diffs carefully** - Graph reflects your actual structure
3. **Keep baseline in git** - Version control tracks changes over time
4. **Check edge connections** - Orphaned nodes indicate missing implementations
5. **Validate namespaces** - Wrong namespace = schema validation failure

## Integration with CI

`.github/workflows/graph-validate.yml` automatically:
- Detects baseline changes
- Validates against schema
- Labels PR if baseline needs update
- Blocks merge if baseline mismatch

**CI Label:** `baseline-update-required`

If you see this label, run:
```bash
npm run graph:update
git add tools/graph/graph-baseline.json
git commit -m "chore: update graph baseline"
git push
```
