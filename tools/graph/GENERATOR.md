# Backend Graph Generator

The `generate-backend.py` script automatically extracts the architecture of your .NET backend and generates a JSON representation of entities, DTOs, services, and controllers.

## Overview

The generator parses C# source files using regex patterns to:
- Extract domain entities from `src/Koinon.Domain/Entities/`
- Extract DTOs from `src/Koinon.Application/DTOs/`
- Extract services from `src/Koinon.Application/Services/` and interfaces
- Extract controllers from `src/Koinon.Api/Controllers/`
- Build relationship edges between components

Output: `tools/graph/backend-graph.json` (compatible with schema.json)

## Usage

### Basic Invocation

```bash
python3 tools/graph/generate-backend.py
```

### With Custom Output Path

```bash
python3 tools/graph/generate-backend.py --output custom/path/graph.json
```

### With Custom Project Root

```bash
python3 tools/graph/generate-backend.py --project-root /path/to/project
```

## Output Structure

The generated JSON includes:

```json
{
  "version": "1.0",
  "generated_at": "ISO timestamp",
  "entities": {
    "Person": {
      "name": "Person",
      "namespace": "Koinon.Domain.Entities",
      "table": "person",
      "properties": {
        "FirstName": "string",
        "LastName": "string"
      },
      "navigations": [
        {
          "name": "PhoneNumbers",
          "target_entity": "PhoneNumber",
          "type": "many"
        }
      ]
    }
  },
  "dtos": {
    "PersonDto": {
      "name": "PersonDto",
      "namespace": "Koinon.Application.DTOs",
      "properties": {
        "IdKey": "string",
        "FirstName": "string"
      },
      "linked_entity": "Person"
    }
  },
  "services": {
    "PersonService": {
      "name": "PersonService",
      "namespace": "Koinon.Application.Services",
      "methods": [
        {
          "name": "GetByIdKeyAsync",
          "return_type": "Task<PersonDto>",
          "is_async": true
        }
      ],
      "dependencies": [
        "IPersonRepository",
        "IMapper"
      ]
    }
  },
  "controllers": {
    "PeopleController": {
      "name": "PeopleController",
      "namespace": "Koinon.Api.Controllers",
      "route": "api/v1/people",
      "endpoints": [
        {
          "name": "GetByIdKey",
          "method": "GET",
          "route": "{idKey}",
          "request_type": null,
          "response_type": "PersonDto",
          "requires_auth": true,
          "required_roles": []
        }
      ],
      "patterns": {
        "response_envelope": true,
        "idkey_routes": true,
        "problem_details": true,
        "result_pattern": false
      },
      "dependencies": [
        "IPersonService",
        "IFileService"
      ]
    }
  },
  "edges": [
    {
      "source": "PersonDto",
      "target": "Person",
      "relationship": "maps_to"
    },
    {
      "source": "PeopleController",
      "target": "PersonService",
      "relationship": "depends_on"
    }
  ],
  "summary": {
    "total_entities": 41,
    "total_dtos": 77,
    "total_services": 69,
    "total_controllers": 22,
    "total_relationships": 202
  }
}
```

## What Gets Extracted

### Entities
- Class name (from file name or class declaration)
- Namespace (from `namespace` declaration)
- Table name (from `[Table]` attribute or snake_case conversion of class name)
- Properties (all `public` properties with types)
- Navigation properties (relationships to other entities)

### DTOs
- Class name (any class ending with `Dto`)
- Namespace (C# namespace)
- Properties (all `public` properties)
- Linked entity (inferred from DTO name, e.g., `PersonDto` → `Person`)

### Services
- Class name (any class ending with `Service`)
- Namespace (from `Koinon.Application.Services` or `Koinon.Application.Interfaces`)
- Public methods (name, return type, async flag)
- Constructor dependencies (primary or traditional constructor parameters)

### Controllers
- Class name (any class ending with `Controller`)
- Namespace (from `Koinon.Api.Controllers`)
- Route (from `[Route]` attribute or inferred from class name)
- HTTP endpoints (for each `[HttpGet]`, `[HttpPost]`, etc.)
- Architectural patterns detected:
  - `response_envelope`: Uses `new { data = ... }` pattern
  - `idkey_routes`: Uses `{idKey}` in routes
  - `problem_details`: Uses `Problem()` or `ProblemDetails`
  - `result_pattern`: Uses `Result<T>` pattern
- Constructor dependencies

### Relationships (Edges)
- DTO → Entity: `maps_to` relationship (inferred from naming)
- Controller → Service: `depends_on` relationship (from constructor)
- Service → DTO: `returns` relationship (from method return types)
- Service → Entity: `uses` relationship (from dependencies)

## Parsing Strategy

The parser uses **regex patterns** to avoid external dependencies:

1. **File-based identification**: Class names extracted from file names (more reliable than parsing)
2. **Property extraction**: Regex pattern for `public Type PropertyName`
3. **Navigation detection**: Pattern for `ICollection<T>` and single references
4. **Primary constructor support**: Handles `.NET 8` primary constructor syntax
5. **Generic type handling**: Respects nested angle brackets in types
6. **Multiple DTOs per file**: Searches for all `...Dto` classes in a file

### Known Limitations

- Doesn't track method-level parameter types (only constructor parameters)
- Doesn't extract request/response types from endpoint signatures (set to null)
- Navigation properties only detected for collections and capitalized single types
- Doesn't handle nested classes or partial class definitions well

## Integration with CI/CD

The graph baseline validation system in CI detects changes:

```yaml
# In .github/workflows/graph-validate.yml
- name: Generate Backend Graph
  run: python3 tools/graph/generate-backend.py
  
- name: Validate Graph
  run: npm run graph:validate
```

When structural changes are detected, CI requires updating the baseline:

```bash
npm run graph:update
git add tools/graph/graph-baseline.json
git commit -m "docs: update graph baseline"
```

## Performance

On Koinon RMS:
- Runtime: < 1 second
- Generates: ~200 relationship edges
- ~300 lines of clean Python code

## Development

To add new extraction logic:

1. Add method to `CSharpParser` class
2. Call method in `BackendGraphGenerator.process_*()` functions
3. Test with sample files
4. Update this documentation

Example: Extracting custom attributes

```python
def extract_custom_attribute(self, content: str) -> Optional[str]:
    match = re.search(r'\[MyAttribute\s*\(\s*["\']([^"\']+)["\']', content)
    return match.group(1) if match else None
```

## Troubleshooting

### "AttributeError: 'NoneType' object has no attribute..."

Check that source files exist at expected paths:
- `src/Koinon.Domain/Entities/`
- `src/Koinon.Application/DTOs/`
- `src/Koinon.Application/Services/`
- `src/Koinon.Api/Controllers/`

### "JSON validation failed"

Run the script with stderr logging:
```bash
python3 tools/graph/generate-backend.py 2>&1 | grep Warning
jq . tools/graph/backend-graph.json  # Shows JSON errors
```

### "Found 0 entities/DTOs/services"

Check file naming:
- Entity files: `ClassName.cs` in Entities directory
- DTO files: `*Dto.cs` in DTOs directory
- Service files: `*Service.cs` in Services directory
- Controller files: `*Controller.cs` in Controllers directory

### Graph doesn't match expected structure

Compare with schema:
```bash
python3 -m jsonschema tools/graph/schema.json tools/graph/backend-graph.json
```

## Related Files

- `schema.json` - JSON Schema that validates output
- `graph-baseline.json` - Approved snapshot for CI validation
- `README.md` - User-facing documentation
- `.github/workflows/graph-validate.yml` - CI integration
