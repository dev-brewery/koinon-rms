---
name: koinon-demo-stack
description: >
  Launch, seed, verify, or debug the full containerized Koinon RMS demo stack
  (postgres+redis+api+web in Docker). Use when: running the app, demoing,
  E2E-testing against containers, or diagnosing container startup/login
  failures. Triggers: "run the app", "start the stack", "demo", "docker up",
  "can't log in", "launch koinon".
---

# Koinon Demo Stack

## Launch

```bash
docker compose -f docker-compose.full.yml up -d --build
```

- Web: http://localhost:3000 · API: http://localhost:5000 · Swagger: /swagger
- Postgres is published on 5432 (koinon/koinon), Redis is internal.
- Migrations apply automatically on API start (`Database__MigrateOnStartup=true`
  is set in the compose file — containerized dev only, never production).

## Seed demo data (first run, or after `down -v`)

The host `dotnet` may not match `global.json` (pinned 8.0.416), so run the
seeder in an SDK 8 container on the compose network:

```bash
docker run --rm -v "<repo-root>:/repo" -w /repo --network koinon_network \
  mcr.microsoft.com/dotnet/sdk:8.0-alpine \
  dotnet run --project tools/Koinon.TestDataSeeder -- seed \
  --connection "Host=postgres;Port=5432;Database=koinon;Username=koinon;Password=koinon"
```

Add `--reset` to truncate first. Demo login: **john.smith@example.com / admin123**.
No campus is seeded, so the first `/admin` visit auto-launches the setup wizard
— that is intended onboarding, not a bug.

## Verify (do all three before declaring it up)

```bash
curl -s http://localhost:5000/health                    # → Healthy
curl -s http://localhost:3000/health                    # → healthy
curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john.smith@example.com","password":"admin123"}'   # → { data: { accessToken... } }
```

## Known failure modes

| Symptom | Cause / fix |
|---------|-------------|
| API crash: "PostgreSQL connection string not configured" | Compose must set `ConnectionStrings__DefaultConnection` (not `__Koinon`) |
| Migration fails on `postgis` extension | Postgres image must be `postgis/postgis:*` (plain `postgres` lacks it) |
| Browser CORS errors from :3000 | `Cors__AllowedOrigins__0=http://localhost:3000` env on the api service |
| Web image build: "nginx.conf not found" | web build context must be `./src/web`, not repo root |
| API image build: MSB3202 missing tools/*.csproj | `src/Koinon.Api/Dockerfile` must build the API project, never the solution |
| Login 401 with correct creds | DB not seeded — run the seeder above |

## Teardown

```bash
docker compose -f docker-compose.full.yml down        # keep data
docker compose -f docker-compose.full.yml down -v     # destroy data (reseed after)
```
