# CLAUDE.md - Koinon RMS Project Intelligence

You are an expert software architect and developer working on **Koinon RMS**, a ground-up implementation of a Church Management System targeting Linux containers with a modern tech stack. This is a greenfield project—you are building from scratch with clean architecture principles.

---

## Project Identity

**Name:** Koinon RMS
**Purpose:** Cross-platform, container-native Church Management System
**Status:** Greenfield development, starting from empty repository

**What this IS:**
- A complete architecture using modern .NET 8 and React
- An opportunity to do things right without legacy constraints
- Focused on performance-critical check-in as the MVP

**What this is NOT:**
- A fork or modification of any existing ChMS
- A port of legacy WebForms to React
- Backwards compatible with legacy plugins/blocks

---

## Repository Structure

```
koinon-rms/
├── CLAUDE.md                      # This file - read first!
├── README.md                      # Project overview
├── docker-compose.yml             # Dev infrastructure (postgres, redis)
├── docker-compose.full.yml        # Full stack for integration testing
├── global.json                    # .NET SDK version pin
├── .editorconfig                  # Code style rules
├── .gitignore
│
├── docs/
│   ├── architecture.md            # High-level architecture decisions
│   └── reference/
│       ├── api-contracts.md       # REST API TypeScript interfaces
│       ├── entity-mappings.md     # Entity field mappings
│       └── work-breakdown.md      # Agent work unit specifications
│
├── src/
│   ├── Koinon.Domain/             # Entities, enums, interfaces (no dependencies)
│   ├── Koinon.Application/        # Use cases, DTOs, validators
│   ├── Koinon.Infrastructure/     # EF Core, Redis, external services
│   ├── Koinon.Api/                # ASP.NET Core Web API
│   └── web/                       # React frontend
│
├── tests/
│   ├── Koinon.Domain.Tests/
│   ├── Koinon.Application.Tests/
│   ├── Koinon.Infrastructure.Tests/
│   └── Koinon.Api.Tests/
│
└── tools/
    └── db-init/                   # Database initialization scripts
```

---

## Technology Stack

### Backend
- **.NET 8** (ASP.NET Core Web API)
- **Entity Framework Core 8** with PostgreSQL provider (Npgsql)
- **PostgreSQL 16+** (primary database)
- **Redis** (caching, session state, distributed locks)
- **JWT** authentication with refresh tokens
- **MediatR** for CQRS pattern
- **FluentValidation** for request validation
- **AutoMapper** for DTO mapping

### Frontend
- **React 18** with TypeScript (strict mode)
- **Vite** for build tooling
- **TanStack Query** (React Query) for server state
- **TailwindCSS** for styling
- **React Router** for navigation
- **PWA** with Workbox for offline capability

### Infrastructure
- **Docker** containers (multi-stage builds)
- **Docker Compose** for local development
- Target: Kubernetes for production

---

## Architecture Principles

### 1. Clean Architecture Layers

Dependencies flow inward only:
```
API → Application → Domain
         ↓
    Infrastructure
```

| Layer | Responsibility | Dependencies |
|-------|----------------|--------------|
| Domain | Entities, enums, interfaces | None |
| Application | Use cases, DTOs, validation | Domain |
| Infrastructure | EF Core, Redis, external APIs | Domain, Application |
| Api | Controllers, middleware, auth | All |

### 2. Entity Design

All entities inherit from `Entity` base class:

```csharp
namespace Koinon.Domain.Entities;

public abstract class Entity : IEntity, IAuditable
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();

    // Computed - never stored in DB
    public string IdKey => IdKeyHelper.Encode(Id);

    // Audit fields
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public int? CreatedByPersonAliasId { get; set; }
    public int? ModifiedByPersonAliasId { get; set; }
}
```

### 3. Database Conventions

| Aspect | Convention | Example |
|--------|------------|---------|
| Table names | snake_case | `group_member` |
| Column names | snake_case | `first_name` |
| C# properties | PascalCase | `FirstName` |
| Primary keys | `id` (int, identity) | |
| Foreign keys | `{entity}_id` | `person_id` |
| Unique constraints | `uix_{table}_{columns}` | `uix_person_guid` |
| Indexes | `ix_{table}_{columns}` | `ix_person_last_name` |

### 4. API Design

- Base path: `/api/v1/`
- Use `IdKey` in URLs, never integer IDs
- Standard response envelopes (see `docs/reference/api-contracts.md`)

### 5. Frontend Patterns

- Functional components only (no class components)
- TanStack Query for all server state
- Custom hooks for shared logic
- Strict TypeScript (no `any`)

---

## Key Documentation

Before implementing, always check the relevant documentation:

| Document | When to Reference |
|----------|-------------------|
| `docs/reference/entity-mappings.md` | Implementing any entity |
| `docs/reference/api-contracts.md` | Implementing any API endpoint |
| `docs/reference/work-breakdown.md` | Understanding work unit scope |
| `docs/architecture.md` | Major architectural decisions |

---

## Check-in MVP Focus

The primary use case driving architecture decisions is **Sunday morning check-in kiosks**.

### Performance Requirements

| Metric | Target |
|--------|--------|
| Touch response | <10ms (client-side) |
| Check-in complete (online) | <200ms |
| Check-in complete (offline) | <50ms |
| Family lookup | <100ms with 10k+ people |
| Label print | <500ms |

### Kiosk Environment

- WiFi connectivity (variable quality)
- Tablet devices (768px+ width)
- Touch targets minimum 48px
- Must work during network outages
- Networked label printers (Zebra, Brother)

---

## Development Commands

```bash
# Infrastructure
docker-compose up -d                    # Start postgres + redis
docker-compose --profile tools up -d    # Include admin UIs
docker-compose down                     # Stop
docker-compose down -v                  # Stop and delete data

# Backend (from repo root)
dotnet build                            # Build all projects
dotnet test                             # Run all tests
dotnet watch run --project src/Koinon.Api  # Run API with hot reload

# Migrations (from repo root)
dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Frontend (from src/web)
npm install                             # Install dependencies
npm run dev                             # Start dev server
npm run build                           # Production build
npm run typecheck                       # TypeScript check
npm run lint                            # ESLint

# Full stack in Docker
docker-compose -f docker-compose.full.yml up --build
```

---

## Coding Standards

### C#

```csharp
// File-scoped namespaces
namespace Koinon.Domain.Entities;

// Primary constructors for services
public class PersonService(KoinonDbContext context, ILogger<PersonService> logger)
{
    public async Task<Person?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.People
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}

// Records for DTOs
public record PersonDto(
    string IdKey,
    string FirstName,
    string? NickName,
    string LastName,
    string FullName);

// Required modifier for required properties
public class Person : Entity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? NickName { get; set; }
}
```

### TypeScript

```typescript
// Strict types - no 'any'
interface PersonSummary {
  idKey: string;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
}

// Functional components with destructured props
interface PersonCardProps {
  person: PersonSummary;
  onSelect?: (person: PersonSummary) => void;
}

export function PersonCard({ person, onSelect }: PersonCardProps) {
  return (
    <button
      onClick={() => onSelect?.(person)}
      className="p-4 rounded-lg hover:bg-gray-100"
    >
      {person.fullName}
    </button>
  );
}
```

---

## Anti-Patterns to Avoid

### Legacy Patterns
- ❌ ViewState or server-side state management
- ❌ Page lifecycle patterns
- ❌ Server controls (`runat="server"`)
- ❌ Legacy templating (use React components)
- ❌ Block/Zone architecture

### General
- ❌ Exposing integer IDs in URLs (use IdKey)
- ❌ N+1 queries (use Include/ThenInclude)
- ❌ Synchronous database calls
- ❌ Business logic in controllers
- ❌ Direct DbContext outside Infrastructure
- ❌ `any` type in TypeScript
- ❌ Class components in React

---

## Work Unit Execution

Work units are defined in `docs/reference/work-breakdown.md`.

### Before Starting a Work Unit

1. Read the work unit specification completely
2. Check referenced documentation
3. Understand acceptance criteria

### Definition of Done

- [ ] All acceptance criteria met
- [ ] Code compiles with zero warnings
- [ ] Unit tests pass
- [ ] Follows conventions in this document
- [ ] No TODO comments (create issues instead)

### Current Phase: Foundation

Starting with WU-1.1.x (solution scaffolding) and WU-1.2.x (core entities).

Priority order:
1. Solution structure and project references
2. Base entity classes and interfaces
3. Core entities (Person, Group, etc.)
4. DbContext and configurations
5. Initial migration

---

## Quick Reference

### Connection Strings (Development)

```
PostgreSQL: Host=localhost;Port=5432;Database=koinon;Username=koinon;Password=koinon
Redis: localhost:6379
```

### Ports (Development)

| Service | Port |
|---------|------|
| PostgreSQL | 5432 |
| Redis | 6379 |
| API | 5000 |
| Web (Vite) | 5173 |
| Adminer | 8080 |
| Redis Commander | 8081 |

### NuGet Packages (Core)

```xml
<!-- Domain - minimal dependencies -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />

<!-- Application -->
<PackageReference Include="MediatR" />
<PackageReference Include="FluentValidation" />
<PackageReference Include="AutoMapper" />

<!-- Infrastructure -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
<PackageReference Include="StackExchange.Redis" />

<!-- API -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Swashbuckle.AspNetCore" />
```

---

## Questions? Check Here First

| Question | Answer Location |
|----------|-----------------|
| Entity field mapping? | `docs/reference/entity-mappings.md` |
| API endpoint contract? | `docs/reference/api-contracts.md` |
| Work unit scope? | `docs/reference/work-breakdown.md` |
| Architecture decision? | `docs/architecture.md` |
