# MCP Servers Usage Examples

This document provides practical examples of how to use each MCP server in the Koinon RMS project.

## Table of Contents

- [PostgreSQL Server](#postgresql-server)
- [Memory Server](#memory-server)
- [GitHub Server](#github-server)
- [Filesystem Server](#filesystem-server)
- [Koinon Dev Server](#koinon-dev-server)

---

## PostgreSQL Server

### Example 1: List All Tables

**Use Case:** Get an overview of the database schema

**Tool Call:**
```json
{
  "tool": "postgres_query",
  "query": "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;"
}
```

**Expected Output:**
```
tablename
-----------
group
group_member
person
person_alias
```

### Example 2: Inspect Table Schema

**Use Case:** Understand the structure of an entity table

**Tool Call:**
```json
{
  "tool": "postgres_query",
  "query": "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'person' ORDER BY ordinal_position;"
}
```

### Example 3: Check Foreign Key Relationships

**Use Case:** Validate entity relationships

**Tool Call:**
```json
{
  "tool": "postgres_query",
  "query": "SELECT tc.table_name, kcu.column_name, ccu.table_name AS foreign_table_name FROM information_schema.table_constraints AS tc JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name WHERE tc.constraint_type = 'FOREIGN KEY';"
}
```

### Example 4: Verify Data Migration

**Use Case:** Check if data was properly migrated

**Tool Call:**
```json
{
  "tool": "postgres_query",
  "query": "SELECT COUNT(*) as total_people, COUNT(CASE WHEN email IS NOT NULL THEN 1 END) as with_email FROM person;"
}
```

---

## Memory Server

### Example 1: Store Work Unit Completion

**Use Case:** Remember that a work unit has been completed

**Tool Call:**
```json
{
  "tool": "memory_store",
  "key": "work-unit-1.1.1",
  "value": {
    "status": "completed",
    "completedDate": "2025-12-05",
    "description": "Domain layer foundation with Entity base class",
    "artifacts": [
      "src/Koinon.Domain/Entities/Entity.cs",
      "src/Koinon.Domain/Interfaces/IEntity.cs"
    ]
  }
}
```

### Example 2: Store Architectural Decision

**Use Case:** Record an important architectural decision for future reference

**Tool Call:**
```json
{
  "tool": "memory_store",
  "key": "decision-idkey-encoding",
  "value": {
    "decision": "Use Base62 encoding for IdKey to obfuscate integer IDs",
    "rationale": "Security through obscurity and cleaner URLs",
    "date": "2025-12-05",
    "alternatives": "GUID-only, Base64, or expose integer IDs"
  }
}
```

### Example 3: Retrieve Context

**Use Case:** Get information stored in a previous session

**Tool Call:**
```json
{
  "tool": "memory_retrieve",
  "key": "work-unit-1.1.1"
}
```

### Example 4: Store Technical Debt

**Use Case:** Track items that need future attention

**Tool Call:**
```json
{
  "tool": "memory_store",
  "key": "tech-debt-eager-loading",
  "value": {
    "issue": "Some queries missing .Include() for navigation properties",
    "impact": "N+1 query problem in production",
    "priority": "high",
    "affectedFiles": ["src/Koinon.Infrastructure/Repositories/GroupRepository.cs"]
  }
}
```

---

## GitHub Server

### Example 1: Create Issue for Missing Feature

**Use Case:** Track a feature request

**Tool Call:**
```json
{
  "tool": "github_create_issue",
  "title": "Implement family check-in workflow",
  "body": "## Description\nImplement the Sunday morning family check-in workflow for kiosks.\n\n## Acceptance Criteria\n- [ ] Family search by phone number\n- [ ] Display family members\n- [ ] Select attendees\n- [ ] Print labels\n- [ ] Complete in <200ms\n\n## Work Unit\nWU-3.2.1",
  "labels": ["enhancement", "mvp", "check-in"]
}
```

### Example 2: List Open Pull Requests

**Use Case:** Review pending code changes

**Tool Call:**
```json
{
  "tool": "github_list_pull_requests",
  "state": "open"
}
```

### Example 3: Create Pull Request

**Use Case:** Submit code for review

**Tool Call:**
```json
{
  "tool": "github_create_pull_request",
  "title": "feat: Add Person entity and repository",
  "body": "## Changes\n- Implemented Person entity with all required fields\n- Created PersonRepository with CRUD operations\n- Added unit tests\n- Updated documentation\n\n## Work Unit\nWU-1.2.1\n\n## Testing\n- All unit tests pass\n- Manual testing completed",
  "head": "feature/person-entity",
  "base": "main"
}
```

### Example 4: Add Comment to Issue

**Use Case:** Update issue with progress

**Tool Call:**
```json
{
  "tool": "github_add_comment",
  "issue_number": 42,
  "body": "✅ Work completed. All acceptance criteria met. Ready for review."
}
```

---

## Filesystem Server

### Example 1: Search for Entity Classes

**Use Case:** Find all entity implementations

**Tool Call:**
```json
{
  "tool": "filesystem_search",
  "path": "/home/mbrewer/projects/koinon-rms/src/Koinon.Domain/Entities",
  "pattern": "*.cs"
}
```

### Example 2: Find All DbContext References

**Use Case:** Ensure DbContext is only used in Infrastructure layer

**Tool Call:**
```json
{
  "tool": "filesystem_search",
  "path": "/home/mbrewer/projects/koinon-rms/src",
  "pattern": "*.cs",
  "contains": "DbContext"
}
```

**Validation:** Results should only show files in `Koinon.Infrastructure` project.

### Example 3: List All Migration Files

**Use Case:** Review database migration history

**Tool Call:**
```json
{
  "tool": "filesystem_list",
  "path": "/home/mbrewer/projects/koinon-rms/src/Koinon.Infrastructure/Migrations"
}
```

### Example 4: Find TypeScript Components

**Use Case:** Locate all React components

**Tool Call:**
```json
{
  "tool": "filesystem_search",
  "path": "/home/mbrewer/projects/koinon-rms/src/web/src/components",
  "pattern": "*.tsx"
}
```

---

## Koinon Dev Server

### Example 1: Validate Database Table Names

**Use Case:** Ensure database naming follows snake_case convention

**Tool Call:**
```json
{
  "tool": "validate_naming",
  "type": "database",
  "names": [
    "person",
    "group_member",
    "person_alias",
    "GroupType",
    "personEmail"
  ]
}
```

**Expected Response:**
```json
{
  "valid": false,
  "issues": [
    "\"GroupType\" - Database names must be snake_case (lowercase with underscores)",
    "\"personEmail\" - Database names must be snake_case (lowercase with underscores)"
  ]
}
```

### Example 2: Validate C# Class Names

**Use Case:** Check C# naming conventions

**Tool Call:**
```json
{
  "tool": "validate_naming",
  "type": "csharp",
  "names": [
    "Person",
    "GroupMember",
    "PersonAlias",
    "personEmail",
    "get_by_id"
  ]
}
```

**Expected Response:**
```json
{
  "valid": false,
  "issues": [
    "\"personEmail\" - C# class/property names must be PascalCase",
    "\"get_by_id\" - C# class/property names must be PascalCase"
  ]
}
```

### Example 3: Validate API Routes

**Use Case:** Ensure routes use IdKey instead of integer IDs

**Tool Call:**
```json
{
  "tool": "validate_routes",
  "routes": [
    "/api/v1/people/{idKey}",
    "/api/v1/groups/{idKey}/members",
    "/api/people/123",
    "/people/{id}"
  ]
}
```

**Expected Response:**
```json
{
  "valid": false,
  "issues": [
    "\"/api/people/123\" - Routes must use IdKey, never integer IDs",
    "\"/api/people/123\" - API routes should start with /api/v{version}/",
    "\"/people/{id}\" - API routes should start with /api/v{version}/"
  ]
}
```

### Example 4: Validate Clean Architecture Dependencies

**Use Case:** Check that Application layer doesn't reference Infrastructure

**Tool Call:**
```json
{
  "tool": "validate_dependencies",
  "project": "Application",
  "dependencies": ["Domain", "Infrastructure"]
}
```

**Expected Response:**
```json
{
  "valid": false,
  "issues": [
    "\"Application\" cannot depend on \"Infrastructure\". Allowed dependencies: Domain"
  ]
}
```

### Example 5: Detect Anti-Patterns in C# Code

**Use Case:** Scan code for legacy patterns

**Tool Call:**
```json
{
  "tool": "detect_antipatterns",
  "code": "public class PersonController : Controller { protected void Page_Load(object sender, EventArgs e) { var person = context.People.Where(p => p.Id == 123).First(); } }",
  "language": "csharp"
}
```

**Expected Response:**
```json
{
  "patterns": [
    "LEGACY: Page lifecycle methods detected - use MediatR handlers",
    "ARCHITECTURE: DbContext should only be used in Infrastructure layer"
  ]
}
```

### Example 6: Detect Anti-Patterns in TypeScript Code

**Use Case:** Scan React code for legacy patterns

**Tool Call:**
```json
{
  "tool": "detect_antipatterns",
  "code": "class PersonCard extends React.Component { componentDidMount() { const data: any = fetchPerson(); } }",
  "language": "typescript"
}
```

**Expected Response:**
```json
{
  "patterns": [
    "TYPE SAFETY: \"any\" type detected - use strict typing",
    "LEGACY: Class components detected - use functional components only",
    "LEGACY: Lifecycle methods detected - use hooks instead"
  ]
}
```

### Example 7: Get Architecture Guidance

**Use Case:** Get help on entity design

**Tool Call:**
```json
{
  "tool": "get_architecture_guidance",
  "topic": "entity"
}
```

**Expected Response:**
```
Entity Design Guidelines:
- All entities inherit from Entity base class
- Use Guid for external references (exposed as IdKey)
- Never expose integer IDs in APIs
- Implement audit fields (CreatedDateTime, ModifiedDateTime, etc.)
- Use required modifier for non-nullable properties
- Navigation properties should be virtual for lazy loading
```

### Example 8: Get API Design Guidance

**Use Case:** Learn API conventions

**Tool Call:**
```json
{
  "tool": "get_architecture_guidance",
  "topic": "api"
}
```

### Example 9: Access Documentation Resources

**Use Case:** Read architecture documentation

**Tool Call:**
```json
{
  "tool": "read_resource",
  "uri": "koinon://docs/architecture"
}
```

### Example 10: Use Review Entity Prompt

**Use Case:** Review an entity implementation

**Tool Call:**
```json
{
  "tool": "get_prompt",
  "name": "review_entity",
  "arguments": {
    "entity_code": "public class Person : Entity { public string FirstName { get; set; } public string LastName { get; set; } }"
  }
}
```

---

## Combined Workflow Examples

### Workflow 1: Implementing a New Entity

1. **Get guidance:**
   ```json
   {"tool": "get_architecture_guidance", "topic": "entity"}
   ```

2. **Check naming conventions:**
   ```json
   {"tool": "validate_naming", "type": "csharp", "names": ["Person", "FirstName", "LastName"]}
   {"tool": "validate_naming", "type": "database", "names": ["person", "first_name", "last_name"]}
   ```

3. **Implement the entity** (using filesystem or code editor)

4. **Detect anti-patterns:**
   ```json
   {"tool": "detect_antipatterns", "code": "<entity code>", "language": "csharp"}
   ```

5. **Store completion:**
   ```json
   {"tool": "memory_store", "key": "entity-person-completed", "value": {"status": "done"}}
   ```

### Workflow 2: Creating an API Endpoint

1. **Get API guidance:**
   ```json
   {"tool": "get_architecture_guidance", "topic": "api"}
   ```

2. **Validate route naming:**
   ```json
   {"tool": "validate_routes", "routes": ["/api/v1/people/{idKey}"]}
   ```

3. **Check dependencies:**
   ```json
   {"tool": "validate_dependencies", "project": "Api", "dependencies": ["Domain", "Application", "Infrastructure"]}
   ```

4. **Implement endpoint** (using code editor)

5. **Review with prompt:**
   ```json
   {"tool": "get_prompt", "name": "review_api_endpoint", "arguments": {"endpoint_code": "<code>"}}
   ```

6. **Create PR:**
   ```json
   {"tool": "github_create_pull_request", "title": "feat: Add People API endpoint", ...}
   ```

### Workflow 3: Database Schema Review

1. **List all tables:**
   ```json
   {"tool": "postgres_query", "query": "SELECT tablename FROM pg_tables WHERE schemaname = 'public'"}
   ```

2. **Validate table names:**
   ```json
   {"tool": "validate_naming", "type": "database", "names": ["person", "group", "group_member"]}
   ```

3. **Check foreign keys:**
   ```json
   {"tool": "postgres_query", "query": "SELECT ... FROM information_schema.table_constraints ..."}
   ```

4. **Store findings:**
   ```json
   {"tool": "memory_store", "key": "schema-review-2025-12-05", "value": {...}}
   ```

---

## Tips for Effective MCP Server Usage

### 1. Combine Servers

Use multiple servers together for comprehensive validation:
- Filesystem to find files
- Koinon Dev to validate conventions
- Memory to store results
- GitHub to create issues

### 2. Store Important Context

Always use Memory server to persist:
- Work unit completions
- Architectural decisions
- Technical debt items
- Known issues and workarounds

### 3. Validate Early and Often

Run validation tools before committing:
- Check naming conventions
- Validate routes
- Detect anti-patterns
- Verify dependencies

### 4. Use Prompts for Reviews

Leverage the built-in prompts for consistent code reviews:
- `review_entity`
- `review_api_endpoint`
- `check_work_unit`

### 5. Query Database for Verification

Use PostgreSQL server to verify:
- Migrations applied correctly
- Data integrity
- Index performance
- Foreign key constraints

---

## Troubleshooting Common Issues

### Issue: "Connection refused" from PostgreSQL server

**Solution:** Start Docker services
```bash
cd /home/mbrewer/projects/koinon-rms
docker-compose up -d
```

### Issue: GitHub server authentication fails

**Solution:** Set GITHUB_TOKEN
```bash
export GITHUB_TOKEN="your_token_here"
```

### Issue: Custom Koinon server not found

**Solution:** Rebuild the server
```bash
cd tools/mcp-koinon-dev
npm run build
```

### Issue: Memory not persisting between sessions

**Solution:** Check Memory server configuration and ensure it's using persistent storage

---

## Additional Resources

- Main README: `tools/mcp-servers/README.md`
- Custom Server Docs: `tools/mcp-koinon-dev/README.md`
- Project Conventions: `CLAUDE.md`
- Setup Script: `tools/mcp-servers/setup.sh`
- Test Script: `tools/mcp-servers/test-servers.sh`
