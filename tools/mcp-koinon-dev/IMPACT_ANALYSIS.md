# Impact Analysis Tool - MCP Server Extension

## Overview

The `get_impact_analysis` tool extends the Koinon RMS development MCP server with comprehensive impact analysis capabilities. It traces dependencies across architectural layers and work units when a file is modified, showing:

1. All affected files across the system
2. Cross-layer dependencies
3. Mapped work units that may be impacted
4. High-impact analysis (files, layers affected)

## Architecture

### Components Added

1. **ImpactAnalysisResult Interface**
   - `affected_files[]` - Array of impacted files with layer and relationship
   - `affected_work_units[]` - Work units that require review
   - `impact_summary` - High-level impact metrics

2. **FileAnalysis Interface**
   - Path and layer identification
   - Entity/DTO/Service/Controller names
   - Component/Hook/API function names

3. **Core Functions**
   - `analyzeFileImpact()` - Main analysis orchestrator
   - `parseFilePath()` - Layer detection
   - `findFrontendConnections()` - Entity→DTO→Component tracing
   - `findFrontendConnectionsForDto()` - DTO→API→Component tracing
   - `findFrontendConnectionsForController()` - Controller→API→Component tracing

## Layer Detection

The tool automatically detects file types and layers:

| Path Pattern | Layer | Type |
|--------------|-------|------|
| `src/Koinon.Domain/Entities/*.cs` | Domain | Entity |
| `src/Koinon.Application/DTOs/*.cs` | Application | DTO |
| `src/Koinon.Application/Services/*.cs` | Application | Service |
| `src/Koinon.Api/Controllers/*.cs` | Api | Controller |
| `src/web/src/services/api/*.ts` | Frontend | API Function |
| `src/web/src/hooks/use*.ts` | Frontend | Hook |
| `src/web/src/components/*.tsx` | Frontend | Component |

## Impact Tracing Logic

### Domain Entity Changes
```
Entity (Person.cs)
    ↓ (linked_entity)
  DTOs (PersonDto, PersonDetailDto)
    ↓ (endpoints use)
  Controllers (PersonController)
    ↓ (returns DTOs)
  API Functions (getPersons, getPerson)
    ↓ (called by)
  Hooks (usePersons, usePerson)
    ↓ (used by)
  Components (PersonList, PersonDetail)
```

Work Units Affected: WU-1.2.Entity, WU-2.Service, WU-3.Controller, WU-4.Components

### DTO Changes
```
DTO (PersonDto.cs)
    ↓ (used in endpoints)
  Controllers
    ↓ (calls)
  API Functions
    ↓ (used by)
  Hooks
    ↓ (used by)
  Components
```

Work Units Affected: WU-3.Controller, WU-4.Components

### Controller Changes
```
Controller
    ↓ (has endpoints)
  API Functions
    ↓ (called by)
  Hooks
    ↓ (used by)
  Components
```

Work Units Affected: WU-4.Components

### Frontend Changes
```
Component/Hook/API Function
    ↓ (uses via edges)
  Dependencies
```

## Work Unit Mapping

Impact analysis automatically maps affected files to work units:

| File Type | WU Pattern | Example |
|-----------|-----------|---------|
| Domain Entity | WU-1.2.{EntityName} | WU-1.2.Person |
| Service | WU-2.{ServiceName} | WU-2.PersonService |
| Controller | WU-3.{ControllerName} | WU-3.PersonController |
| Frontend | WU-4.{ComponentName} | WU-4.PersonList |

## Usage Examples

### Analyze Entity Changes
```json
{
  "file_path": "src/Koinon.Domain/Entities/Person.cs"
}
```

Response:
```json
{
  "affected_files": [
    {
      "path": "src/Koinon.Domain/Entities/Person.cs",
      "layer": "Domain",
      "relationship": "source"
    },
    {
      "path": "src/Koinon.Application/DTOs/PersonDto.cs",
      "layer": "Application",
      "relationship": "dependent_on_Person"
    },
    {
      "path": "src/Koinon.Api/Controllers/PersonController.cs",
      "layer": "Api",
      "relationship": "serves_Person"
    },
    {
      "path": "src/web/src/components/PersonList.tsx",
      "layer": "Frontend",
      "relationship": "related"
    }
  ],
  "affected_work_units": [
    {
      "id": "WU-1.2.Person",
      "name": "Entity: Person",
      "reason": "Domain entity definition"
    },
    {
      "id": "WU-2.PersonService",
      "name": "Service: PersonService",
      "reason": "Service references Person entity"
    },
    {
      "id": "WU-3.PersonController",
      "name": "Controller: PersonController",
      "reason": "Controller exposes Person through API endpoints"
    },
    {
      "id": "WU-4.PersonList",
      "name": "Frontend: PersonList",
      "reason": "Component/hook uses API connected to Person"
    }
  ],
  "impact_summary": {
    "total_files": 4,
    "high_impact": false,
    "layers_affected": ["Domain", "Application", "Api", "Frontend"]
  }
}
```

### High-Impact Detection
- `high_impact: true` when:
  - More than 5 affected files
  - Changes span more than 2 layers

## Integration with Graph Baseline

The tool relies on the API graph baseline (`tools/graph/graph-baseline.json`) which tracks:

- **Entities** - Domain model definitions with linked DTOs
- **DTOs** - Application data transfer objects with `linked_entity`
- **Services** - Business logic with method signatures and return types
- **Controllers** - API endpoints with request/response types
- **API Functions** - Frontend API calls with endpoints and return types
- **Hooks** - React hooks with dependencies
- **Components** - React components with used hooks
- **Edges** - Relationships between all elements (uses, calls, implements)

## Best Practices

1. **Before modifying domain entities**
   - Run `get_impact_analysis` to understand downstream impacts
   - Review all affected DTOs, services, and controllers
   - Plan UI component updates

2. **When updating DTOs**
   - Check controller endpoints that use them
   - Verify frontend API functions still match
   - Review hook implementations

3. **When changing controllers**
   - Verify API function signatures match
   - Check all dependent hooks
   - Review component implementations

4. **For frontend changes**
   - Trace back to verify backend API compatibility
   - Check if hook signatures changed
   - Update dependent components

## Performance Considerations

- Graph baseline is cached in memory
- Analysis is performed in O(n) time where n = number of edges in graph
- Typical analysis completes in <100ms

## Future Enhancements

- [ ] Bidirectional impact analysis (what changed upstream)
- [ ] Database schema impact analysis
- [ ] Test impact detection
- [ ] Breaking change warnings
- [ ] Migration path suggestions

