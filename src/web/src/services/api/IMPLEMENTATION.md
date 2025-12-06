# WU-4.1.1: Frontend API Client Implementation

## Overview

Complete implementation of type-safe API client for Koinon RMS React frontend using native fetch API (not axios) with automatic token refresh and comprehensive error handling.

## Implemented Files

### Core Client (`client.ts`)
- Native fetch-based HTTP client
- Token storage in memory (not localStorage for security)
- Automatic JWT token refresh on 401 responses
- Request/response error handling
- Typed error responses
- Convenience methods: get, post, put, del, patch
- Request cancellation support via AbortController

### Type Definitions (`types.ts`)
- Complete TypeScript interfaces matching backend DTOs
- All API request/response types
- Common types: IdKey, DateTime, DateOnly, Guid
- Response envelopes: ApiResponse, PagedResult, ApiError
- Person, Family, Group, Check-in types
- Reference data types: DefinedType, Campus, GroupType

### Service Modules

1. **auth.ts** - Authentication API
   - `login()` - Username/password authentication
   - `refresh()` - Token refresh
   - `logout()` - Invalidate tokens
   - `clearSession()` - Local token cleanup

2. **people.ts** - People API
   - `searchPeople()` - Search with filters and pagination
   - `getPersonByIdKey()` - Get person details
   - `createPerson()` - Create new person
   - `updatePerson()` - Update person details
   - `deletePerson()` - Soft delete person
   - `getPersonFamily()` - Get person's family
   - `getPersonGroups()` - Get person's groups

3. **families.ts** - Families API
   - `searchFamilies()` - Search families
   - `getFamilyByIdKey()` - Get family details
   - `createFamily()` - Create new family
   - `updateFamily()` - Update family
   - `addFamilyMember()` - Add member to family
   - `removeFamilyMember()` - Remove member from family
   - `updateFamilyAddress()` - Update family address

4. **groups.ts** - Groups API
   - `searchGroups()` - Search groups
   - `getGroupByIdKey()` - Get group details
   - `getGroupMembers()` - Get group members
   - `addGroupMember()` - Add member to group
   - `removeGroupMember()` - Remove member from group

5. **checkin.ts** - Check-in API
   - `getCheckinConfiguration()` - Get kiosk/campus config
   - `searchFamiliesForCheckin()` - Search families by phone/name
   - `getCheckinOpportunities()` - Get available check-in options
   - `recordAttendance()` - Record check-in attendance
   - `checkout()` - Record check-out
   - `getLabels()` - Get printable labels

6. **reference.ts** - Reference Data API
   - `getDefinedTypes()` - Get all defined types
   - `getDefinedTypeValues()` - Get values for defined type
   - `getCampuses()` - List campuses
   - `getGroupTypes()` - List group types with roles

### Main Export (`index.ts`)
- Centralized exports for all services and types
- Named exports for better tree-shaking
- Service namespaces: authApi, peopleApi, familiesApi, groupsApi, checkinApi, referenceApi

### Documentation (`README.md`)
- Usage examples for all services
- Error handling patterns
- TanStack Query integration examples
- Request cancellation examples
- Environment configuration

## Key Features

### Security
✅ Tokens stored in memory (not localStorage)
✅ Automatic token refresh on 401
✅ No sensitive data exposure
✅ Type-safe error handling

### Performance
✅ Native fetch (no axios dependency)
✅ Request cancellation support
✅ Automatic retry with token refresh
✅ Efficient query parameter building

### Developer Experience
✅ Full TypeScript type safety
✅ No `any` types
✅ Comprehensive JSDoc comments
✅ IDE autocomplete support
✅ Clear error messages

### Standards Compliance
✅ Follows API contracts in docs/reference/api-contracts.md
✅ Uses IdKey (not integer IDs) in URLs
✅ Consistent response envelope handling
✅ Proper HTTP method usage

## Acceptance Criteria

- [x] Base client with auth header injection
- [x] Automatic token refresh on 401
- [x] Type-safe request/response types
- [x] Error handling utilities
- [x] Request cancellation support
- [x] Uses native fetch (not axios)
- [x] Types match API DTOs exactly
- [x] Token stored in memory (not localStorage)
- [x] All services implemented:
  - [x] Auth
  - [x] People
  - [x] Families
  - [x] Groups
  - [x] Check-in
  - [x] Reference data

## Validation

```bash
# TypeScript compilation
npm run typecheck
✅ PASS - No type errors

# ESLint
npm run lint
✅ PASS - No linting errors

# Build
npm run build
✅ PASS - Production build successful
```

## Usage Example

```typescript
import { authApi, peopleApi, setTokens } from '@/services/api';

// Login
const result = await authApi.login({
  username: 'user@example.com',
  password: 'password'
});

// Search people
const people = await peopleApi.searchPeople({
  q: 'John',
  page: 1,
  pageSize: 25
});

// Error handling
try {
  const person = await peopleApi.getPersonByIdKey('invalid');
} catch (error) {
  if (error instanceof ApiClientError) {
    console.error(error.error.message);
  }
}
```

## Next Steps

- WU-4.1.2: Authentication State (AuthContext, useAuth hook)
- WU-4.1.3: Layout Components (AppShell, Sidebar, Header)
- TanStack Query setup for server state management
- Protected route wrapper

## Notes

- All types match backend DTOs from api-contracts.md
- Token refresh is automatic and transparent
- Services are tree-shakeable (use named imports)
- No runtime dependencies added (uses native fetch)
- Compatible with React Server Components (if needed later)
