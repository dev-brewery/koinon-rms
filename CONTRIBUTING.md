# Contributing to Koinon RMS

Thank you for your interest in contributing to Koinon RMS! This document provides guidance for developers working on the project.

## Getting Started

1. Read `CLAUDE.md` for architecture overview and conventions
2. Read `koinon-rms/CLAUDE.md` for project-specific guidance
3. Install dependencies: `npm install`
4. Start infrastructure: `docker-compose up -d`

## Development Workflow

### Code Changes

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make code changes following project conventions
3. Run local validation: `npm run validate`
4. Stage changes: `git add .`
5. Commit with descriptive message
6. Push and create PR

### Graph Baseline Updates

The API graph is a contract between layers (Domain → Application → Infrastructure → Api).

#### When to Update

Update the baseline when you make structural changes:

- **Adding new entities** - New `Entity` subclass
- **Adding new DTOs** - New `Dto` class
- **Adding API endpoints** - New controller methods
- **Renaming fields or parameters** - Property or parameter renames
- **Reorganizing components** - Moving components to new folders
- **Major refactoring** - Significant structural changes

Do NOT update when:

- Implementing method bodies
- Changing comments or documentation
- Adding private methods
- Formatting or whitespace changes
- Adding tests (unless changing component types)

#### How to Update

1. Make your code changes
2. Run the update command:
   ```bash
   npm run graph:update
   ```

3. Review the baseline changes:
   ```bash
   git diff tools/graph/graph-baseline.json
   ```

4. Verify the changes match your code modifications
5. Stage both code and baseline:
   ```bash
   git add -A
   ```

6. Include graph update in commit message:
   ```
   feat(attendance): add attendance tracking with graph update
   ```

See `tools/graph/README.md` for detailed scenarios and troubleshooting.

## Code Standards

### C#

- File-scoped namespaces: `namespace Koinon.Domain.Entities;`
- PascalCase file names: `PersonEntity.cs`
- Private fields with underscore: `_firstName`
- Async methods with Async suffix: `GetPersonAsync()`
- Use primary constructors for DI
- Use records for DTOs

Example:
```csharp
namespace Koinon.Application.DTOs;

public record PersonDto(
    string IdKey,
    string FirstName,
    string Email,
    DateTime CreatedDateTime);
```

### TypeScript/React

- Strict mode enabled
- No `any` types (use `unknown` if needed)
- Functional components only
- Use TanStack Query for data fetching
- Component files: `PascalCase.tsx`
- Hook files: `useCamelCase.ts`
- Utility files: `camelCase.ts`

Example:
```typescript
interface PersonListProps {
  people: Person[];
  onSelect: (person: Person) => void;
}

export function PersonList({ people, onSelect }: PersonListProps) {
  return (
    <ul>
      {people.map(person => (
        <li key={person.idKey} onClick={() => onSelect(person)}>
          {person.firstName}
        </li>
      ))}
    </ul>
  );
}
```

## Testing

### Backend Tests

```bash
# Run all tests
npm run test

# Run specific test file
dotnet test tests/Koinon.Application.Tests/ServiceTests.cs
```

### Frontend Tests

```bash
# From src/web directory
npm test

# Watch mode
npm test -- --watch
```

## API Design

- Base path: `/api/v1/`
- Use IdKey in URLs (never integer IDs)
- Consistent response format
- Proper HTTP status codes
- Documented with examples

See `docs/reference/api-contracts.md` for endpoint specifications.

## Database Migrations

```bash
# Create migration
dotnet ef migrations add YourMigrationName \
  -p src/Koinon.Infrastructure \
  -s src/Koinon.Api

# Review generated files

# Apply migration
dotnet ef database update \
  -p src/Koinon.Infrastructure \
  -s src/Koinon.Api
```

Follow naming conventions:
- Table names: `snake_case`
- Column names: `snake_case`
- Use `id` for primary key
- Use `{entity}_id` for foreign keys

## Code Review

All PRs require code review and CI approval before merging.

- Push code and create PR
- Wait for CI checks to pass
- Code reviewer will provide feedback
- Address feedback and update PR
- Once approved, PR can be merged

## Anti-Patterns to Avoid

### C#
- Never expose integer IDs in DTOs or URLs
- No business logic in controllers
- No synchronous database calls (use `.ToListAsync()` instead of `.ToList()`)
- No N+1 queries (use `.Include()` for related data)
- No hardcoded credentials or secrets

### TypeScript
- No `any` types
- No class components (use functional)
- No direct DOM manipulation (use React refs if needed)
- No console.log in production code

## Common Commands

```bash
# Validate everything locally
npm run validate

# Quick validation of staged changes
npm run validate:quick

# Build only
npm run build

# Run tests
npm run test

# Type check frontend
npm run typecheck

# Lint frontend
npm run lint

# Run API with hot reload
npm run dev:api

# Run frontend dev server
npm run dev:web

# Update graph baseline
npm run graph:update

# Validate graph without updating
npm run graph:validate
```

## Documentation

- Update docs when changing architecture or APIs
- Include examples in complex documentation
- Reference related sections from `CLAUDE.md`

## Getting Help

- Check `CLAUDE.md` for architecture guidance
- Check `koinon-rms/CLAUDE.md` for project conventions
- Check `docs/reference/` for entity mappings and API contracts
- Review recent PRs for pattern examples
- Ask in pull request comments

## Branch Naming

- Feature: `feature/issue-###-brief-description`
- Bug fix: `fix/issue-###-brief-description`
- Refactor: `refactor/issue-###-brief-description`
- Docs: `docs/issue-###-brief-description`

Example: `feature/issue-289-graph-baseline-docs`

## Commit Messages

Follow conventional commits format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Example:
```
feat(graph): add baseline validation system

Add comprehensive documentation for graph validation including
update scenarios and troubleshooting guide.

Closes #289
```

## Questions or Issues?

If you encounter problems:

1. Check existing documentation
2. Review related issues
3. Ask in a PR comment or new issue
4. Reference the CLAUDE.md files for guidance

Thank you for contributing!
