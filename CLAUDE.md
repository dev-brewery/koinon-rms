# CLAUDE.md - Koinon RMS Project Intelligence

You are an expert software architect working on **Koinon RMS**, a greenfield Church Management System using .NET 8 and React with clean architecture.

## Session Start (Required)

```bash
mcp__memory__read_graph                    # 1. Load context
.claude/scripts/confirm-memory-check.sh   # 2. Confirm
.claude/scripts/verify-session.sh         # 3. Verify (unlocks code operations)
```

Sessions expire after 4 hours. Hooks enforce verification before code changes.

## Project Identity

- **Name:** Koinon RMS
- **Tech:** .NET 8, EF Core, PostgreSQL, Redis, React 18, TypeScript, Vite
- **MVP Focus:** Performance-critical check-in kiosks (<200ms online, <50ms offline)

## Repository Structure

```
koinon-rms/
├── src/
│   ├── Koinon.Domain/         # Entities, interfaces (no dependencies)
│   ├── Koinon.Application/    # Use cases, DTOs, validators
│   ├── Koinon.Infrastructure/ # EF Core, Redis
│   ├── Koinon.Api/            # Web API
│   └── web/                   # React frontend
├── tests/                     # Test projects
├── docs/reference/            # entity-mappings, api-contracts, work-breakdown
└── .claude/                   # Hooks, agents, scripts
```

## Clean Architecture

```
API → Application → Domain
         ↓
    Infrastructure
```

| Layer | Responsibility |
|-------|----------------|
| Domain | Entities, interfaces (no dependencies) |
| Application | Use cases, DTOs, validation (depends on Domain) |
| Infrastructure | EF Core, Redis, external APIs |
| Api | Controllers, middleware, auth |

## Entity Design

```csharp
public abstract class Entity : IEntity, IAuditable
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string IdKey => IdKeyHelper.Encode(Id);  // Use in URLs
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
}
```

## Database Conventions

| Aspect | Convention | Example |
|--------|------------|---------|
| Tables | snake_case | `group_member` |
| Columns | snake_case | `first_name` |
| C# properties | PascalCase | `FirstName` |
| Primary keys | `id` | int identity |
| Foreign keys | `{entity}_id` | `person_id` |

## API Design

- Base path: `/api/v1/`
- Use `IdKey` in URLs (never integer IDs)
- See `docs/reference/api-contracts.md`

## Development Commands

```bash
# Infrastructure
docker-compose up -d                    # Start postgres + redis
docker-compose down -v                  # Stop and delete data

# Backend
dotnet build
dotnet test
dotnet watch run --project src/Koinon.Api

# Migrations
dotnet ef migrations add <Name> -p src/Koinon.Infrastructure -s src/Koinon.Api
dotnet ef database update -p src/Koinon.Infrastructure -s src/Koinon.Api

# Frontend (from src/web)
npm install && npm run dev
```

## Coding Standards

**C#:** File-scoped namespaces, primary constructors, records for DTOs, PascalCase files
**TypeScript:** Strict mode, no `any`, functional components, TanStack Query

## Anti-Patterns

- Never expose integer IDs (use IdKey)
- No business logic in controllers
- No synchronous DB calls
- No N+1 queries
- No `any` in TypeScript
- No class components

## PM Role

- **Delegate code changes to agents** (enforced by hooks)
- Run code-critic after implementations
- Complete session verification before starting

## MCP Servers

Use MCPs for 70-99% token savings:
- `postgres` - Query DB instead of reading files
- `memory` - Session context (must read at start)
- `github` - Issues, PRs (owner: dev-brewery)
- `koinon-dev` - Naming/route validation

## Quick Reference

```
PostgreSQL: localhost:5432 (koinon/koinon)
Redis: localhost:6379
API: localhost:5000
Web: localhost:5173
```

## Key Documentation

| Document | When to Use |
|----------|-------------|
| `docs/reference/entity-mappings.md` | Implementing entities |
| `docs/reference/api-contracts.md` | Implementing API endpoints |
| `docs/reference/work-breakdown.md` | Work unit specs |
| `.claude/GUARDRAILS.md` | Safety rules |
| `.claude/HOOKS.md` | Hook reference |
