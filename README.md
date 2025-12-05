# Koinon RMS

A modern, cross-platform Church Management System built on .NET 8 and React.

## Overview

Koinon RMS is a ground-up implementation of a Church Management System targeting Linux containers with a modern technology stack. It is designed for cloud-native deployment with a focus on performance and usability.

### Why Koinon RMS?

Traditional church management systems are often constrained by outdated technology choices:

| Constraint | Legacy Systems | Koinon RMS |
|------------|----------------|------------|
| Platform | Windows Server + IIS | Linux containers |
| Framework | .NET Framework 4.x | .NET 8 |
| Frontend | ASP.NET WebForms | React 18 + TypeScript |
| Database | SQL Server (licensed) | PostgreSQL (free) |
| Deployment | Manual/complex | Docker/Kubernetes |

### Current Focus: Check-in MVP

The first milestone is a fully-functional check-in system optimized for:

- **Fast**: <10ms touch response, <200ms complete check-in
- **Offline-capable**: Works during network outages
- **Cross-platform**: Runs on any tablet (iPad, Android, Windows)
- **Simple deployment**: Single `docker-compose up`

---

## Tech Stack

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core 8
- PostgreSQL 16+
- Redis (caching/sessions)

### Frontend
- React 18 with TypeScript
- Vite build tooling
- TanStack Query (server state)
- TailwindCSS
- PWA with offline support

### Infrastructure
- Docker multi-stage builds
- Docker Compose for development
- Kubernetes-ready

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) (recommend using nvm)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Development Setup

```bash
# Clone the repository
git clone https://github.com/your-org/koinon-rms.git
cd koinon-rms

# Start infrastructure (PostgreSQL + Redis)
docker-compose up -d

# Run database migrations
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Start the API (in one terminal)
cd src/Koinon.Api
dotnet watch run

# Start the frontend (in another terminal)
cd src/web
npm install
npm run dev
```

The API will be available at `http://localhost:5000` and the frontend at `http://localhost:5173`.

### Full Stack in Docker

To run everything in containers:

```bash
docker-compose -f docker-compose.full.yml up --build
```

---

## Project Structure

```
koinon-rms/
├── src/
│   ├── Koinon.Domain/         # Entities, enums, interfaces
│   ├── Koinon.Application/    # Use cases, DTOs, validators
│   ├── Koinon.Infrastructure/ # EF Core, Redis, external services
│   ├── Koinon.Api/            # ASP.NET Core Web API
│   └── web/                   # React frontend
├── tests/                     # Unit and integration tests
├── docs/                      # Documentation
│   ├── entity-mappings.md     # Entity mappings
│   ├── api-contracts.md       # REST API specifications
│   └── work-breakdown.md      # Development work units
└── tools/                     # Migration and utility scripts
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [CLAUDE.md](./CLAUDE.md) | Project context for AI-assisted development |
| [Entity Mappings](./docs/entity-mappings.md) | Field-by-field entity mapping |
| [API Contracts](./docs/api-contracts.md) | REST API TypeScript interfaces |
| [Work Breakdown](./docs/work-breakdown.md) | Development work units and phases |

---

## Development

### Commands

```bash
# Run tests
dotnet test

# Create a migration
dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api

# Apply migrations
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Format code
dotnet format

# Frontend type checking
cd src/web && npm run typecheck

# Frontend linting
cd src/web && npm run lint
```

### Architecture

This project follows Clean Architecture principles:

```
┌──────────────────────────────────────────┐
│                  API                      │  ← HTTP Controllers, Middleware
├──────────────────────────────────────────┤
│              Application                  │  ← Use Cases, DTOs, Validation
├──────────────────────────────────────────┤
│               Domain                      │  ← Entities, Business Rules
├──────────────────────────────────────────┤
│            Infrastructure                 │  ← Database, Cache, External APIs
└──────────────────────────────────────────┘
```

Dependencies flow inward—Domain has no dependencies, Infrastructure implements interfaces defined in Domain/Application.

---

## API Overview

All API endpoints use the `/api/v1/` prefix and follow RESTful conventions.

### Core Resources

| Resource | Endpoints | Description |
|----------|-----------|-------------|
| `/api/v1/auth` | POST login, refresh, logout | Authentication |
| `/api/v1/people` | CRUD + search | Person management |
| `/api/v1/families` | CRUD + members | Family management |
| `/api/v1/groups` | CRUD + members | Group management |
| `/api/v1/checkin` | Search, opportunities, record | Check-in operations |

### Response Format

Success:
```json
{
  "data": { ... },
  "meta": {
    "page": 1,
    "pageSize": 25,
    "totalCount": 100
  }
}
```

Error:
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "email": ["Invalid email format"]
    }
  }
}
```

---

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__Koinon` | (required) | PostgreSQL connection string |
| `ConnectionStrings__Redis` | `localhost:6379` | Redis connection string |
| `Jwt__Secret` | (required) | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | `koinon` | JWT issuer claim |
| `Jwt__Audience` | `koinon` | JWT audience claim |
| `Jwt__AccessTokenExpirationMinutes` | `15` | Access token lifetime |
| `Jwt__RefreshTokenExpirationDays` | `7` | Refresh token lifetime |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Koinon": "Host=localhost;Database=koinon;Username=koinon;Password=koinon",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters-long",
    "Issuer": "koinon",
    "Audience": "koinon"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## Roadmap

### Phase 1: Foundation (Current)
- [x] Project architecture
- [ ] Core entities (Person, Group, Family)
- [ ] Database context and migrations
- [ ] Basic API endpoints

### Phase 2: Services
- [ ] Person service with search
- [ ] Family management
- [ ] Group management
- [ ] Authentication/Authorization

### Phase 3: Check-in MVP
- [ ] Check-in configuration
- [ ] Family search
- [ ] Attendance recording
- [ ] Label printing
- [ ] Offline support

### Phase 4: Admin Interface
- [ ] Person/Family CRUD UI
- [ ] Group management UI
- [ ] Check-in configuration UI

### Future
- [ ] Event registration
- [ ] Giving/Contributions
- [ ] Communication tools
- [ ] Reporting
- [ ] Data migration tools

---

## Contributing

This project is currently in early development. Contribution guidelines will be established as the project matures.

---

## License

TBD - License to be determined.
