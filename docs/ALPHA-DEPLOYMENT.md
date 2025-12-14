# Koinon RMS Alpha - Deployment Guide

**Version:** 0.1.0-alpha
**Last Updated:** December 2025

This guide walks through deploying Koinon RMS for alpha testing using Docker Compose.

## Prerequisites

### Required Software

| Software | Minimum Version | Check Command |
|----------|-----------------|---------------|
| Docker | 24.0+ | `docker --version` |
| Docker Compose | 2.20+ | `docker compose version` |
| Git | 2.30+ | `git --version` |

### Hardware Requirements

**Minimum (Testing):**
- 2 CPU cores
- 4 GB RAM
- 10 GB free disk space

**Recommended (Alpha Deployment):**
- 4 CPU cores
- 8 GB RAM
- 50 GB SSD

### Network Requirements

| Port | Service | Purpose |
|------|---------|---------|
| 5000 | API | Backend API server |
| 5173 | Frontend | React development server |
| 5432 | PostgreSQL | Database (internal) |
| 6379 | Redis | Cache (internal) |

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/dev-brewery/koinon-rms.git
cd koinon-rms
```

### 2. Start Infrastructure

```bash
docker compose up -d
```

This starts:
- PostgreSQL database
- Redis cache
- API server
- Frontend dev server

### 3. Verify Services

```bash
# Check all services are running
docker compose ps

# Check API health
curl http://localhost:5000/health

# Check database connectivity
docker compose exec postgres pg_isready
```

### 4. Access the Application

| URL | Purpose |
|-----|---------|
| http://localhost:5173 | Main application |
| http://localhost:5173/checkin | Check-in kiosk |
| http://localhost:5173/admin | Admin console |
| http://localhost:5000/swagger | API documentation |

### 5. Login

Default test credentials:

| Email | Password | Role |
|-------|----------|------|
| john.smith@example.com | admin123 | Admin |
| jane.doe@example.com | user123 | Standard User |

## Detailed Setup

### Environment Configuration

Create a `.env` file in the project root (optional - defaults are provided):

```bash
# Database
POSTGRES_USER=koinon
POSTGRES_PASSWORD=""  # Set your database password
POSTGRES_DB=koinon

# Redis
REDIS_URL=redis://localhost:6379

# API
API_URL=http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development

# Frontend
VITE_API_URL=http://localhost:5000/api/v1
```

### Database Initialization

The database is automatically:
1. Created on first start
2. Migrated to latest schema
3. Seeded with test data

To reset the database:

```bash
# Stop services
docker compose down

# Remove database volume
docker volume rm koinon-rms_postgres-data

# Restart (will recreate and reseed)
docker compose up -d
```

### Viewing Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f api
docker compose logs -f postgres
docker compose logs -f redis
```

## Production-like Deployment

For a more production-like alpha deployment:

### 1. Use Production Build

```bash
# Build production frontend
cd src/web
npm run build

# Build production API
cd ../..
docker compose -f docker-compose.prod.yml up -d
```

### 2. Configure SSL (Optional)

For HTTPS, place certificates in `./certs/` and update docker-compose:

```yaml
services:
  nginx:
    image: nginx:alpine
    volumes:
      - ./certs:/etc/nginx/certs:ro
    ports:
      - "443:443"
```

### 3. External Database (Optional)

To use an external PostgreSQL instance:

```bash
# Set connection string (replace YOUR_HOST and YOUR_PASS with actual values)
export DATABASE_URL="Host=YOUR_HOST;Database=koinon;Username=user;Pass=YOUR_PASS"

# Run without local postgres
docker compose up -d api frontend
```

## Troubleshooting

### Services Won't Start

```bash
# Check for port conflicts
sudo lsof -i :5000
sudo lsof -i :5432

# View detailed logs
docker compose logs --tail=50
```

### Database Connection Failed

```bash
# Verify PostgreSQL is accepting connections
docker compose exec postgres pg_isready -U koinon

# Check connection from API container
docker compose exec api dotnet ef database update --dry-run
```

### Frontend Can't Reach API

```bash
# Verify API is responding
curl http://localhost:5000/health

# Check CORS configuration
curl -I -X OPTIONS http://localhost:5000/api/v1/people
```

### Out of Disk Space

```bash
# Clean Docker resources
docker system prune -a

# Remove unused volumes
docker volume prune
```

### Reset Everything

```bash
# Nuclear option - removes all data
docker compose down -v
docker system prune -a
docker compose up -d
```

## Backup and Restore

### Backup Database

```bash
docker compose exec postgres pg_dump -U koinon koinon > backup.sql
```

### Restore Database

```bash
docker compose exec -T postgres psql -U koinon koinon < backup.sql
```

## Updating

To update to a newer alpha version:

```bash
# Pull latest code
git pull origin main

# Rebuild and restart
docker compose down
docker compose build --no-cache
docker compose up -d

# Run migrations (if any)
docker compose exec api dotnet ef database update
```

## Support

For deployment issues:
1. Check the [Known Issues](./ALPHA-KNOWN-ISSUES.md) document
2. Search GitHub Issues
3. Create a new issue with:
   - Docker version and host OS
   - Output of `docker compose logs`
   - Steps to reproduce
