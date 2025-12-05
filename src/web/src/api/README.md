# API Client

This directory contains the TypeScript API client for the Koinon RMS backend.

## Generated Client

The `generated-client.ts` file is **auto-generated** from the OpenAPI specification. Do not edit it manually.

### Regenerating the Client

```bash
# From project root
.claude/scripts/generate-api-client.sh
```

This will:
1. Build the .NET API project
2. Extract the OpenAPI specification
3. Generate TypeScript types and client classes

## Usage

### Basic Example

```typescript
import { PeopleClient, CreatePersonRequest } from '@/api/generated-client';
import { useApiClient } from '@/hooks/useApiClient';

function MyComponent() {
  const client = useApiClient(PeopleClient);

  const handleCreate = async () => {
    const request: CreatePersonRequest = {
      firstName: 'John',
      lastName: 'Doe',
      // ... other fields
    };

    const person = await client.create(request);
    console.log('Created:', person);
  };

  return <button onClick={handleCreate}>Create Person</button>;
}
```

### With React Query

```typescript
import { useMutation, useQuery } from '@tanstack/react-query';
import { PeopleClient } from '@/api/generated-client';
import { useApiClient } from '@/hooks/useApiClient';

function usePerson(idKey: string) {
  const client = useApiClient(PeopleClient);

  return useQuery({
    queryKey: ['person', idKey],
    queryFn: () => client.getByIdKey(idKey),
  });
}

function useCreatePerson() {
  const client = useApiClient(PeopleClient);

  return useMutation({
    mutationFn: (request: CreatePersonRequest) => client.create(request),
  });
}
```

## Custom API Wrapper

For custom logic (auth, error handling, etc.), create wrapper functions:

```typescript
// src/api/people.ts
import { PeopleClient, CreatePersonRequest } from './generated-client';
import { apiClient } from './client';

export async function createPerson(request: CreatePersonRequest) {
  const client = new PeopleClient(apiClient);

  try {
    return await client.create(request);
  } catch (error) {
    // Custom error handling
    throw new AppError('Failed to create person', error);
  }
}
```

## Configuration

The API base URL is configured via environment variables:

```env
# .env.local
VITE_API_BASE_URL=http://localhost:5000
```

## Type Safety

The generated client provides full TypeScript type safety:
- Request/response types match API DTOs exactly
- Enums are properly typed
- Optional fields are nullable
- Validation errors are typed

## Updating After API Changes

When the API changes:
1. Make your changes to controllers/DTOs
2. Build the API project
3. Run `.claude/scripts/generate-api-client.sh`
4. Review the TypeScript changes
5. Update frontend code to match new types
6. Run `npm run typecheck` to catch breaking changes

## CI/CD Integration

The GitHub Actions workflow automatically:
- Validates the OpenAPI spec is valid
- Checks if generated client is up to date
- Fails the build if frontend types don't match API

This ensures backend and frontend stay in sync.
