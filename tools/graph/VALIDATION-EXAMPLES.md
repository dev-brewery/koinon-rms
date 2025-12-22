# Graph Schema Validation Examples

## Valid Examples

### Complete Node Example: Entity

```json
{
  "Person": {
    "name": "Person",
    "namespace": "Koinon.Domain.Entities",
    "table": "person",
    "properties": {
      "id": "int",
      "guid": "Guid",
      "first_name": "string",
      "last_name": "string",
      "email": "string",
      "birth_date": "DateTime?",
      "is_active": "bool"
    },
    "navigations": [
      {
        "name": "Addresses",
        "target_entity": "Address",
        "type": "many"
      },
      {
        "name": "Campus",
        "target_entity": "Campus",
        "type": "one"
      }
    ]
  }
}
```

**Valid because:**
- name: PascalCase
- namespace: Exactly `Koinon.Domain.Entities`
- table: snake_case
- properties: Valid C# types
- navigations: Correct cardinality (one/many)

### Complete Node Example: DTO

```json
{
  "CreatePersonRequest": {
    "name": "CreatePersonRequest",
    "namespace": "Koinon.Application.DTOs.Requests",
    "properties": {
      "firstName": "string",
      "lastName": "string",
      "email": "string",
      "birthDate": "DateTime?",
      "campusId": "string"
    }
  }
}
```

**Valid because:**
- name: PascalCase (ends with Request)
- namespace: Recognized DTO namespace variant
- properties: camelCase in DTO

### Complete Node Example: Controller

```json
{
  "PeopleController": {
    "name": "PeopleController",
    "namespace": "Koinon.Api.Controllers",
    "route": "api/v1/people",
    "endpoints": [
      {
        "name": "SearchPeople",
        "method": "GET",
        "route": "",
        "response_type": "PaginatedPersonDto",
        "requires_auth": true
      },
      {
        "name": "GetByIdKey",
        "method": "GET",
        "route": "{idKey}",
        "response_type": "PersonDto",
        "requires_auth": true,
        "required_roles": []
      },
      {
        "name": "Create",
        "method": "POST",
        "route": "",
        "request_type": "CreatePersonRequest",
        "response_type": "PersonDto",
        "requires_auth": true,
        "required_roles": ["Admin"]
      },
      {
        "name": "UpdateByIdKey",
        "method": "PUT",
        "route": "{idKey}",
        "request_type": "UpdatePersonRequest",
        "response_type": "PersonDto",
        "requires_auth": true,
        "required_roles": ["Admin"]
      }
    ],
    "patterns": {
      "response_envelope": false,
      "idkey_routes": true,
      "problem_details": true,
      "result_pattern": false
    }
  }
}
```

**Valid because:**
- route: Matches `api/v{version}/{resource}` pattern
- All endpoints have GET/POST/PUT/DELETE methods
- Route uses `{idKey}` not `{id}`
- Patterns accurately describe code conventions

### Complete Node Example: Hook

```json
{
  "usePersons": {
    "name": "usePersons",
    "path": "hooks/usePersons.ts",
    "api_binding": "searchPersons",
    "query_key": ["persons"],
    "uses_query": true,
    "uses_mutation": false
  }
}
```

**Valid because:**
- name: camelCase with "use" prefix
- path: Exactly `hooks/use*.ts`
- api_binding: Matches an ApiFunction
- query_key: Valid TanStack Query key

### Complete Node Example: Component

```json
{
  "PersonTable": {
    "name": "PersonTable",
    "path": "features/people/PersonTable.tsx",
    "hooks": ["usePersons"],
    "is_page": false
  },
  "PeopleList": {
    "name": "PeopleList",
    "path": "features/people/pages/PeopleList.tsx",
    "hooks": ["usePersons", "useUpdatePerson"],
    "is_page": true,
    "route": "/people"
  }
}
```

**Valid because:**
- name: PascalCase
- path: Ends in `.tsx` and in `components/` or `features/`
- hooks: Array of valid hook names
- is_page: Boolean indicating route component
- route: Matches React Router format

### Valid Edge Examples

```json
[
  {
    "from": "Entity:Person",
    "to": "DTO:PersonDto",
    "type": "entity_to_dto"
  },
  {
    "from": "DTO:PersonDto",
    "to": "Service:IPersonService",
    "type": "dto_to_service"
  },
  {
    "from": "Service:IPersonService",
    "to": "Controller:PeopleController",
    "type": "service_to_controller"
  },
  {
    "from": "Hook:usePersons",
    "to": "ApiFunction:searchPersons",
    "type": "hook_to_api"
  },
  {
    "from": "Component:PersonTable",
    "to": "Hook:usePersons",
    "type": "component_to_hook"
  }
]
```

**Valid because:**
- from/to: Follow `NodeType:NodeName` format
- type: Valid edge type
- References match defined nodes

## Invalid Examples

### Invalid Entity - Wrong Namespace

```json
{
  "Person": {
    "name": "Person",
    "namespace": "Koinon.Domain.Models",  // ✗ INVALID
    "table": "person",
    "properties": { "id": "int" },
    "navigations": []
  }
}
```

**Error:** Entity namespace must be `Koinon.Domain.Entities`

### Invalid DTO - ID Field Exposed

```json
{
  "PersonDto": {
    "name": "PersonDto",
    "namespace": "Koinon.Application.DTOs",
    "properties": {
      "id": "int",  // ✗ INVALID - should be string for IdKey
      "firstName": "string"
    }
  }
}
```

**Error:** DTOs should use `id: string` for IdKey, never `int`

### Invalid Controller - Wrong Route Format

```json
{
  "PeopleController": {
    "name": "PeopleController",
    "namespace": "Koinon.Api.Controllers",
    "route": "api/Person",  // ✗ INVALID
    "endpoints": [],
    "patterns": { ... }
  }
}
```

**Error:** Route must be `api/v{version}/{lowercase}`, e.g., `api/v1/people`

### Invalid Controller - ID in Route

```json
{
  "PeopleController": {
    "name": "PeopleController",
    "namespace": "Koinon.Api.Controllers",
    "route": "api/v1/people",
    "endpoints": [
      {
        "name": "GetById",
        "method": "GET",
        "route": "{id}",  // ✗ INVALID - use {idKey}
        "response_type": "PersonDto"
      }
    ],
    "patterns": { ... }
  }
}
```

**Error:** Routes must use `{idKey}`, never `{id}`

### Invalid Hook - Not in hooks Directory

```json
{
  "usePersons": {
    "name": "usePersons",
    "path": "services/api/usePersons.ts",  // ✗ INVALID
    "api_binding": "searchPersons",
    "query_key": ["persons"],
    "uses_query": true,
    "uses_mutation": false
  }
}
```

**Error:** Hook path must match `hooks/use*.ts`

### Invalid Component - Not in Component Directory

```json
{
  "PersonTable": {
    "name": "PersonTable",
    "path": "PersonTable.tsx",  // ✗ INVALID
    "hooks": []
  }
}
```

**Error:** Component path must be in `components/` or `features/`

### Invalid Edge - Incorrect Type

```json
{
  "from": "Entity:Person",
  "to": "DTO:PersonDto",
  "type": "entity_to_service"  // ✗ INVALID
}
```

**Error:** Valid edge types for Entity→DTO are only `entity_to_dto`

## Validation Process

### Manual Validation

```bash
# Validate schema is well-formed
jq . tools/graph/schema.json > /dev/null && echo "Valid JSON Schema"

# Validate graph against schema (Python)
python3 -m jsonschema -i tools/graph/graph.json tools/graph/schema.json
```

### Automated Validation

CI runs validation on every PR:

```yaml
# .github/workflows/graph-validate.yml
- name: Validate graph against schema
  run: npm run graph:validate
```

### Common Validation Failures

| Error | Cause | Fix |
|-------|-------|-----|
| `"namespace" does not match pattern` | Wrong C# namespace | Check entity is in `Koinon.Domain.Entities` |
| `"route" does not match pattern` | Invalid route format | Use `api/v{version}/{resource}` |
| `"name" does not match pattern` | Wrong naming convention | PascalCase entities, camelCase hooks |
| `Additional properties are not allowed` | Extra fields in object | Check object structure matches schema |
| `"type" is not one of` | Invalid edge type | Use valid edge type from enum |

## Testing Your Schema Changes

If you modify `schema.json`:

1. Validate schema itself:
   ```bash
   python3 -c "import json; from jsonschema import Draft7Validator; \
     Draft7Validator.check_schema(json.load(open('tools/graph/schema.json')))"
   ```

2. Test against baseline:
   ```bash
   python3 -m jsonschema -i tools/graph/graph-baseline.json tools/graph/schema.json
   ```

3. Test against generated graph:
   ```bash
   python3 -m jsonschema -i tools/graph/graph.json tools/graph/schema.json
   ```

4. Run full validation:
   ```bash
   npm run graph:validate
   ```
