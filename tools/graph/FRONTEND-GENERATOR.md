# Frontend Graph Generator

This document describes the **Frontend Graph Generator**, a Node.js tool that parses TypeScript/React source files to extract type definitions, API functions, hooks, and components into a machine-readable graph.

## Overview

The frontend graph generator analyzes the Koinon RMS React frontend to create a complete map of:

- **Types**: Interfaces, type aliases, and enums from `src/web/src/services/api/types.ts`
- **API Functions**: HTTP client functions from `src/web/src/services/api/*.ts`
- **Hooks**: React custom hooks from `src/web/src/hooks/*.ts` (TanStack Query based)
- **Components**: React components from `src/web/src/components/**/*.tsx` (MVP)
- **Relationships**: Edges connecting these elements (hook -> API, component -> hook, etc.)

## Usage

### Run the Generator

```bash
# From project root
node tools/graph/generate-frontend.js
```

This will:
1. Parse `src/web/src/services/api/types.ts` for type definitions
2. Scan `src/web/src/services/api/` for API functions
3. Scan `src/web/src/hooks/` for React hooks
4. Scan `src/web/src/components/` for React components (optional)
5. Build relationship edges
6. Write output to `tools/graph/frontend-graph.json`

### Output

The generator produces a JSON file at `tools/graph/frontend-graph.json`:

```json
{
  "version": "1.0.0",
  "generated_at": "2025-12-22T21:58:24.625Z",
  "types": { ... },
  "api_functions": { ... },
  "hooks": { ... },
  "components": { ... },
  "edges": [ ... ]
}
```

## What Gets Extracted

### Types

Extracts all **exported** interfaces, type aliases, and enums:

```typescript
// Extracted
export interface PersonDetailDto { ... }
export type IdKey = string;
export enum CapacityStatus { Available, Warning, Full }

// Not extracted
interface InternalType { ... }
type PrivateType = string;
```

For each type:
- `name`: Type name (PascalCase)
- `kind`: One of `interface`, `type`, or `enum`
- `properties`: Object mapping property names to TypeScript type strings
- `path`: Always `services/api/types.ts`

### API Functions

Extracts all **exported async functions** with Promise return types from API service files:

```typescript
// Extracted
export async function searchFamilies(params: FamiliesSearchParams = {}): Promise<PagedResult<FamilySummaryDto>> {
  return get<PagedResult<FamilySummaryDto>>('/families?...');
}

// Not extracted
async function internalHelper() { }                           // Not exported
export function notAsync() { }                                // Not async
export async function noPromise() { return {}; }             // No Promise return type
```

For each API function:
- `name`: Function name (camelCase)
- `path`: File path relative to `src/web/src`
- `endpoint`: API endpoint extracted from `get()`, `post()`, etc. calls
- `method`: HTTP method inferred from function call (GET, POST, PUT, PATCH, DELETE)
- `responseType`: TypeScript return type from Promise<T>

### Hooks

Extracts all **exported functions starting with "use"** from hook files:

```typescript
// Extracted
export function useFamilies(params: FamiliesSearchParams = {}) {
  return useQuery({
    queryKey: ['families', params],
    queryFn: () => familiesApi.searchFamilies(params),
    staleTime: 5 * 60 * 1000,
  });
}

// Not extracted
export function getFamilies() { }       // Doesn't start with 'use'
const useInternal = () => { };         // Not exported
```

For each hook:
- `name`: Hook name (camelCase with `use` prefix)
- `path`: File path relative to `src/web/src`
- `apiBinding`: Name of the API function this hook calls (if using useQuery/useMutation)
- `queryKey`: TanStack Query key array (extracted from `queryKey` prop)
- `usesQuery`: Boolean - whether hook uses `useQuery`
- `usesMutation`: Boolean - whether hook uses `useMutation`
- `dependencies`: Array of API function names called within the hook

### Components

Extracts **exported React components** from `.tsx` files (MVP - basic extraction):

```typescript
// Extracted
export function PersonDetail({ id }: Props) {
  const person = usePerson(id);
  return <div>...</div>;
}

export const FamilyList = () => {
  const families = useFamilies();
  return <div>...</div>;
};

// Not extracted
const InternalComponent = () => { };    // Not exported
function notAComponent() { }            // Doesn't look like component
```

For each component:
- `name`: Component name (PascalCase)
- `path`: File path relative to `src/web/src`
- `hooksUsed`: Array of hook names the component calls
- `apiCallsDirectly`: Boolean - whether component directly uses `fetch()` or API client (anti-pattern)

### Edges

Relationships connecting nodes:

| Type | From | To | Meaning |
|------|------|----|---------| 
| `api_binding` | Hook | API Function | Hook wraps this API function |
| `depends_on` | Hook | Hook | Hook calls another hook |
| `uses_hook` | Component | Hook | Component calls this hook |

## How It Works

### 1. Type Parsing

Uses regex to extract interface/enum declarations:

```javascript
const interfaceRegex = /export\s+interface\s+(\w+)\s*(?:extends\s+[\w<>,\s]+)?\s*\{([^}]+)\}/g;
const typeRegex = /export\s+type\s+(\w+)\s*=\s*([^;]+);/g;
const enumRegex = /export\s+enum\s+(\w+)\s*\{([^}]+)\}/g;
```

Properties are parsed by finding all `name: type;` patterns within the interface/type body.

### 2. API Function Parsing

Searches for async functions with Promise return types:

```javascript
const funcRegex = /export\s+async\s+function\s+(\w+)\s*\([^)]*\)\s*:\s*Promise<([^>]+)>\s*\{/g;
```

HTTP method and endpoint are extracted from the function body by looking for calls like:
- `get<Type>('/path')`
- `post<Type>('/path')`
- `put<Type>('/path')`
- `patch<Type>('/path')`
- `del<Type>('/path')`

### 3. Hook Parsing

Finds exported functions starting with `use`:

```javascript
const hookRegex = /export\s+function\s+(use\w+)\s*\([^)]*\)\s*\{/g;
```

Then analyzes the hook body to detect:
- **useQuery** vs **useMutation** usage
- **queryKey** array (TanStack Query)
- **API function binding** via `queryFn: () => functionName()` or `mutationFn: (...) => functionName()`
- **Dependencies** on other API functions

### 4. Component Parsing

Recursively scans the components directory for `.tsx` files and extracts:
- Component name from `export function Name()` or filename
- Hooks used via regex matching `useName(` patterns
- Direct API calls (fetch, apiClient, etc.) - anti-pattern detection

### 5. Edge Building

Creates edges by:
1. Linking hooks to their API functions (from `apiBinding`)
2. Linking hooks to other hooks they depend on
3. Linking components to hooks they use

## Configuration

Paths are hardcoded in the generator:

```javascript
const WEB_SRC_DIR = path.join(process.cwd(), 'src/web/src');
const TYPES_FILE = path.join(WEB_SRC_DIR, 'services/api/types.ts');
const SERVICES_API_DIR = path.join(WEB_SRC_DIR, 'services/api');
const HOOKS_DIR = path.join(WEB_SRC_DIR, 'hooks');
const COMPONENTS_DIR = path.join(WEB_SRC_DIR, 'components');
const OUTPUT_FILE = path.join(process.cwd(), 'tools/graph/frontend-graph.json');
```

The generator must be run from the project root.

## Limitations

### MVP Scope

1. **No deep code analysis** - Uses regex, not AST parsing. This means:
   - Nested generics may be truncated
   - Comments in property bodies might be captured
   - Complex type unions may not parse correctly

2. **Component extraction is basic**:
   - Only detects direct hook calls
   - Cannot detect hooks passed as props
   - No prop type extraction

3. **No dependency analysis**:
   - Doesn't track imports/exports between files
   - Only extracts from each file in isolation
   - Circular dependencies not detected

### Known Issues

1. **Type properties with complex types** - Properties containing `{` or `}` may be truncated:
   ```typescript
   // This might not parse correctly
   handler?: (data: { id: string; name: string }) => void;
   ```

2. **Inline comments in interfaces** - Comments might appear in property type strings:
   ```typescript
   export interface Example {
     // this is a comment
     prop: string;  // inline comment
   }
   ```

3. **Generic type parameters** - Complex generics may be partially captured.

## Future Enhancements

- [ ] Use TypeScript compiler API for accurate AST parsing
- [ ] Extract component prop types
- [ ] Detect circular dependencies
- [ ] Generate dependency tree visualizations
- [ ] Validate frontend-backend type contracts
- [ ] Integration with backend graph for full-stack validation

## Integration with CI/CD

The graph is currently generated manually but could be integrated into CI:

```bash
# In CI workflow
node tools/graph/generate-frontend.js

# Optional: Validate against baseline
npm run graph:validate  # (Not yet implemented)
```

See `.github/workflows/graph-validate.yml` for backend integration example.

