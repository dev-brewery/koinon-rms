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

Works the same on Windows and Linux.

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (version pinned in `global.json`)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + Compose on Linux)

### See it running (full demo stack in Docker)

```bash
git clone https://github.com/dev-brewery/koinon-rms.git
cd koinon-rms
npm ci                                             # root tooling (husky, validation)
cp .env.example .env
docker compose -f docker-compose.full.yml up -d --build
```

- Web: http://localhost:3000 · API: http://localhost:5000 (health: `/health`, Swagger: `/swagger`)
- Migrations apply automatically on API start (containerized dev only).
- **First run:** seed demo data, then log in as `john.smith@example.com` / `admin123`:

```bash
docker run --rm -v "$(pwd):/repo" -w /repo --network koinon_network \
  mcr.microsoft.com/dotnet/sdk:8.0-alpine \
  dotnet run --project tools/Koinon.TestDataSeeder -- seed \
  --connection "Host=postgres;Port=5432;Database=koinon;Username=koinon;Password=koinon"
```

The first `/admin` visit launches the setup wizard (no campus is seeded) — that's
intended onboarding, not a bug. Launch/login troubleshooting lives in
`.claude/skills/koinon-demo-stack/SKILL.md`.

### One-command end-to-end test

Starts the stack if needed, seeds if needed, runs the Playwright smoke suite,
opens the report:

```powershell
tools/qa/run-e2e-demo.ps1        # Windows
```
```bash
tools/qa/run-e2e-demo.sh         # Linux / macOS
```

### Local development loop (code changes)

```bash
docker compose up -d                                              # postgres + redis only
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api
npm run dev:api                                                   # terminal 1 — API on :5000
npm --prefix src/web ci && npm run dev:web                        # terminal 2 — web on :5173
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
├── tests/                     # Unit, integration, and architecture tests
├── docs/
│   ├── reference/             # Canonical: conventions, contracts, playbooks
│   ├── adr/                   # Architecture decision records
│   └── archive/               # Historical documents (do not trust)
└── tools/                     # MCP servers, QA runners, seeders, print-bridge
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [CLAUDE.md](./CLAUDE.md) | Project context for AI-assisted development |
| [Conventions](./docs/reference/conventions.md) | Canonical architecture conventions |
| [QA Playbook](./docs/reference/qa-playbook.md) | Testing handbook (tiers, E2E, printer mocking) |
| [Entity Mappings](./docs/reference/entity-mappings.md) | Field-by-field entity mapping |
| [API Contracts](./docs/reference/api-contracts.md) | REST API TypeScript interfaces |
| [Work Breakdown](./docs/reference/work-breakdown.md) | Development work units and phases |
| [ADRs](./docs/adr/) | Architecture decision records |
| [docs/README.md](./docs/README.md) | Map of all other documentation |

---

## Development

### Commands

All from the repo root, on either OS:

```bash
npm run build          # dotnet build
npm test               # dotnet test + frontend vitest
npm run typecheck      # frontend TypeScript check
npm run lint           # frontend ESLint
npm run validate       # full local gate (what pre-push runs)
npm run graph:validate # architecture graph drift check
dotnet format          # backend formatting (CI enforces --verify-no-changes)

# Migrations
dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Browser tests (full suite; smoke tier runs in CI)
tools/qa/run-e2e-demo.ps1 --all      # Windows
tools/qa/run-e2e-demo.sh --all       # Linux / macOS
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
| `ConnectionStrings__DefaultConnection` | (required) | PostgreSQL connection string |
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
    "DefaultConnection": "Host=localhost;Database=koinon;Username=koinon;Password=koinon",
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

## Status

The foundation phases are complete: ~60 domain entities, 40 API controllers,
50+ migrations, JWT auth, the check-in kiosk with label printing (via the
print-bridge service), and the admin interface. Current work is demo
hardening and feature completion — see
[work-breakdown.md](./docs/reference/work-breakdown.md) and the GitHub
issue tracker for what's in flight.

---

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md). Short version: read
`docs/reference/conventions.md` first, follow the feature-slice order, every
user-facing change needs an E2E test, and the architecture tests + graph
baseline will hold you to the layer rules mechanically.

---

## License

TBD - License to be determined.
