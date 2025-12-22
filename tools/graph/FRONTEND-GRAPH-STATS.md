# Frontend Graph Statistics

Generated: 2025-12-22T21:58:24.625Z

## Summary

| Element | Count |
|---------|-------|
| **Types** | 109 |
| **API Functions** | 69 |
| **Hooks** | 71 |
| **Components** | 87 |
| **Edges** | 25 |
| **Total Nodes** | 336 |

## Breakdown by Type

### Types (109)

Found in `src/web/src/services/api/types.ts`:
- **Interfaces**: Request/response DTOs, envelopes, parameter objects
- **Type Aliases**: IdKey, DateOnly, DateTime, Guid, enums like Gender, EmailPreference
- **Enums**: CheckinSearchType, CapacityStatus, MembershipRequestStatus

Key types:
- API Response types: ApiResponse<T>, PagedResult<T>, ProblemDetails
- Entity DTOs: PersonDetailDto, FamilyDetailDto, GroupDetailDto, etc.
- Request types: CreatePersonRequest, UpdateFamilyRequest, etc.
- Shared types: IdKey, DateOnly, DateTime, Guid

### API Functions (69)

Distributed across service files:
- `families.ts` - 5 functions (search, get, create, update, add member, remove member, update address)
- `people.ts` - ~8 functions
- `groups.ts` - ~8 functions
- `checkin.ts` - ~10 functions
- `myGroups.ts` - ~5 functions
- `auth.ts` - 4 functions (login, refresh, logout, supervisor)
- `schedules.ts` - ~8 functions
- `groupTypes.ts` - ~5 functions
- `membershipRequests.ts` - ~4 functions
- Other APIs - analytics, dashboard, etc.

Each function has:
- HTTP method (GET, POST, PUT, PATCH, DELETE)
- API endpoint path
- Request/response types

### Hooks (71)

React custom hooks using TanStack Query:

Families: useFamilies, useFamily, useCreateFamily, useUpdateFamily, useAddFamilyMember, useRemoveFamilyMember
People: usePeople, usePerson, useCreatePerson, useUpdatePerson
Groups: useGroups, useGroup, useCreateGroup, useUpdateGroup, useGroupTypes, useMyGroups
Check-in: useCheckinConfig, useCheckinSearch, useRoomRoster, useRecordAttendance
Other: useAuth, useErrorHandler, useMutationWithToast, etc.

Patterns:
- Query hooks (read): useFamilies, useFamily, usePeople, etc.
- Mutation hooks (write): useCreateFamily, useUpdateFamily, etc.
- Utility hooks: useAuth, useErrorHandler, useMutationWithToast, useIdleTimeout

Each hook:
- Binds to specific API function via `apiBinding`
- Uses TanStack Query (useQuery or useMutation)
- Has queryKey for cache invalidation
- May depend on other API functions

### Components (87)

React components across the application:

Key component families:
- Auth: LoginPage, RegisterForm, SupervisorLogin, etc.
- People: PersonList, PersonDetail, PersonForm, etc.
- Families: FamilyList, FamilyDetail, FamilyMemberForm, etc.
- Groups: GroupList, GroupDetail, GroupMemberForm, etc.
- Check-in: CheckinConfig, CheckinSearch, RoomRoster, etc.
- Admin: DashboardLayout, AdminLayout, etc.

Characteristics:
- 87 components using hooks
- Some components use multiple hooks
- Generally follow pattern: component -> hook -> API function

### Edges (25)

Relationships in the graph:

| Type | Count | Example |
|------|-------|---------|
| `api_binding` | ~20 | useFamilies -> searchFamilies |
| `depends_on` | ~3 | useMutationWithToast -> useErrorHandler |
| `uses_hook` | ~2 | FamilyList -> useFamilies |

Most edges are hook -> API function bindings, which is expected given the architecture.

## Architecture Patterns Detected

### 1. TanStack Query Pattern
All hooks follow the TanStack Query pattern:
```typescript
export function useHook(...) {
  return useQuery/useMutation({
    queryKey: [...],
    queryFn: () => apiFunction(...),
  });
}
```

### 2. Query Invalidation
Components that mutate data properly invalidate related queries:
```typescript
onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: ['families'] });
}
```

### 3. Separation of Concerns
Clear layering:
- **services/api/*.ts** - HTTP communication
- **hooks/*.ts** - State management (TanStack Query)
- **components/**/*.tsx** - UI logic

### 4. Anti-Pattern Detection
The graph flags components that call API functions directly (would bypass hooks):
- No components detected calling API client directly (good!)
- All components use hooks (proper pattern)

## Type Coverage

### DTO Alignment

API types like `PersonDetailDto`, `FamilyDetailDto` should map to backend DTOs:
- Frontend types found: 109
- Backend DTOs would be similar count
- Types appear correctly structured for API contracts

### Required vs Optional

Proper use of optional properties:
- Properties like `photoUrl?: string` (optional)
- Properties like `firstName: string` (required)

## Performance Considerations

### Bundle Size
- 69 API functions across multiple service files
- 71 hooks that will be tree-shaken if unused
- Component extraction helps identify dead code

### Cache Keys
Query keys are properly structured:
- Single string keys: `['families']`, `['people']`
- Composite keys: `['families', params]` for cache busting

## Next Steps for Validation

1. **Cross-reference with backend graph**:
   - Compare frontend types with backend DTOs
   - Validate API endpoint contracts
   - Check for missing or orphaned endpoints

2. **Component visualization**:
   - Generate dependency tree diagrams
   - Identify deeply nested hook dependencies
   - Find unused components

3. **Type contract validation**:
   - Ensure request/response types match backend
   - Detect type misalignments early in development

4. **Integration testing**:
   - Generate integration tests from graph
   - Validate frontend-backend contracts automatically

