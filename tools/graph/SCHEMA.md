# Graph Schema Documentation

## Overview

The graph schema (`schema.json`) defines the structure for Koinon RMS architecture graphs. It uses JSON Schema draft-07 to validate:

- **Baseline graphs** (`graph-baseline.json`) - approved snapshots of architecture
- **Generated graphs** (`backend-graph.json`, `frontend-graph.json`, merged `graph.json`) - current state

## Root Properties

All graph files must contain these top-level properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `version` | string | Yes | Semantic version (e.g., "1.0.0") |
| `generated_at` | string (ISO 8601) | Yes | Timestamp when graph was generated |
| `entities` | object | Yes | Domain entities by name |
| `dtos` | object | Yes | Data transfer objects by name |
| `services` | object | Yes | Application services by name |
| `controllers` | object | Yes | API controllers by name |
| `hooks` | object | Yes | React hooks by name |
| `api_functions` | object | Yes | Frontend API functions by name |
| `components` | object | Yes | React components by name |
| `edges` | array | Yes | Relationships between nodes |

## Node Definitions

### Entity

Represents domain entities from `Koinon.Domain.Entities`.

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
      "last_name": "string"
    },
    "navigations": [
      {
        "name": "Addresses",
        "target_entity": "Address",
        "type": "many"
      }
    ]
  }
}
```

**Required fields:**
- `name` - PascalCase class name
- `namespace` - Always `Koinon.Domain.Entities`
- `table` - snake_case database table name
- `properties` - Entity properties and their C# types
- `navigations` - Foreign key relationships

**Navigation type:**
- `"one"` - Single related entity (e.g., `Person ParentPerson`)
- `"many"` - Collection of related entities (e.g., `ICollection<Address> Addresses`)

### DTO

Represents data transfer objects from `Koinon.Application.DTOs`.

```json
{
  "PersonDto": {
    "name": "PersonDto",
    "namespace": "Koinon.Application.DTOs",
    "properties": {
      "id": "string",
      "firstName": "string",
      "lastName": "string"
    },
    "linked_entity": "Person"
  }
}
```

**Required fields:**
- `name` - PascalCase ending in `Dto`
- `namespace` - One of:
  - `Koinon.Application.DTOs`
  - `Koinon.Application.DTOs.Requests`
  - `Koinon.Application.DTOs.Responses`
- `properties` - DTO properties and their C# types

**Optional fields:**
- `linked_entity` - Source entity name if DTO maps directly to entity

### Service

Represents application services implementing business logic.

```json
{
  "PersonService": {
    "name": "IPersonService",
    "namespace": "Koinon.Application.Interfaces",
    "methods": [
      {
        "name": "CreatePersonAsync",
        "return_type": "Task<PersonDto>",
        "is_async": true
      }
    ],
    "dependencies": [
      "IPersonRepository",
      "IValidator<CreatePersonRequest>"
    ]
  }
}
```

**Required fields:**
- `name` - Service class or interface name (may start with `I`)
- `namespace` - `Koinon.Application.Interfaces` or `Koinon.Application.Services`
- `methods` - Public methods with name, return type, async flag
- `dependencies` - Constructor-injected dependencies

### Controller

Represents ASP.NET Core API controllers.

```json
{
  "PeopleController": {
    "name": "PeopleController",
    "namespace": "Koinon.Api.Controllers",
    "route": "api/v1/people",
    "endpoints": [
      {
        "name": "GetByIdKey",
        "method": "GET",
        "route": "{idKey}",
        "response_type": "PersonDto",
        "requires_auth": true,
        "required_roles": ["All"]
      },
      {
        "name": "Create",
        "method": "POST",
        "route": "",
        "request_type": "CreatePersonRequest",
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

**Required fields:**
- `name` - PascalCase ending in `Controller`
- `namespace` - Always `Koinon.Api.Controllers`
- `route` - Base path like `api/v1/people` (must match `/^api/v\d+/[a-z-]+$/`)
- `endpoints` - Array of HTTP endpoints
- `patterns` - Architectural patterns used

**Endpoint fields:**
- `name` - Action method name
- `method` - HTTP verb (GET, POST, PUT, PATCH, DELETE)
- `route` - Relative path (`{idKey}`, `batch`, `{idKey}/children`)
- `response_type` - Response DTO name
- `request_type` - Request DTO name (for POST/PUT/PATCH)
- `requires_auth` - Boolean (default true)
- `required_roles` - List of role names

**Pattern fields (describe coding conventions used):**
- `response_envelope` - Returns wrapped response like `new { data = ... }`
- `idkey_routes` - Uses `{idKey}` instead of `{id}` in routes
- `problem_details` - Uses `ProblemDetails` for errors
- `result_pattern` - Uses `Result<T>` wrapper type

### Hook

Represents React custom hooks using TanStack Query.

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

**Required fields:**
- `name` - camelCase starting with `use`
- `path` - Relative path like `hooks/useName.ts`
- `api_binding` - API function it calls

**Optional fields:**
- `query_key` - TanStack Query key array
- `uses_query` - Whether hook uses `useQuery`
- `uses_mutation` - Whether hook uses `useMutation`

### ApiFunction

Represents frontend API client functions.

```json
{
  "searchPersons": {
    "name": "searchPersons",
    "path": "services/api/persons.ts",
    "endpoint": "/people",
    "method": "GET",
    "response_type": "PaginatedPersonDto",
    "request_type": "PersonSearchParams"
  }
}
```

**Required fields:**
- `name` - camelCase function name
- `path` - Relative path like `services/api/people.ts`
- `endpoint` - Backend endpoint path
- `method` - HTTP verb
- `response_type` - TypeScript response type

**Optional fields:**
- `request_type` - TypeScript request type (for POST/PUT/PATCH)

### Component

Represents React components.

```json
{
  "PersonDetail": {
    "name": "PersonDetail",
    "path": "features/people/PersonDetail.tsx",
    "hooks": ["usePerson", "useUpdatePerson"],
    "is_page": true,
    "route": "/people/:idKey"
  }
}
```

**Required fields:**
- `name` - PascalCase component name
- `path` - Relative path like `components/Name.tsx` or `features/module/Name.tsx`

**Optional fields:**
- `hooks` - Custom hooks used
- `is_page` - Whether component is a route page
- `route` - Route path if page (e.g., `/people/:idKey`)

## Edges

Edges represent relationships between nodes.

```json
{
  "from": "Entity:Person",
  "to": "DTO:PersonDto",
  "type": "entity_to_dto"
}
```

**Format:**
- `from` - Source node as `NodeType:NodeName`
- `to` - Target node as `NodeType:NodeName`
- `type` - Relationship type (see below)
- `metadata` - Optional extra information

**Valid edge types:**

| Type | Source | Target | Meaning |
|------|--------|--------|---------|
| `entity_to_dto` | Entity | DTO | Entity is serialized to DTO |
| `dto_to_service` | DTO | Service | Service method uses/returns DTO |
| `service_to_controller` | Service | Controller | Controller injects service |
| `controller_to_dto` | Controller | DTO | Controller endpoint uses DTO |
| `hook_to_api` | Hook | ApiFunction | Hook calls API function |
| `component_to_hook` | Component | Hook | Component uses hook |
| `component_to_component` | Component | Component | Component uses another component |
| `api_to_api` | ApiFunction | ApiFunction | API function calls another |

## Validation Rules

The schema enforces:

### C# Naming
- Entity/DTO/Service/Controller names must be PascalCase
- Table/field names must be snake_case
- Hook/API function names must be camelCase
- Component names must be PascalCase

### Namespace Rules
- Entities must be in `Koinon.Domain.Entities`
- DTOs in `Koinon.Application.DTOs*`
- Services in `Koinon.Application.Interfaces` or `Koinon.Application.Services`
- Controllers in `Koinon.Api.Controllers`

### Route Rules
- Controller routes must match `api/v{version}/{resource}`
- All routes use `/` not `\`
- Segments are lowercase with hyphens for multi-word names

### Type Rules
- Entity properties use C# types (int, string, Guid, DateTime, etc.)
- DTO properties use C# types
- Navigation types are either "one" or "many"

## Example: Complete Graph Fragment

```json
{
  "version": "1.0.0",
  "generated_at": "2024-12-22T15:30:00Z",
  "entities": {
    "Person": {
      "name": "Person",
      "namespace": "Koinon.Domain.Entities",
      "table": "person",
      "properties": {
        "id": "int",
        "guid": "Guid",
        "first_name": "string",
        "last_name": "string"
      },
      "navigations": [
        {
          "name": "Addresses",
          "target_entity": "Address",
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
        "id": "string",
        "firstName": "string",
        "lastName": "string"
      },
      "linked_entity": "Person"
    }
  },
  "services": {},
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
          "response_type": "PersonDto",
          "requires_auth": true,
          "required_roles": []
        }
      ],
      "patterns": {
        "response_envelope": false,
        "idkey_routes": true,
        "problem_details": true,
        "result_pattern": false
      }
    }
  },
  "hooks": {
    "usePerson": {
      "name": "usePerson",
      "path": "hooks/usePerson.ts",
      "api_binding": "getPersonByIdKey",
      "query_key": ["person"],
      "uses_query": true,
      "uses_mutation": false
    }
  },
  "api_functions": {
    "getPersonByIdKey": {
      "name": "getPersonByIdKey",
      "path": "services/api/people.ts",
      "endpoint": "/people/{idKey}",
      "method": "GET",
      "response_type": "PersonDto"
    }
  },
  "components": {
    "PersonDetail": {
      "name": "PersonDetail",
      "path": "features/people/PersonDetail.tsx",
      "hooks": ["usePerson"],
      "is_page": true,
      "route": "/people/:idKey"
    }
  },
  "edges": [
    {
      "from": "Entity:Person",
      "to": "DTO:PersonDto",
      "type": "entity_to_dto"
    },
    {
      "from": "DTO:PersonDto",
      "to": "Controller:PeopleController",
      "type": "controller_to_dto"
    },
    {
      "from": "ApiFunction:getPersonByIdKey",
      "to": "DTO:PersonDto",
      "type": "api_to_dto"
    },
    {
      "from": "Hook:usePerson",
      "to": "ApiFunction:getPersonByIdKey",
      "type": "hook_to_api"
    },
    {
      "from": "Component:PersonDetail",
      "to": "Hook:usePerson",
      "type": "component_to_hook"
    }
  ]
}
```

## Related Files

- **Schema definition:** `tools/graph/schema.json` (this file)
- **Baseline snapshot:** `tools/graph/graph-baseline.json` (approved version)
- **Generator scripts:** `tools/graph/` (Python/TypeScript generators)
- **CI validation:** `.github/workflows/graph-validate.yml`
- **Architecture documentation:** `CLAUDE.md` section "Graph Baseline System"
