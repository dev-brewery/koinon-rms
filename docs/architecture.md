# Koinon RMS Architecture: Linux Container Stack with React Frontend

## Executive Summary

Koinon RMS is a church/relationship management system built for Linux containers using **ASP.NET Core Web API** with a **React TypeScript frontend**, optimized for performance-critical deployments including WiFi-connected check-in kiosks.

---

## Why React + ASP.NET Core API?

### Performance Priority: Check-in Kiosks on WiFi

The requirement for snappy check-in kiosks on WiFi connections eliminates Blazor Server, which requires:
- Persistent SignalR connections (problematic on spotty WiFi)
- Server round-trips for every interaction (50-200ms+ latency)
- Higher memory per concurrent user

### Architecture Comparison

| Metric | Blazor Server | React + API |
|--------|---------------|-------------|
| Interaction latency | 50-200ms (network bound) | <10ms (local) |
| WiFi resilience | Poor (connection required) | Excellent (works offline) |
| Kiosk responsiveness | Sluggish on poor connections | Instant feedback |
| Scalability | Limited by SignalR connections | Stateless, unlimited |
| Initial load | Fast | Fast (with code splitting) |
| Offline capability | None | PWA possible |

### React Specifically

- **Component model**: Self-contained UI units with props/state
- **Ecosystem maturity**: Rich library support for forms, tables, drag-drop
- **TypeScript**: Strong typing catches errors at compile time
- **Talent pool**: Easier to hire React developers than Blazor specialists
- **Performance tooling**: React DevTools, profilers, optimization patterns well-documented

---

## Target Architecture

### Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 18+ with TypeScript, Vite, TanStack Query |
| API | ASP.NET Core 8.0 Web API |
| ORM | Entity Framework Core 8.0 |
| Database | PostgreSQL 16+ |
| Caching | Redis |
| Search | PostgreSQL full-text |
| Runtime | Linux containers (Alpine-based) |
| Orchestration | Docker Compose / Kubernetes |

### System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         CDN / Edge                              │
│         (React SPA, static assets, cached API responses)        │
└─────────────────────────────┬───────────────────────────────────┘
                              │
┌─────────────────────────────▼───────────────────────────────────┐
│                      Load Balancer                              │
│                    (nginx / Traefik)                            │
└─────────────────────────────┬───────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
   ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
   │  API Server  │   │  API Server  │   │  API Server  │
   │  (Stateless) │   │  (Stateless) │   │  (Stateless) │
   │  ASP.NET Core│   │  ASP.NET Core│   │  ASP.NET Core│
   └──────┬───────┘   └──────┬───────┘   └──────┬───────┘
          │                  │                  │
          └──────────────────┼──────────────────┘
                             │
              ┌──────────────┴──────────────┐
              ▼                             ▼
   ┌─────────────────────┐      ┌─────────────────────┐
   │   PostgreSQL        │      │      Redis          │
   │   (Primary +        │      │   (Cache + Session) │
   │    Read Replicas)   │      │                     │
   └─────────────────────┘      └─────────────────────┘
```

### Check-in Kiosk Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Check-in Kiosk (Browser)                     │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    React PWA                              │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │  Service Worker                                     │  │  │
│  │  │  - Caches static assets                            │  │  │
│  │  │  - Caches family/person lookup data                │  │  │
│  │  │  - Queues check-ins during offline                 │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  │                                                           │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │  Check-in Components                               │  │  │
│  │  │  - Instant touch response (<10ms)                  │  │  │
│  │  │  - Optimistic UI updates                           │  │  │
│  │  │  - Background sync with server                     │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────┘
                              │ WiFi (when available)
                              ▼
                         API Servers
```

**Key Kiosk Features:**
- **Instant response**: All UI feedback is immediate; server sync happens in background
- **Offline resilience**: Check-ins queue locally if WiFi drops, sync when restored
- **Pre-cached data**: Family lookups cached during idle time
- **PWA installation**: Runs in kiosk mode without browser chrome

---

## Project Structure

```
koinon-rms/
├── src/
│   ├── Koinon.Domain/               # Domain models, interfaces
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Extensions/
│   │
│   ├── Koinon.Infrastructure/       # EF Core, repositories
│   │   ├── Data/
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   └── Migrations/
│   │
│   ├── Koinon.Application/          # Business logic
│   │   ├── Services/
│   │   ├── DTOs/
│   │   └── Validators/
│   │
│   ├── Koinon.Api/                  # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   │
│   └── web/                         # React frontend
│       ├── src/
│       │   ├── components/
│       │   │   ├── blocks/          # Feature blocks
│       │   │   ├── controls/        # Reusable form controls
│       │   │   └── layout/          # Page layouts
│       │   ├── features/
│       │   │   ├── people/
│       │   │   ├── groups/
│       │   │   ├── checkin/
│       │   │   └── admin/
│       │   ├── hooks/
│       │   ├── services/            # API client
│       │   ├── stores/              # State management
│       │   └── utils/
│       ├── package.json
│       └── vite.config.ts
│
├── tests/
│   ├── Koinon.Domain.Tests/
│   ├── Koinon.Application.Tests/
│   ├── Koinon.Infrastructure.Tests/
│   └── Koinon.Api.Tests/
│
├── docker/
│   ├── api.Dockerfile
│   ├── web.Dockerfile
│   └── docker-compose.yml
│
└── k8s/
    ├── base/
    └── overlays/
```

---

## API Design

### RESTful Resource Structure

```
/api/v1/
├── people/
│   ├── GET    /                      # List/search people
│   ├── POST   /                      # Create person
│   ├── GET    /{idKey}               # Get person
│   ├── PUT    /{idKey}               # Update person
│   ├── DELETE /{idKey}               # Delete person
│   ├── GET    /{idKey}/family        # Get family members
│   ├── GET    /{idKey}/groups        # Get group memberships
│   └── GET    /{idKey}/history       # Get history
│
├── families/
│   ├── GET    /
│   ├── POST   /
│   ├── GET    /{idKey}
│   ├── PUT    /{idKey}
│   └── GET    /{idKey}/members
│
├── groups/
│   ├── GET    /
│   ├── POST   /
│   ├── GET    /{idKey}
│   ├── PUT    /{idKey}
│   ├── GET    /{idKey}/members
│   └── POST   /{idKey}/members
│
├── checkin/
│   ├── GET    /configuration         # Get check-in config
│   ├── POST   /search                 # Search for family
│   ├── GET    /opportunities/{familyId}  # Available check-in options
│   ├── POST   /attendance             # Record check-in
│   └── POST   /checkout               # Record check-out
│
├── communications/
│   ├── POST   /email
│   ├── POST   /sms
│   └── GET    /templates
│
└── admin/
    ├── /defined-types
    ├── /campuses
    ├── /group-types
    └── /workflows
```

### API Response Patterns

```typescript
// Standard success response
interface ApiResponse<T> {
  data: T;
  meta?: {
    page?: number;
    pageSize?: number;
    totalCount?: number;
  };
}

// Error response
interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, string[]>;
  };
}

// Example: Person response
interface PersonResponse {
  idKey: string;
  firstName: string;
  lastName: string;
  email?: string;
  photoUrl?: string;
  connectionStatus: string;
  recordStatus: string;
  primaryFamily?: FamilySummary;
}
```

### Controller Example

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PeopleController : ControllerBase
{
    private readonly IPersonService _personService;
    private readonly ILogger<PeopleController> _logger;

    public PeopleController(
        IPersonService personService,
        ILogger<PeopleController> logger)
    {
        _personService = personService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PersonSummaryDto>>), 200)]
    public async Task<IActionResult> GetPeople(
        [FromQuery] PersonSearchParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _personService.SearchAsync(parameters, cancellationToken);

        return Ok(new ApiResponse<IEnumerable<PersonSummaryDto>>
        {
            Data = result.Items,
            Meta = new {
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalCount = result.TotalCount
            }
        });
    }

    [HttpGet("{idKey}")]
    [ProducesResponseType(typeof(ApiResponse<PersonDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPerson(
        string idKey,
        CancellationToken cancellationToken)
    {
        var person = await _personService.GetByIdKeyAsync(idKey, cancellationToken);

        if (person is null)
            return NotFound();

        return Ok(new ApiResponse<PersonDto> { Data = person });
    }

    [HttpPost]
    [Authorize(Policy = "EditPeople")]
    [ProducesResponseType(typeof(ApiResponse<PersonDto>), 201)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> CreatePerson(
        [FromBody] CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _personService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new ApiError { Error = result.Error });

        return CreatedAtAction(
            nameof(GetPerson),
            new { idKey = result.Value.IdKey },
            new ApiResponse<PersonDto> { Data = result.Value });
    }
}
```

---

## React Frontend Architecture

### Component Pattern

```typescript
// src/components/blocks/PersonBio/PersonBio.tsx
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { personApi } from '@/services/api';
import { Panel, PersonPhoto, ContactInfo } from '@/components/controls';
import { BlockSkeleton } from '@/components/layout';

interface PersonBioBlockProps {
  blockId: string;
  settings: PersonBioSettings;
}

interface PersonBioSettings {
  showPhoto: boolean;
  showContactInfo: boolean;
  showBadges: boolean;
}

export function PersonBioBlock({ settings }: PersonBioBlockProps) {
  const { personId } = useParams<{ personId: string }>();

  const { data: person, isLoading, error } = useQuery({
    queryKey: ['person', personId],
    queryFn: () => personApi.get(personId!),
    staleTime: 30_000,
    enabled: !!personId,
  });

  if (isLoading) return <BlockSkeleton />;
  if (error) return <BlockError error={error} />;
  if (!person) return <BlockNotFound entity="Person" />;

  return (
    <Panel title="Bio" editUrl={`/person/${personId}/edit`}>
      <div className="flex gap-4">
        {settings.showPhoto && (
          <PersonPhoto
            person={person}
            size="lg"
            className="flex-shrink-0"
          />
        )}

        <div className="flex-grow">
          <h1 className="text-2xl font-semibold">
            {person.fullName}
          </h1>

          {settings.showBadges && (
            <PersonBadges person={person} className="mt-2" />
          )}

          {settings.showContactInfo && (
            <ContactInfo
              email={person.email}
              phone={person.phone}
              address={person.primaryAddress}
              className="mt-4"
            />
          )}
        </div>
      </div>
    </Panel>
  );
}
```

### Check-in Kiosk Component (Performance Optimized)

```typescript
// src/features/checkin/CheckinKiosk.tsx
import { useState, useCallback, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { checkinApi } from '@/services/api';
import { useOfflineQueue } from '@/hooks/useOfflineQueue';

export function CheckinKiosk() {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedFamily, setSelectedFamily] = useState<Family | null>(null);
  const queryClient = useQueryClient();
  const { queueAction, isOnline } = useOfflineQueue();

  // Prefetch common data during idle time
  useEffect(() => {
    if ('requestIdleCallback' in window) {
      requestIdleCallback(() => {
        queryClient.prefetchQuery({
          queryKey: ['checkin', 'config'],
          queryFn: checkinApi.getConfiguration,
        });
      });
    }
  }, [queryClient]);

  // Family search with debouncing
  const { data: searchResults, isFetching } = useQuery({
    queryKey: ['checkin', 'search', searchTerm],
    queryFn: () => checkinApi.searchFamilies(searchTerm),
    enabled: searchTerm.length >= 2,
    staleTime: 60_000,
    placeholderData: (prev) => prev,
  });

  // Check-in mutation with optimistic updates
  const checkinMutation = useMutation({
    mutationFn: checkinApi.recordAttendance,
    onMutate: async (checkinData) => {
      await queryClient.cancelQueries({
        queryKey: ['checkin', 'opportunities', selectedFamily?.id]
      });

      const previous = queryClient.getQueryData(
        ['checkin', 'opportunities', selectedFamily?.id]
      );

      queryClient.setQueryData(
        ['checkin', 'opportunities', selectedFamily?.id],
        (old: any) => ({
          ...old,
          checkedIn: [...(old?.checkedIn ?? []), checkinData.personId],
        })
      );

      return { previous };
    },
    onError: (err, variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(
          ['checkin', 'opportunities', selectedFamily?.id],
          context.previous
        );
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({
        queryKey: ['checkin', 'opportunities', selectedFamily?.id]
      });
    },
  });

  const handleCheckin = useCallback(async (
    personId: string,
    groupId: string,
    locationId: string
  ) => {
    const checkinData = { personId, groupId, locationId };

    if (isOnline) {
      checkinMutation.mutate(checkinData);
    } else {
      queueAction({
        type: 'CHECKIN',
        payload: checkinData,
        timestamp: Date.now(),
      });
    }
  }, [checkinMutation, isOnline, queueAction]);

  return (
    <div className="min-h-screen bg-gray-100 p-4">
      {!isOnline && (
        <OfflineBanner onSync={() => {}} />
      )}

      <SearchInput
        value={searchTerm}
        onChange={setSearchTerm}
        isLoading={isFetching}
        placeholder="Enter phone or name..."
      />

      {searchResults && !selectedFamily && (
        <FamilyList
          families={searchResults}
          onSelect={setSelectedFamily}
        />
      )}

      {selectedFamily && (
        <CheckinOpportunities
          family={selectedFamily}
          onCheckin={handleCheckin}
          onBack={() => setSelectedFamily(null)}
        />
      )}
    </div>
  );
}
```

---

## Database Schema

### PostgreSQL Example

```sql
-- Example: Person table
CREATE TABLE person (
    id SERIAL PRIMARY KEY,
    id_key VARCHAR(22) NOT NULL UNIQUE,
    guid UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),

    first_name VARCHAR(50) NOT NULL,
    nick_name VARCHAR(50),
    last_name VARCHAR(50) NOT NULL,

    email VARCHAR(75),
    is_email_active BOOLEAN NOT NULL DEFAULT true,

    gender SMALLINT NOT NULL DEFAULT 0,
    birth_date DATE,

    record_status_value_id INTEGER REFERENCES defined_value(id),
    connection_status_value_id INTEGER REFERENCES defined_value(id),

    primary_family_id INTEGER REFERENCES family(id),

    photo_id INTEGER REFERENCES binary_file(id),

    created_date_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_date_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_person_alias_id INTEGER,
    modified_by_person_alias_id INTEGER,

    -- Full text search
    search_vector TSVECTOR GENERATED ALWAYS AS (
        setweight(to_tsvector('english', coalesce(first_name, '')), 'A') ||
        setweight(to_tsvector('english', coalesce(last_name, '')), 'A') ||
        setweight(to_tsvector('english', coalesce(nick_name, '')), 'B') ||
        setweight(to_tsvector('english', coalesce(email, '')), 'C')
    ) STORED
);

CREATE INDEX idx_person_search ON person USING GIN(search_vector);
CREATE INDEX idx_person_email ON person(email) WHERE email IS NOT NULL;
CREATE INDEX idx_person_family ON person(primary_family_id);
```

### EF Core Configuration

```csharp
public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("person");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.IdKey)
            .HasMaxLength(22)
            .IsRequired();

        builder.HasIndex(p => p.IdKey)
            .IsUnique();

        builder.Property(p => p.FirstName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasMaxLength(75);

        builder.HasOne(p => p.PrimaryFamily)
            .WithMany(f => f.Members)
            .HasForeignKey(p => p.PrimaryFamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        // PostgreSQL full-text search
        builder.HasGeneratedTsVectorColumn(
            p => p.SearchVector,
            "english",
            p => new { p.FirstName, p.LastName, p.NickName, p.Email })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");
    }
}
```

---

## Container Configuration

### API Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy and restore
COPY ["src/Koinon.Api/Koinon.Api.csproj", "Koinon.Api/"]
COPY ["src/Koinon.Domain/Koinon.Domain.csproj", "Koinon.Domain/"]
COPY ["src/Koinon.Infrastructure/Koinon.Infrastructure.csproj", "Koinon.Infrastructure/"]
COPY ["src/Koinon.Application/Koinon.Application.csproj", "Koinon.Application/"]
RUN dotnet restore "Koinon.Api/Koinon.Api.csproj"

# Copy source and publish
COPY src/ .
RUN dotnet publish "Koinon.Api/Koinon.Api.csproj" \
    -c Release \
    -o /app \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app

RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Koinon.Api.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: docker/api.Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Koinon=Host=db;Database=koinon;Username=koinon;Password=${DB_PASSWORD}
      - Redis__ConnectionString=redis:6379
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_started
    healthcheck:
      test: ["CMD", "wget", "--spider", "-q", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build:
      context: .
      dockerfile: docker/web.Dockerfile
    ports:
      - "80:80"
    depends_on:
      - api

  db:
    image: postgres:16-alpine
    environment:
      - POSTGRES_USER=koinon
      - POSTGRES_PASSWORD=${DB_PASSWORD}
      - POSTGRES_DB=koinon
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U koinon"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

---

## Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| Check-in touch response | <10ms | Time to visual feedback |
| Check-in complete (online) | <200ms | Family search to label print |
| Check-in complete (offline) | <50ms | Queued locally |
| API response (simple) | <50ms | p95 latency |
| API response (complex) | <200ms | p95 latency |
| Initial page load | <2s | First contentful paint |
| Bundle size (gzipped) | <200KB | Main app chunk |
| Lighthouse score | >90 | Performance category |

---

## Success Criteria

1. **Performance**: Check-in kiosks respond instantly on WiFi with 200ms+ latency
2. **Reliability**: 99.9% uptime; graceful offline handling
3. **Cost**: Reduced hosting costs via Linux containers
4. **Scalability**: Linear scaling with container replicas
5. **Developer Experience**: <30 min to running dev environment
6. **Feature Completeness**: All critical ChMS features functional
