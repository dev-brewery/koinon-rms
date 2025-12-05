# Koinon RMS Development MCP Server

A custom Model Context Protocol (MCP) server that provides development validation and architectural guidance specifically for the Koinon RMS project.

## Purpose

This MCP server helps maintain code quality and architectural consistency by:

- Validating naming conventions (snake_case for database, PascalCase for C#)
- Ensuring routes use IdKey instead of integer IDs
- Checking clean architecture dependency rules
- Detecting legacy anti-patterns
- Providing work unit validation
- Offering contextual architectural guidance

## Features

### Tools

#### 1. `validate_naming`
Validates naming conventions for different contexts.

**Parameters:**
- `type`: One of `database`, `csharp`, `typescript`, or `route`
- `names`: Array of names to validate

**Example:**
```json
{
  "type": "database",
  "names": ["group_member", "person_alias", "firstName"]
}
```

**Response:**
```json
{
  "valid": false,
  "issues": [
    "\"firstName\" - Database names must be snake_case (lowercase with underscores)"
  ]
}
```

#### 2. `validate_routes`
Ensures API routes follow conventions (IdKey usage, versioning).

**Parameters:**
- `routes`: Array of route patterns

**Example:**
```json
{
  "routes": [
    "/api/v1/people/{idKey}",
    "/api/person/123"
  ]
}
```

#### 3. `validate_dependencies`
Validates clean architecture dependency rules.

**Parameters:**
- `project`: One of `Domain`, `Application`, `Infrastructure`, or `Api`
- `dependencies`: Array of layer dependencies

**Example:**
```json
{
  "project": "Application",
  "dependencies": ["Domain", "Infrastructure"]
}
```

#### 4. `detect_antipatterns`
Scans code for legacy patterns and anti-patterns.

**Parameters:**
- `code`: Code snippet to analyze
- `language`: Either `csharp` or `typescript`

**Detected Patterns:**
- Legacy server controls (`runat="server"`)
- ViewState usage
- Page lifecycle methods
- DbContext outside Infrastructure layer
- Synchronous database calls
- Integer IDs in routes
- TypeScript `any` type
- React class components
- Component lifecycle methods

#### 5. `get_architecture_guidance`
Retrieves architectural guidance for specific topics.

**Parameters:**
- `topic`: One of `entity`, `api`, `database`, `frontend`, or `clean-architecture`

### Resources

The server provides access to documentation resources:

- `koinon://docs/architecture` - Clean architecture patterns
- `koinon://docs/conventions` - Naming and code conventions
- `koinon://docs/entity-design` - Entity implementation guidelines
- `koinon://docs/api-design` - API design patterns

### Prompts

Pre-configured prompts for common review tasks:

1. **review_entity** - Review entity implementations
2. **review_api_endpoint** - Review API endpoint code
3. **check_work_unit** - Validate work unit completion

## Installation

```bash
cd tools/mcp-koinon-dev
npm install
npm run build
```

## Usage

### In Claude Code Configuration

Add to your Claude Code MCP configuration:

```json
{
  "mcpServers": {
    "koinon-dev": {
      "command": "node",
      "args": ["/home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev/dist/index.js"],
      "env": {
        "KOINON_PROJECT_ROOT": "/home/mbrewer/projects/koinon-rms"
      }
    }
  }
}
```

### Example Interactions

**Validate entity naming:**
```
Use the validate_naming tool with:
- type: "csharp"
- names: ["Person", "GroupMember", "personAlias"]
```

**Check routes:**
```
Use the validate_routes tool with:
- routes: ["/api/v1/people/{idKey}", "/api/groups/123"]
```

**Detect anti-patterns in code:**
```
Use the detect_antipatterns tool with:
- code: "public async Task<Person> GetPerson(int id) { return context.People.Where(p => p.Id == id).First(); }"
- language: "csharp"
```

**Get guidance:**
```
Use the get_architecture_guidance tool with:
- topic: "entity"
```

## Development

### Build
```bash
npm run build
```

### Watch mode
```bash
npm run watch
```

### Run locally
```bash
npm start
```

## Architecture

The server is built using:
- `@modelcontextprotocol/sdk` - MCP SDK for TypeScript
- `zod` - Schema validation
- TypeScript with strict mode

## Validation Rules

### Database Naming
- Must be snake_case: lowercase with underscores
- Valid: `person`, `group_member`, `created_date_time`
- Invalid: `Person`, `groupMember`, `CreatedDateTime`

### C# Naming
- Classes, Properties, Methods: PascalCase
- Valid: `Person`, `FirstName`, `GetById`
- Invalid: `person`, `firstName`, `get_by_id`

### TypeScript Naming
- Variables, functions: camelCase
- Types, Interfaces: PascalCase
- Valid: `firstName`, `PersonDto`
- Invalid: `FirstName`, `person_dto`

### Route Conventions
- Must start with `/api/v{version}/`
- Must use IdKey, not integer IDs
- Valid: `/api/v1/people/{idKey}`
- Invalid: `/api/people/123`, `/people/{id}`

### Clean Architecture Dependencies

| Layer | Allowed Dependencies |
|-------|---------------------|
| Domain | None |
| Application | Domain |
| Infrastructure | Domain, Application |
| Api | Domain, Application, Infrastructure |

## Anti-Patterns Detected

### C# Anti-Patterns
- `runat="server"` - Legacy server controls
- `ViewState` - Legacy state management
- `Page_Load`, `Page_Init` - WebForms lifecycle
- `DbContext` outside Infrastructure - Layering violation
- `.Result`, `.Wait()` - Synchronous database calls
- Integer IDs in routes - Should use IdKey

### TypeScript Anti-Patterns
- `: any` - Type safety violation
- Class components - Use functional components
- Lifecycle methods - Use hooks instead

## Contributing

When adding new validation rules:

1. Add the rule to the appropriate validation function
2. Update tests (if implemented)
3. Document the rule in this README
4. Update the tool schema if parameters change

## License

MIT - Part of the Koinon RMS project
