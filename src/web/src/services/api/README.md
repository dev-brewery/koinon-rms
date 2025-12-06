# Koinon RMS API Client

Type-safe API client for Koinon RMS using native fetch with automatic token refresh.

## Features

- Native fetch API (no axios dependency)
- Automatic JWT token refresh on 401
- Token storage in memory (not localStorage for security)
- Full TypeScript type safety
- Request cancellation support via AbortController
- Error handling with typed errors

## Usage

### Authentication

```typescript
import { authApi, setTokens } from '@/services/api';

// Login
const result = await authApi.login({
  username: 'user@example.com',
  password: 'password123'
});

// Tokens are automatically stored in memory
console.log(result.user.firstName);
```

### People API

```typescript
import { peopleApi } from '@/services/api';

// Search people
const results = await peopleApi.searchPeople({
  q: 'John',
  page: 1,
  pageSize: 25
});

// Get person details
const person = await peopleApi.getPersonByIdKey('abc123...');

// Create person
const newPerson = await peopleApi.createPerson({
  firstName: 'John',
  lastName: 'Doe',
  email: 'john@example.com'
});

// Update person
const updated = await peopleApi.updatePerson('abc123...', {
  email: 'newemail@example.com'
});
```

### Families API

```typescript
import { familiesApi } from '@/services/api';

// Get family details
const family = await familiesApi.getFamilyByIdKey('family123...');

// Add family member
const member = await familiesApi.addFamilyMember('family123...', {
  personId: 'person123...',
  roleId: 'adult-role-id...'
});
```

### Check-in API

```typescript
import { checkinApi } from '@/services/api';

// Get check-in configuration
const config = await checkinApi.getCheckinConfiguration({
  campusId: 'campus123...'
});

// Search families for check-in
const families = await checkinApi.searchFamiliesForCheckin({
  searchValue: '5551234567',
  searchType: 'Phone'
});

// Record attendance
const result = await checkinApi.recordAttendance({
  checkins: [
    {
      personIdKey: 'person123...',
      groupIdKey: 'group123...',
      locationIdKey: 'location123...',
      scheduleIdKey: 'schedule123...'
    }
  ]
});
```

### Error Handling

```typescript
import { peopleApi, ApiClientError } from '@/services/api';

try {
  const person = await peopleApi.getPersonByIdKey('invalid-id');
} catch (error) {
  if (error instanceof ApiClientError) {
    console.error('API Error:', error.error.message);
    console.error('Status:', error.statusCode);

    // Field-level validation errors
    if (error.error.details) {
      Object.entries(error.error.details).forEach(([field, errors]) => {
        console.error(`${field}: ${errors.join(', ')}`);
      });
    }
  }
}
```

### Request Cancellation

```typescript
import { peopleApi } from '@/services/api';

// Create abort controller
const controller = new AbortController();

// Pass signal to request
const promise = peopleApi.searchPeople({
  q: 'John'
}, {
  signal: controller.signal
});

// Cancel request
controller.abort();
```

### With TanStack Query

```typescript
import { useQuery, useMutation } from '@tanstack/react-query';
import { peopleApi } from '@/services/api';

// Query
function usePerson(idKey: string) {
  return useQuery({
    queryKey: ['person', idKey],
    queryFn: () => peopleApi.getPersonByIdKey(idKey)
  });
}

// Mutation
function useCreatePerson() {
  return useMutation({
    mutationFn: peopleApi.createPerson,
    onSuccess: (data) => {
      console.log('Person created:', data.fullName);
    }
  });
}

// Usage in component
function PersonDetails({ idKey }: { idKey: string }) {
  const { data: person, isLoading, error } = usePerson(idKey);

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return <div>{person.fullName}</div>;
}
```

## API Services

- **authApi** - Authentication (login, logout, refresh)
- **peopleApi** - People management
- **familiesApi** - Family management
- **groupsApi** - Group management
- **checkinApi** - Check-in operations
- **referenceApi** - Reference data (campuses, defined types, etc.)

## Token Management

Tokens are stored in memory for security (not localStorage):

```typescript
import { setTokens, clearTokens, getAccessToken } from '@/services/api';

// Set tokens (done automatically by login)
setTokens('access-token', 'refresh-token');

// Get current access token
const token = getAccessToken();

// Clear tokens (logout)
clearTokens();
```

## Environment Variables

Configure API URL in `.env`:

```bash
VITE_API_URL=http://localhost:5000/api/v1
```

Default: `http://localhost:5000/api/v1`
