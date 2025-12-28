# Graph Generator Test Fixtures

This directory contains test fixtures for validating the graph generators (`generate-backend.py` and `generate-frontend.js`).

## Directory Structure

```
fixtures/
├── valid/           # Files that follow all project conventions
├── invalid/         # Files that violate project rules (should be detected)
└── edge-cases/      # Unusual but potentially valid cases (parser resilience)
```

## Backend Fixtures (C#)

### Valid Files (`valid/`)

| File | Purpose | Tests |
|------|---------|-------|
| `PersonEntity.cs` | Canonical entity | Entity inheritance, required properties, computed properties, navigation properties |
| `PersonDto.cs` | Canonical DTOs | Record types, IdKey usage, no int Id exposure, nested DTOs |
| `PersonService.cs` | Canonical service | Async/await, CancellationToken, proper dependency injection, mapping logic |
| `PeopleController.cs` | Canonical controller | Route attributes, IdKey parameters, ProblemDetails, response envelopes, HTTP methods |

### Invalid Files (`invalid/`)

| File | Violation | Expected Detection |
|------|-----------|-------------------|
| `BadEntity.cs` | Entity not inheriting from `Entity` base class | Missing inheritance pattern |
| `ExposedIdDto.cs` | DTO with `public int Id` | Rule 04: API Design violation |
| `IntIdController.cs` | Routes with `{id}` instead of `{idKey}` | Rule 04: API Design violation |

### Edge Cases (`edge-cases/`)

| File | Edge Case | Tests |
|------|-----------|-------|
| `EmptyFile.cs` | Empty file with only comments | Parser handling of empty input |
| `SyntaxError.cs` | Intentional syntax errors | Error recovery and graceful failure |
| `UnicodeNames.cs` | Unicode characters in strings/comments | Unicode handling: 日本語, العربية, emoji 🎉 |
| `UnusualFormatting.cs` | Unusual but valid whitespace | Parser resilience to spacing variations |

## Frontend Fixtures (TypeScript)

### Valid Files (`valid/`)

| File | Purpose | Tests |
|------|---------|-------|
| `types.ts` | Type definitions | Interface exports, type aliases, proper typing |
| `people.ts` | API service functions | Client wrapper usage, async/await, type imports |
| `usePeople.ts` | TanStack Query hooks | useQuery, useMutation, queryKey patterns, cache invalidation |

### Invalid Files (`invalid/`)

| File | Violation | Expected Detection |
|------|-----------|-------------------|
| `directFetch.tsx` | Direct fetch/axios calls | Not using API service layer |

### Edge Cases (`edge-cases/`)

| File | Edge Case | Tests |
|------|-----------|-------|
| `emptyTypes.ts` | Empty file with only comments | Parser handling of empty input |
| `syntaxError.ts` | TypeScript syntax errors | Error recovery and graceful failure |
| `unicodeContent.ts` | Unicode in property names and values | International characters, emojis, symbols |
| `unusualFormatting.ts` | Unusual but valid formatting | Whitespace variations, single-line, multi-line breaks |

## Usage in Tests

### Backend Generator Testing

```python
# Test valid patterns
valid_entity = parse_file('fixtures/valid/PersonEntity.cs')
assert valid_entity['inherits_from'] == 'Entity'
assert 'FirstName' in valid_entity['properties']

# Test violation detection
bad_entity = parse_file('fixtures/invalid/BadEntity.cs')
assert bad_entity['violations']['missing_base_class'] == True

exposed_id_dto = parse_file('fixtures/invalid/ExposedIdDto.cs')
assert exposed_id_dto['violations']['exposes_int_id'] == True

# Test edge cases
syntax_error = parse_file('fixtures/edge-cases/SyntaxError.cs')
assert syntax_error['parse_errors'] is not None
assert syntax_error['error_recovery_succeeded'] == True
```

### Frontend Generator Testing

```javascript
// Test valid patterns
const validTypes = parseFile('fixtures/valid/types.ts');
assert(validTypes.exports.includes('PersonSummaryDto'));

const validHooks = parseFile('fixtures/valid/usePeople.ts');
assert(validHooks.hooks.includes('usePeople'));
assert(validHooks.apiCalls.includes('peopleApi.searchPeople'));

// Test violation detection
const directFetch = parseFile('fixtures/invalid/directFetch.tsx');
assert(directFetch.violations.directFetch === true);

// Test edge cases
const syntaxError = parseFile('fixtures/edge-cases/syntaxError.ts');
assert(syntaxError.parseErrors !== null);
assert(syntaxError.errorRecoverySucceeded === true);
```

## Fixture Design Principles

1. **Realistic**: Fixtures mirror actual codebase patterns
2. **Focused**: Each file tests specific patterns or violations
3. **Documented**: Comments explain what's being tested
4. **Comprehensive**: Cover happy path, violations, and edge cases
5. **Maintainable**: Based on canonical examples from `src/`

## Adding New Fixtures

When adding new fixtures:

1. Place in appropriate directory (`valid/`, `invalid/`, `edge-cases/`)
2. Add comprehensive comments explaining what's being tested
3. Update this README with fixture details
4. Ensure fixture tests a specific, documented pattern
5. Verify fixture with actual generator before committing

## References

- Canonical backend patterns: `src/Koinon.Api/Controllers/PeopleController.cs`
- Canonical frontend patterns: `src/web/src/hooks/usePeople.ts`
- API contracts: `docs/reference/api-contracts.md`
- Project rules: `.claude/rules/`
