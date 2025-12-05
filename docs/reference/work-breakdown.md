# Koinon RMS: AI Agent Work Breakdown Structure

## Overview

This document breaks the Koinon RMS development into discrete, context-window-friendly work units designed for Claude Code agents. Each work unit is:

- **Self-contained**: Can be completed without knowledge of other units
- **Bounded**: Fits within agent context limits (~100-200KB of relevant code)
- **Testable**: Has clear acceptance criteria
- **Documented**: Includes all necessary context in the task description

---

## Work Unit Principles

### Size Guidelines

| Unit Type | Target Size | Context Budget |
|-----------|-------------|----------------|
| Entity port | 1-3 related entities | ~50KB |
| Service layer | 1 service + tests | ~80KB |
| API controller | 1 controller + DTOs | ~60KB |
| React component | 1-3 related components | ~80KB |
| Integration | Single integration point | ~100KB |

### Task Template

Each work unit should include:

```markdown
## Task: [Name]

### Context
[Brief description of where this fits in the system]

### Input Artifacts
- [Files/schemas the agent needs to reference]

### Output Artifacts
- [Files the agent will create/modify]

### Acceptance Criteria
- [ ] [Specific, testable requirement]
- [ ] [Another requirement]

### Reference Examples
[Link to similar completed work or patterns to follow]

### Constraints
- [Technology constraints]
- [Pattern constraints]
- [What NOT to do]
```

---

## Phase 1: Foundation

### 1.1 Project Scaffolding

#### WU-1.1.1: Solution Structure
```markdown
## Task: Create Solution Structure

### Context
Initialize the .NET solution with all project shells and references.

### Input Artifacts
- None (greenfield)

### Output Artifacts
- koinon-rms.sln
- src/Koinon.Domain/Koinon.Domain.csproj
- src/Koinon.Infrastructure/Koinon.Infrastructure.csproj
- src/Koinon.Application/Koinon.Application.csproj
- src/Koinon.Api/Koinon.Api.csproj
- tests/Koinon.Domain.Tests/Koinon.Domain.Tests.csproj
- tests/Koinon.Infrastructure.Tests/Koinon.Infrastructure.Tests.csproj
- tests/Koinon.Application.Tests/Koinon.Application.Tests.csproj
- tests/Koinon.Api.Tests/Koinon.Api.Tests.csproj
- .editorconfig
- Directory.Build.props
- Directory.Packages.props (central package management)

### Acceptance Criteria
- [ ] Solution builds with `dotnet build`
- [ ] All projects target .NET 8.0
- [ ] Central package management configured
- [ ] Project references correctly established
- [ ] EditorConfig enforces consistent style

### Constraints
- Use minimal API style for Koinon.Api
- Enable nullable reference types in all projects
- Use file-scoped namespaces
```

#### WU-1.1.2: React Project Scaffolding
```markdown
## Task: Create React Frontend Project

### Context
Initialize the React TypeScript frontend with Vite, TanStack Query, and TailwindCSS.

### Input Artifacts
- None

### Output Artifacts
- src/web/package.json
- src/web/vite.config.ts
- src/web/tsconfig.json
- src/web/tailwind.config.js
- src/web/src/main.tsx
- src/web/src/App.tsx
- src/web/src/index.css
- src/web/src/vite-env.d.ts

### Acceptance Criteria
- [ ] `npm install` completes without errors
- [ ] `npm run dev` starts development server
- [ ] `npm run build` produces production bundle
- [ ] TypeScript strict mode enabled
- [ ] TailwindCSS classes work
- [ ] TanStack Query provider configured

### Constraints
- React 18+
- Vite 5+
- TypeScript strict mode
- No CSS-in-JS (use Tailwind only)
```

#### WU-1.1.3: Docker Development Environment
```markdown
## Task: Create Docker Development Environment

### Context
Docker Compose configuration for local development with hot reload.

### Input Artifacts
- Solution structure from WU-1.1.1

### Output Artifacts
- docker/docker-compose.yml
- docker/docker-compose.override.yml
- docker/api.Dockerfile
- docker/web.Dockerfile
- docker/.env.example

### Acceptance Criteria
- [ ] `docker-compose up` starts all services
- [ ] API accessible at localhost:5000
- [ ] Web accessible at localhost:3000
- [ ] PostgreSQL accessible at localhost:5432
- [ ] Redis accessible at localhost:6379
- [ ] Hot reload works for API changes
- [ ] Hot reload works for React changes

### Constraints
- PostgreSQL 16
- Redis 7
- Use Alpine base images where possible
```

---

### 1.2 Core Entity Porting

Each entity group is a separate work unit. Entities are grouped by dependency relationships.

#### WU-1.2.1: Base Entity Classes
```markdown
## Task: Create Base Entity Classes

### Context
Foundation classes that all Koinon entities inherit from.

### Input Artifacts
- Reference: ChMS entity patterns

### Output Artifacts
- src/Koinon.Domain/Entities/IEntity.cs
- src/Koinon.Domain/Entities/Entity.cs
- src/Koinon.Domain/Entities/IHasAttributes.cs
- src/Koinon.Domain/Entities/IAuditable.cs
- src/Koinon.Domain/Data/IdKey.cs (Base64 ID encoding)

### Acceptance Criteria
- [ ] IEntity interface with Id, Guid, IdKey properties
- [ ] Entity base class implementing IEntity
- [ ] IAuditable with Created/Modified tracking
- [ ] IdKey generates URL-safe Base64 from int Id
- [ ] IdKey parses back to int Id
- [ ] Unit tests for IdKey encoding/decoding

### Reference
Uses integer IDs internally but exposes IdKey (Base64 encoded)
for URLs to prevent enumeration attacks.

### Constraints
- No EF Core dependencies in Koinon.Domain
- IdKey must be 22 characters (128-bit space)
```

#### WU-1.2.2: Defined Type Entities
```markdown
## Task: Port Defined Type Entities

### Context
DefinedType and DefinedValue are the dictionary/lookup system.
Almost every entity references DefinedValues for status fields.

### Input Artifacts
- Reference: ChMS defined type patterns
- Base entities from WU-1.2.1

### Output Artifacts
- src/Koinon.Domain/Entities/DefinedType.cs
- src/Koinon.Domain/Entities/DefinedValue.cs
- src/Koinon.Infrastructure/Configurations/DefinedTypeConfiguration.cs
- src/Koinon.Infrastructure/Configurations/DefinedValueConfiguration.cs
- tests/Koinon.Domain.Tests/Entities/DefinedTypeTests.cs

### Acceptance Criteria
- [ ] DefinedType entity with Name, Description, Category
- [ ] DefinedValue entity with Value, Description, Order
- [ ] DefinedValue has FK to DefinedType
- [ ] DefinedValue supports IsActive flag
- [ ] EF Core configurations create proper schema
- [ ] Indexes on frequently queried columns

### Constraints
- Table names: defined_type, defined_value (snake_case)
- Include system GUIDs for well-known types
```

#### WU-1.2.3: Campus Entity
```markdown
## Task: Port Campus Entity

### Context
Campus represents physical church locations. Referenced by people,
groups, and check-in configurations.

### Input Artifacts
- Reference: ChMS campus patterns
- Base entities from WU-1.2.1

### Output Artifacts
- src/Koinon.Domain/Entities/Campus.cs
- src/Koinon.Infrastructure/Configurations/CampusConfiguration.cs
- tests/Koinon.Domain.Tests/Entities/CampusTests.cs

### Acceptance Criteria
- [ ] Campus entity with Name, ShortCode, IsActive
- [ ] Location/Address properties
- [ ] Service times collection
- [ ] Leader person reference (nullable)
- [ ] EF Core configuration with proper indexes

### Constraints
- Table name: campus
- ShortCode max 10 characters
```

#### WU-1.2.4: Person Entity (Core)
```markdown
## Task: Port Person Entity - Core Properties

### Context
Person is the central entity. This unit covers core
demographic properties only, not relationships.

### Input Artifacts
- Reference: ChMS person entity patterns
- Base entities from WU-1.2.1
- DefinedValue from WU-1.2.2

### Output Artifacts
- src/Koinon.Domain/Entities/Person.cs
- src/Koinon.Domain/Enums/Gender.cs
- src/Koinon.Domain/Enums/EmailPreference.cs
- src/Koinon.Infrastructure/Configurations/PersonConfiguration.cs
- tests/Koinon.Domain.Tests/Entities/PersonTests.cs

### Acceptance Criteria
- [ ] Person with FirstName, NickName, LastName
- [ ] MiddleName, TitleValueId, SuffixValueId
- [ ] BirthDate (Date only, not DateTime)
- [ ] Gender enum (Unknown, Male, Female)
- [ ] Email with IsEmailActive flag
- [ ] EmailPreference enum
- [ ] RecordStatusValueId, ConnectionStatusValueId FKs
- [ ] Computed FullName property
- [ ] PostgreSQL full-text search vector column
- [ ] Unit tests for FullName computation

### Constraints
- Do NOT include family/group relationships yet
- Do NOT include phone numbers yet
- Table name: person
```

#### WU-1.2.5: Person Phone Numbers
```markdown
## Task: Port Person Phone Numbers

### Context
Phone numbers as a separate entity with type classification.

### Input Artifacts
- Person entity from WU-1.2.4
- DefinedValue from WU-1.2.2

### Output Artifacts
- src/Koinon.Domain/Entities/PhoneNumber.cs
- src/Koinon.Infrastructure/Configurations/PhoneNumberConfiguration.cs
- Update Person.cs with PhoneNumbers navigation
- tests/Koinon.Domain.Tests/Entities/PhoneNumberTests.cs

### Acceptance Criteria
- [ ] PhoneNumber entity with Number, Extension
- [ ] NumberTypeValueId FK to DefinedValue
- [ ] CountryCode property
- [ ] IsMessagingEnabled, IsUnlisted flags
- [ ] PersonId FK
- [ ] Proper indexing on PersonId and Number

### Constraints
- Table name: phone_number
- Store numbers in E.164 format internally
```

#### WU-1.2.6: Family/Group Core
```markdown
## Task: Port Family and Group Entities - Core

### Context
Groups are the universal container. Family is a specific GroupType.
This unit covers Group entity without member relationships.

### Input Artifacts
- Reference: ChMS group patterns
- Base entities

### Output Artifacts
- src/Koinon.Domain/Entities/Group.cs
- src/Koinon.Domain/Entities/GroupType.cs
- src/Koinon.Domain/Entities/GroupTypeRole.cs
- src/Koinon.Infrastructure/Configurations/GroupConfiguration.cs
- src/Koinon.Infrastructure/Configurations/GroupTypeConfiguration.cs
- src/Koinon.Infrastructure/Configurations/GroupTypeRoleConfiguration.cs

### Acceptance Criteria
- [ ] Group with Name, Description, IsActive, IsArchived
- [ ] GroupTypeId FK
- [ ] ParentGroupId self-referencing FK
- [ ] CampusId FK (nullable)
- [ ] GroupType with Name, Description, GroupTerm, GroupMemberTerm
- [ ] GroupType.IsFamilyGroupType flag
- [ ] GroupTypeRole with Name, IsLeader, Order

### Constraints
- Table names: group, group_type, group_type_role
- "group" is reserved word in SQL - ensure proper quoting
```

#### WU-1.2.7: Group Membership
```markdown
## Task: Port Group Member Entity

### Context
Links people to groups with role information.

### Input Artifacts
- Group entities from WU-1.2.6
- Person entity from WU-1.2.4

### Output Artifacts
- src/Koinon.Domain/Entities/GroupMember.cs
- src/Koinon.Domain/Enums/GroupMemberStatus.cs
- src/Koinon.Infrastructure/Configurations/GroupMemberConfiguration.cs
- Update Person.cs with navigation properties
- Update Group.cs with Members navigation

### Acceptance Criteria
- [ ] GroupMember with PersonId, GroupId, GroupRoleId
- [ ] GroupMemberStatus enum (Inactive, Active, Pending)
- [ ] DateTimeAdded tracking
- [ ] IsArchived flag
- [ ] Composite unique index on (GroupId, PersonId, GroupRoleId)
- [ ] Person.PrimaryFamilyId convenience FK

### Constraints
- Table name: group_member
- A person can be in the same group multiple times with different roles
```

#### WU-1.2.8: Location Entities
```markdown
## Task: Port Location Entities

### Context
Locations represent physical places - addresses, rooms, buildings.
Used by campuses, groups, and check-in.

### Input Artifacts
- Reference: ChMS location patterns
- Base entities

### Output Artifacts
- src/Koinon.Domain/Entities/Location.cs
- src/Koinon.Domain/ValueObjects/Address.cs
- src/Koinon.Infrastructure/Configurations/LocationConfiguration.cs
- tests/Koinon.Domain.Tests/ValueObjects/AddressTests.cs

### Acceptance Criteria
- [ ] Location with Name, IsActive
- [ ] ParentLocationId self-reference (rooms in buildings)
- [ ] LocationTypeValueId FK
- [ ] Address value object (Street1, Street2, City, State, PostalCode, Country)
- [ ] GeoPoint for geocoded locations (PostGIS compatible)
- [ ] SoftRoom capacity properties

### Constraints
- Table name: location
- Use PostGIS geography type for GeoPoint
```

---

### 1.3 Data Layer

#### WU-1.3.1: DbContext Setup
```markdown
## Task: Create KoinonDbContext

### Context
Central EF Core DbContext with all entity DbSets.

### Input Artifacts
- All entities from 1.2.x units

### Output Artifacts
- src/Koinon.Infrastructure/Data/KoinonDbContext.cs
- src/Koinon.Infrastructure/DesignTimeDbContextFactory.cs

### Acceptance Criteria
- [ ] DbContext with all entity DbSets
- [ ] OnModelCreating applies all configurations
- [ ] Snake_case naming convention applied globally
- [ ] Design-time factory for migrations
- [ ] Configurable for PostgreSQL and SQL Server

### Constraints
- Use IEntityTypeConfiguration pattern
- No lazy loading
- Split query behavior for collections
```

#### WU-1.3.2: PostgreSQL Provider
```markdown
## Task: Create PostgreSQL Database Provider

### Context
Database provider abstraction for PostgreSQL-specific features.

### Input Artifacts
- KoinonDbContext from WU-1.3.1

### Output Artifacts
- src/Koinon.Infrastructure/Providers/IDatabaseProvider.cs
- src/Koinon.Infrastructure/Providers/PostgreSqlProvider.cs
- src/Koinon.Infrastructure/Extensions/PostgreSqlModelBuilderExtensions.cs

### Acceptance Criteria
- [ ] IDatabaseProvider interface
- [ ] PostgreSQL provider implementation
- [ ] Full-text search configuration helpers
- [ ] PostGIS geography type support
- [ ] Connection string builder

### Constraints
- Npgsql.EntityFrameworkCore.PostgreSQL package
- Use NodaTime for date/time types
```

#### WU-1.3.3: Initial Migration
```markdown
## Task: Create Initial Database Migration

### Context
First EF Core migration creating all Phase 1 tables.

### Input Artifacts
- All entities and configurations from Phase 1

### Output Artifacts
- src/Koinon.Infrastructure/Migrations/[timestamp]_InitialCreate.cs

### Acceptance Criteria
- [ ] Migration creates all Phase 1 tables
- [ ] All indexes created
- [ ] All foreign keys created
- [ ] Migration can apply to empty database
- [ ] Migration can rollback cleanly

### Constraints
- Generate for PostgreSQL
- Include seed data for system DefinedTypes
```

#### WU-1.3.4: Repository Pattern Base
```markdown
## Task: Create Repository Base Classes

### Context
Generic repository pattern for data access abstraction.

### Input Artifacts
- KoinonDbContext from WU-1.3.1
- Entity interfaces from WU-1.2.1

### Output Artifacts
- src/Koinon.Infrastructure/Repositories/IRepository.cs
- src/Koinon.Infrastructure/Repositories/Repository.cs
- src/Koinon.Infrastructure/Repositories/IUnitOfWork.cs
- src/Koinon.Infrastructure/UnitOfWork.cs

### Acceptance Criteria
- [ ] IRepository<T> with CRUD operations
- [ ] Async methods throughout
- [ ] IQueryable exposure for complex queries
- [ ] IUnitOfWork for transaction management
- [ ] Specification pattern support

### Constraints
- All methods async with CancellationToken
- No tracking by default for queries
```

---

## Phase 2: Services Layer

### 2.1 Core Services

#### WU-2.1.1: Person Service
```markdown
## Task: Create Person Service

### Context
Business logic for person management operations.

### Input Artifacts
- Person entity and related entities
- Repository interfaces

### Output Artifacts
- src/Koinon.Application/Interfaces/IPersonService.cs
- src/Koinon.Application/PersonService.cs
- src/Koinon.Application/DTOs/PersonDto.cs
- src/Koinon.Application/DTOs/PersonSearchParameters.cs
- tests/Koinon.Application.Tests/PersonServiceTests.cs

### Acceptance Criteria
- [ ] GetByIdAsync, GetByIdKeyAsync
- [ ] SearchAsync with pagination
- [ ] CreateAsync with validation
- [ ] UpdateAsync with validation
- [ ] Full-text search support
- [ ] Family member retrieval
- [ ] 80%+ test coverage

### Constraints
- Return DTOs, not entities
- Use FluentValidation for validation
- All methods async
```

#### WU-2.1.2: Family Service
```markdown
## Task: Create Family Service

### Context
Business logic for family (household) management.

### Input Artifacts
- Group entities
- Person service from WU-2.1.1

### Output Artifacts
- src/Koinon.Application/Interfaces/IFamilyService.cs
- src/Koinon.Application/FamilyService.cs
- src/Koinon.Application/DTOs/FamilyDto.cs
- src/Koinon.Application/DTOs/FamilyMemberDto.cs
- tests/Koinon.Application.Tests/FamilyServiceTests.cs

### Acceptance Criteria
- [ ] GetByIdAsync with members
- [ ] CreateFamilyAsync
- [ ] AddFamilyMemberAsync
- [ ] RemoveFamilyMemberAsync
- [ ] SetPrimaryFamilyAsync for person
- [ ] Address management for family

### Constraints
- Family is GroupType where IsFamilyGroupType = true
```

#### WU-2.1.3: Group Service
```markdown
## Task: Create Group Service

### Context
Business logic for general group management (non-family).

### Input Artifacts
- Group entities
- Repository interfaces

### Output Artifacts
- src/Koinon.Application/Interfaces/IGroupService.cs
- src/Koinon.Application/GroupService.cs
- src/Koinon.Application/DTOs/GroupDto.cs
- src/Koinon.Application/DTOs/GroupMemberDto.cs
- tests/Koinon.Application.Tests/GroupServiceTests.cs

### Acceptance Criteria
- [ ] CRUD operations
- [ ] Member management
- [ ] Hierarchy traversal (parent/children)
- [ ] Group type filtering
- [ ] Campus filtering

### Constraints
- Exclude family-specific logic (use FamilyService)
```

---

### 2.2 Check-in Services

#### WU-2.2.1: Check-in Configuration Service
```markdown
## Task: Create Check-in Configuration Service

### Context
Manages check-in area configurations, schedules, and group/location mappings.

### Input Artifacts
- Group, Location entities
- Reference: Check-in area/group concepts

### Output Artifacts
- src/Koinon.Domain/Entities/CheckinArea.cs
- src/Koinon.Domain/Entities/CheckinConfiguration.cs
- src/Koinon.Application/Interfaces/ICheckinConfigurationService.cs
- src/Koinon.Application/CheckinConfigurationService.cs
- src/Koinon.Application/DTOs/CheckinConfigurationDto.cs
- Database migration for new entities

### Acceptance Criteria
- [ ] CheckinArea entity (special GroupType)
- [ ] Configuration retrieval by kiosk/campus
- [ ] Schedule-aware availability
- [ ] Location capacity tracking
- [ ] Active time window support

### Constraints
- Configuration should be cacheable
- Support multiple campuses
```

#### WU-2.2.2: Check-in Search Service
```markdown
## Task: Create Check-in Family Search Service

### Context
Fast family lookup for check-in kiosk search box.

### Input Artifacts
- Person, Family services
- Phone number entities

### Output Artifacts
- src/Koinon.Application/Interfaces/ICheckinSearchService.cs
- src/Koinon.Application/CheckinSearchService.cs
- src/Koinon.Application/DTOs/CheckinFamilySearchResultDto.cs
- tests/Koinon.Application.Tests/CheckinSearchServiceTests.cs

### Acceptance Criteria
- [ ] Search by phone number (last 4, full)
- [ ] Search by name (partial match)
- [ ] Search by check-in code
- [ ] Returns family with members and photos
- [ ] Response time <50ms for indexed searches
- [ ] Results limited to active families

### Constraints
- Optimize for speed over completeness
- Use database full-text search
```

#### WU-2.2.3: Check-in Attendance Service
```markdown
## Task: Create Check-in Attendance Service

### Context
Records attendance and generates security codes.

### Input Artifacts
- Check-in configuration
- Person, Group, Location entities

### Output Artifacts
- src/Koinon.Domain/Entities/Attendance.cs
- src/Koinon.Domain/Entities/AttendanceCode.cs
- src/Koinon.Application/Interfaces/IAttendanceService.cs
- src/Koinon.Application/AttendanceService.cs
- src/Koinon.Application/DTOs/AttendanceDto.cs
- Database migration for attendance tables

### Acceptance Criteria
- [ ] Attendance entity with Person, Group, Location, Schedule
- [ ] Security code generation (configurable format)
- [ ] Duplicate attendance prevention
- [ ] Check-out support
- [ ] Attendance history queries

### Constraints
- Security codes must be unique per day
- Support alphanumeric and numeric-only codes
```

#### WU-2.2.4: Label Generation Service
```markdown
## Task: Create Label Generation Service

### Context
Generates check-in labels (ZPL format for thermal printers).

### Input Artifacts
- Attendance data
- Person/Family data

### Output Artifacts
- src/Koinon.Application/Interfaces/ILabelService.cs
- src/Koinon.Application/LabelService.cs
- src/Koinon.Application/Labels/LabelTemplate.cs
- src/Koinon.Application/Labels/ZplGenerator.cs
- src/Koinon.Application/DTOs/LabelDto.cs

### Acceptance Criteria
- [ ] ZPL label generation
- [ ] Configurable label templates
- [ ] Child label with security code
- [ ] Parent label with security code
- [ ] Name tag label
- [ ] Merge field substitution

### Constraints
- ZPL 2 format compatibility
- Support common Zebra printer models
```

---

## Phase 3: API Layer

### 3.1 API Foundation

#### WU-3.1.1: API Project Configuration
```markdown
## Task: Configure API Project

### Context
ASP.NET Core Web API configuration with authentication,
CORS, and OpenAPI documentation.

### Input Artifacts
- Koinon.Api project shell
- Service interfaces

### Output Artifacts
- src/Koinon.Api/Program.cs (complete configuration)
- src/Koinon.Api/appsettings.json
- src/Koinon.Api/appsettings.Development.json

### Acceptance Criteria
- [ ] Dependency injection configured
- [ ] PostgreSQL connection configured
- [ ] JWT authentication configured
- [ ] CORS policy for React dev server
- [ ] OpenAPI/Swagger documentation
- [ ] Health check endpoints
- [ ] Global exception handling
- [ ] Request logging

### Constraints
- Minimal API style where appropriate
- Use System.Text.Json
```

#### WU-3.1.2: Authentication Controller
```markdown
## Task: Create Authentication Controller

### Context
JWT token issuance and refresh for API authentication.

### Input Artifacts
- Person service
- JWT configuration

### Output Artifacts
- src/Koinon.Api/Controllers/AuthController.cs
- src/Koinon.Application/Interfaces/IAuthService.cs
- src/Koinon.Application/AuthService.cs
- src/Koinon.Application/DTOs/LoginRequest.cs
- src/Koinon.Application/DTOs/TokenResponse.cs

### Acceptance Criteria
- [ ] POST /api/v1/auth/login
- [ ] POST /api/v1/auth/refresh
- [ ] POST /api/v1/auth/logout
- [ ] JWT access token (15 min expiry)
- [ ] Refresh token (7 day expiry)
- [ ] Password hashing with Argon2

### Constraints
- No cookie-based auth (API only)
- Refresh tokens stored in database
```

### 3.2 Resource Controllers

#### WU-3.2.1: People Controller
```markdown
## Task: Create People API Controller

### Context
REST API endpoints for person management.

### Input Artifacts
- PersonService from WU-2.1.1

### Output Artifacts
- src/Koinon.Api/Controllers/PeopleController.cs
- src/Koinon.Api/DTOs/Requests/CreatePersonRequest.cs
- src/Koinon.Api/DTOs/Requests/UpdatePersonRequest.cs
- tests/Koinon.Api.Tests/Controllers/PeopleControllerTests.cs

### Acceptance Criteria
- [ ] GET /api/v1/people (search/list)
- [ ] GET /api/v1/people/{idKey}
- [ ] POST /api/v1/people
- [ ] PUT /api/v1/people/{idKey}
- [ ] DELETE /api/v1/people/{idKey}
- [ ] GET /api/v1/people/{idKey}/family
- [ ] Proper HTTP status codes
- [ ] Request validation
- [ ] Integration tests

### Constraints
- Use IdKey in URLs, not integer IDs
- Pagination via query parameters
```

#### WU-3.2.2: Families Controller
```markdown
## Task: Create Families API Controller

### Context
REST API endpoints for family management.

### Input Artifacts
- FamilyService from WU-2.1.2

### Output Artifacts
- src/Koinon.Api/Controllers/FamiliesController.cs
- src/Koinon.Api/DTOs/Requests/CreateFamilyRequest.cs
- tests/Koinon.Api.Tests/Controllers/FamiliesControllerTests.cs

### Acceptance Criteria
- [ ] GET /api/v1/families
- [ ] GET /api/v1/families/{idKey}
- [ ] POST /api/v1/families
- [ ] PUT /api/v1/families/{idKey}
- [ ] GET /api/v1/families/{idKey}/members
- [ ] POST /api/v1/families/{idKey}/members
- [ ] DELETE /api/v1/families/{idKey}/members/{personIdKey}

### Constraints
- Family address updates affect all members
```

#### WU-3.2.3: Check-in Controller
```markdown
## Task: Create Check-in API Controller

### Context
REST API endpoints for check-in kiosk operations.

### Input Artifacts
- Check-in services from Phase 2.2

### Output Artifacts
- src/Koinon.Api/Controllers/CheckinController.cs
- src/Koinon.Api/DTOs/Requests/CheckinSearchRequest.cs
- src/Koinon.Api/DTOs/Requests/RecordAttendanceRequest.cs
- src/Koinon.Api/DTOs/Responses/CheckinOpportunitiesResponse.cs
- tests/Koinon.Api.Tests/Controllers/CheckinControllerTests.cs

### Acceptance Criteria
- [ ] GET /api/v1/checkin/configuration
- [ ] POST /api/v1/checkin/search
- [ ] GET /api/v1/checkin/opportunities/{familyIdKey}
- [ ] POST /api/v1/checkin/attendance
- [ ] POST /api/v1/checkin/checkout
- [ ] GET /api/v1/checkin/labels/{attendanceIdKey}
- [ ] Response time <100ms for all endpoints

### Constraints
- Kiosk authentication via device token
- Support batch attendance recording
```

---

## Phase 4: React Frontend

### 4.1 Foundation

#### WU-4.1.1: API Client Setup
```markdown
## Task: Create TypeScript API Client

### Context
Type-safe API client using fetch with TanStack Query integration.

### Input Artifacts
- OpenAPI spec from API (or manual type definitions)

### Output Artifacts
- src/web/src/services/api/client.ts
- src/web/src/services/api/types.ts
- src/web/src/services/api/people.ts
- src/web/src/services/api/families.ts
- src/web/src/services/api/checkin.ts

### Acceptance Criteria
- [ ] Base client with auth header injection
- [ ] Automatic token refresh on 401
- [ ] Type-safe request/response types
- [ ] Error handling utilities
- [ ] Request cancellation support

### Constraints
- Use native fetch, not axios
- Types must match API DTOs exactly
```

#### WU-4.1.2: Authentication State
```markdown
## Task: Create Authentication State Management

### Context
React context and hooks for authentication state.

### Input Artifacts
- API client from WU-4.1.1

### Output Artifacts
- src/web/src/contexts/AuthContext.tsx
- src/web/src/hooks/useAuth.ts
- src/web/src/components/auth/LoginForm.tsx
- src/web/src/components/auth/ProtectedRoute.tsx

### Acceptance Criteria
- [ ] AuthContext with user state
- [ ] useAuth hook for components
- [ ] Login form component
- [ ] Token storage in memory (not localStorage)
- [ ] Refresh token in httpOnly cookie
- [ ] Protected route wrapper
- [ ] Logout functionality

### Constraints
- No sensitive data in localStorage
- Handle token expiry gracefully
```

#### WU-4.1.3: Layout Components
```markdown
## Task: Create Layout Components

### Context
Application shell with navigation, header, and content areas.

### Input Artifacts
- TailwindCSS configuration

### Output Artifacts
- src/web/src/components/layout/AppShell.tsx
- src/web/src/components/layout/Sidebar.tsx
- src/web/src/components/layout/Header.tsx
- src/web/src/components/layout/PageHeader.tsx
- src/web/src/components/layout/Panel.tsx

### Acceptance Criteria
- [ ] Responsive sidebar navigation
- [ ] Collapsible on mobile
- [ ] Header with user menu
- [ ] Breadcrumb support
- [ ] Panel component for content blocks

### Constraints
- Mobile-first responsive design
- Clean, modern visual style
```

### 4.2 Control Components

#### WU-4.2.1: Form Controls
```markdown
## Task: Create Form Control Components

### Context
Reusable form input components with validation.

### Input Artifacts
- TailwindCSS configuration

### Output Artifacts
- src/web/src/components/controls/TextInput.tsx
- src/web/src/components/controls/Select.tsx
- src/web/src/components/controls/DatePicker.tsx
- src/web/src/components/controls/Checkbox.tsx
- src/web/src/components/controls/RadioGroup.tsx
- src/web/src/components/controls/FormField.tsx

### Acceptance Criteria
- [ ] Consistent styling across all controls
- [ ] Label and help text support
- [ ] Error state display
- [ ] Disabled state
- [ ] React Hook Form compatible
- [ ] Accessible (ARIA labels)

### Constraints
- Use react-hook-form for form state
- Use zod for validation schemas
```

#### WU-4.2.2: Data Display Components
```markdown
## Task: Create Data Display Components

### Context
Components for displaying data in lists and tables.

### Input Artifacts
- TailwindCSS configuration

### Output Artifacts
- src/web/src/components/controls/DataTable.tsx
- src/web/src/components/controls/Pagination.tsx
- src/web/src/components/controls/Badge.tsx
- src/web/src/components/controls/Avatar.tsx
- src/web/src/components/controls/EmptyState.tsx
- src/web/src/components/controls/LoadingSpinner.tsx

### Acceptance Criteria
- [ ] DataTable with sorting, pagination
- [ ] Virtual scrolling for large lists
- [ ] Badge for status display
- [ ] Avatar with fallback initials
- [ ] Empty state for no data
- [ ] Loading states

### Constraints
- Use @tanstack/react-table for DataTable
- Virtual scrolling with @tanstack/react-virtual
```

### 4.3 Feature Modules

#### WU-4.3.1: Person Directory Feature
```markdown
## Task: Create Person Directory Feature

### Context
Person search and list view with detail navigation.

### Input Artifacts
- API client, layout components, data display components

### Output Artifacts
- src/web/src/features/people/PeopleListPage.tsx
- src/web/src/features/people/PersonSearchBar.tsx
- src/web/src/features/people/PersonListItem.tsx
- src/web/src/features/people/hooks/usePeopleSearch.ts

### Acceptance Criteria
- [ ] Search input with debouncing
- [ ] Results list with pagination
- [ ] Person card with photo, name, email
- [ ] Click to navigate to detail
- [ ] Loading and empty states
- [ ] URL query param sync for search

### Constraints
- Debounce search by 300ms
- Cache results with TanStack Query
```

#### WU-4.3.2: Person Detail Feature
```markdown
## Task: Create Person Detail Feature

### Context
Person detail view with bio, contact info, family, groups.

### Input Artifacts
- API client, layout components

### Output Artifacts
- src/web/src/features/people/PersonDetailPage.tsx
- src/web/src/features/people/blocks/PersonBioBlock.tsx
- src/web/src/features/people/blocks/ContactInfoBlock.tsx
- src/web/src/features/people/blocks/FamilyMembersBlock.tsx
- src/web/src/features/people/blocks/GroupsBlock.tsx

### Acceptance Criteria
- [ ] Bio block with photo, name, demographics
- [ ] Contact info with email, phone, address
- [ ] Family members list
- [ ] Group memberships list
- [ ] Edit button (navigation to edit page)
- [ ] Responsive layout

### Constraints
- Block-based layout
- Each block independently loadable
```

#### WU-4.3.3: Check-in Kiosk Feature
```markdown
## Task: Create Check-in Kiosk Feature

### Context
Touch-optimized check-in interface for kiosk deployment.

### Input Artifacts
- Check-in API client
- Service worker setup

### Output Artifacts
- src/web/src/features/checkin/CheckinKioskPage.tsx
- src/web/src/features/checkin/FamilySearch.tsx
- src/web/src/features/checkin/FamilySelect.tsx
- src/web/src/features/checkin/PersonCheckin.tsx
- src/web/src/features/checkin/CheckinSuccess.tsx
- src/web/src/features/checkin/hooks/useCheckin.ts
- src/web/src/features/checkin/hooks/useOfflineCheckin.ts

### Acceptance Criteria
- [ ] Full-screen kiosk layout
- [ ] Large touch targets (min 48px)
- [ ] Phone/name search
- [ ] Family selection
- [ ] Person/group/location selection
- [ ] Optimistic UI updates
- [ ] Offline queue with sync
- [ ] Success screen with security code

### Constraints
- Must work on tablet (768px minimum)
- Touch response <10ms
- Offline-capable via service worker
```

#### WU-4.3.4: Check-in PWA Configuration
```markdown
## Task: Configure Check-in as PWA

### Context
Progressive Web App configuration for installable kiosk mode.

### Input Artifacts
- Vite configuration
- Check-in feature

### Output Artifacts
- src/web/vite.config.ts (PWA plugin)
- src/web/public/manifest.json
- src/web/src/sw.ts
- src/web/public/icons/ (app icons)

### Acceptance Criteria
- [ ] Web app manifest configured
- [ ] Service worker with Workbox
- [ ] Offline caching strategy
- [ ] Background sync for queued check-ins
- [ ] Install prompt handling
- [ ] Update prompt when new version available

### Constraints
- Use vite-plugin-pwa
- Cache check-in config aggressively
- Queue attendance POSTs when offline
```

---

## Phase 5: Integration & Polish

### 5.1 Data Migration

#### WU-5.1.1: Data Import Tool
```markdown
## Task: Create Database Import Tool

### Context
CLI tool to migrate data from existing church database.

### Input Artifacts
- Entity definitions
- Source database schema reference

### Output Artifacts
- tools/Koinon.Import/Koinon.Import.csproj
- tools/Koinon.Import/Program.cs
- tools/Koinon.Import/Importers/PersonImporter.cs
- tools/Koinon.Import/Importers/FamilyImporter.cs
- tools/Koinon.Import/Importers/GroupImporter.cs

### Acceptance Criteria
- [ ] Connect to source SQL Server
- [ ] Connect to target PostgreSQL
- [ ] Import people with ID mapping
- [ ] Import families preserving relationships
- [ ] Import groups and memberships
- [ ] Progress reporting
- [ ] Error logging with skip option
- [ ] Dry-run mode

### Constraints
- Must handle millions of records
- Batch inserts for performance
- Preserve GUIDs for reference
```

### 5.2 Testing

#### WU-5.2.1: Integration Test Suite
```markdown
## Task: Create API Integration Test Suite

### Context
End-to-end API tests using test containers.

### Input Artifacts
- All API controllers

### Output Artifacts
- tests/Koinon.Api.IntegrationTests/
- Test fixtures and factories
- Docker test configuration

### Acceptance Criteria
- [ ] TestContainers for PostgreSQL
- [ ] Test data factories
- [ ] Authentication test helpers
- [ ] Tests for all critical paths
- [ ] CI/CD compatible

### Constraints
- Use xUnit
- Tests must be parallelizable
- Clean database between tests
```

#### WU-5.2.2: React Component Tests
```markdown
## Task: Create React Component Test Suite

### Context
Component and integration tests for React frontend.

### Input Artifacts
- All React components

### Output Artifacts
- Component test files (*.test.tsx)
- Test utilities and mocks
- MSW handlers for API mocking

### Acceptance Criteria
- [ ] Unit tests for utility functions
- [ ] Component tests with React Testing Library
- [ ] MSW for API mocking
- [ ] Check-in flow integration test
- [ ] Accessibility tests

### Constraints
- Use Vitest
- Use MSW for API mocking
- Test user interactions, not implementation
```

---

## Work Unit Execution Order

### Critical Path (MVP: Check-in System)

```
WU-1.1.1 → WU-1.1.2 → WU-1.1.3  (Scaffolding - parallel)
    ↓
WU-1.2.1 → WU-1.2.2 → WU-1.2.3  (Base entities)
    ↓
WU-1.2.4 → WU-1.2.5              (Person)
    ↓
WU-1.2.6 → WU-1.2.7              (Groups/Family)
    ↓
WU-1.2.8                          (Locations)
    ↓
WU-1.3.1 → WU-1.3.2 → WU-1.3.3  (Data layer)
    ↓
WU-2.1.1 → WU-2.1.2              (Person/Family services)
    ↓
WU-2.2.1 → WU-2.2.2 → WU-2.2.3 → WU-2.2.4  (Check-in services)
    ↓
WU-3.1.1 → WU-3.1.2              (API foundation)
    ↓
WU-3.2.3                          (Check-in API)
    ↓
WU-4.1.1 → WU-4.1.2 → WU-4.1.3  (React foundation)
    ↓
WU-4.2.1 → WU-4.2.2              (Controls)
    ↓
WU-4.3.3 → WU-4.3.4              (Check-in kiosk)
```

### Estimated Work Units: 45-50 total

### Per-Unit Time Estimate

| Phase | Units | Est. Hours/Unit | Total Hours |
|-------|-------|-----------------|-------------|
| 1. Foundation | 15 | 4-8 | 60-120 |
| 2. Services | 8 | 6-10 | 48-80 |
| 3. API | 6 | 4-6 | 24-36 |
| 4. React | 12 | 6-10 | 72-120 |
| 5. Integration | 4 | 8-12 | 32-48 |
| **Total** | **45** | | **236-404** |

With Claude Code agents, expect 2-4x productivity gain on well-specified units, potentially completing the MVP in 3-6 months of focused effort.

---

## Agent Execution Notes

### Context Preparation

Before starting each work unit, prepare a context package:

1. **Task specification** (from this document)
2. **Reference files** (existing code to build upon)
3. **Schema/type definitions** (for consistency)
4. **Test examples** (for pattern matching)

### Success Verification

After each unit:

1. Run unit tests: `dotnet test` or `npm test`
2. Build verification: `dotnet build` or `npm run build`
3. Linting: `dotnet format` or `npm run lint`
4. Manual smoke test if applicable

### Dependency Management

Track completed units in a simple manifest:

```json
{
  "completed": ["WU-1.1.1", "WU-1.1.2"],
  "in_progress": "WU-1.2.1",
  "blocked": []
}
```

### Error Recovery

If an agent produces incorrect output:

1. Identify specific failure
2. Add clarifying constraints to task spec
3. Provide error output as context
4. Re-run with narrower scope if needed
