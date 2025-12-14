# Proposal: Add E2E Testing Job to CI Pipeline

**Issue:** #164
**Branch:** `feature/issue-164-e2e-ci`
**Status:** Needs human approval to modify workflow file

## Overview

This proposal adds an E2E testing job to `.github/workflows/ci.yml` to run Playwright tests on every PR.

## Blocked By

- **Issue #182**: User entity and test user seeding not implemented
  - E2E tests require authentication which doesn't work yet
  - The E2E job can be added now, but tests will fail until #182 is resolved

## Proposed Changes to `.github/workflows/ci.yml`

### 1. Add E2E Job (insert after `integration` job, before `work-unit-validation`)

```yaml
  # End-to-End tests using Playwright
  e2e:
    name: E2E Tests
    runs-on: [self-hosted, linux]
    needs: [backend, frontend]
    env:
      # Define CI test values at job level - copy actual values from integration job
      # Note: Use same Jwt__ config key names as integration job (Jwt__Secret not Jwt__SigningKey)
      CI_DB_CONNECTION: "<copy from integration job ConnectionStrings__DefaultConnection>"
      CI_JWT_SIGNING_KEY: "<copy from integration job Jwt__ value>"

    steps:
    - name: Checkout code
      uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        global-json-file: global.json

    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: ${{ env.NODE_VERSION }}
        cache: 'npm'
        cache-dependency-path: src/web/package-lock.json

    - name: Start test services
      run: |
        docker rm -f ci-postgres-$RUN_ID ci-redis-$RUN_ID 2>/dev/null || true
        docker-compose -f docker-compose.ci.yml up -d --wait
        timeout 60 bash -c 'until docker exec ci-postgres-'"$RUN_ID"' pg_isready -U koinon -d koinon; do sleep 2; done'
      env:
        RUN_ID: ${{ github.run_id }}
        COMPOSE_PROJECT_NAME: ci-${{ github.run_id }}

    - name: Restore and build backend
      run: |
        dotnet restore
        dotnet build --no-restore --configuration Release

    - name: Apply database migrations
      run: |
        dotnet tool restore
        dotnet ef database update \
          --project src/Koinon.Infrastructure \
          --startup-project src/Koinon.Api \
          --configuration Release
      env:
        ConnectionStrings__DefaultConnection: ${{ env.CI_DB_CONNECTION }}
        Jwt__SigningKey: ${{ env.CI_JWT_SIGNING_KEY }}
        Jwt__Issuer: "koinon-ci-e2e"
        Jwt__Audience: "koinon-ci-e2e"

    - name: Seed test data
      run: |
        dotnet run --project tools/Koinon.TestDataSeeder --no-build --configuration Release
      env:
        ConnectionStrings__DefaultConnection: ${{ env.CI_DB_CONNECTION }}

    - name: Start API in background
      run: |
        dotnet run --project src/Koinon.Api --no-build --configuration Release &
        echo $! > /tmp/api.pid
        sleep 5
      env:
        ASPNETCORE_URLS: "http://0.0.0.0:5000"
        ConnectionStrings__DefaultConnection: ${{ env.CI_DB_CONNECTION }}
        ConnectionStrings__Redis: "ci-redis-${{ github.run_id }}:6379"
        Jwt__SigningKey: ${{ env.CI_JWT_SIGNING_KEY }}
        Jwt__Issuer: "koinon-ci-e2e"
        Jwt__Audience: "koinon-ci-e2e"

    - name: Wait for API to be ready
      run: |
        timeout 120 bash -c 'until curl -sf http://localhost:5000/health > /dev/null 2>&1; do sleep 3; done'

    - name: Install frontend dependencies
      working-directory: src/web
      run: npm ci

    - name: Build frontend
      working-directory: src/web
      run: npm run build
      env:
        VITE_API_BASE_URL: "http://localhost:5000"

    - name: Start frontend preview server
      working-directory: src/web
      run: |
        npm run preview -- --port 5173 --host 0.0.0.0 &
        echo $! > /tmp/vite.pid
        sleep 5
      env:
        VITE_API_BASE_URL: "http://localhost:5000"

    - name: Wait for frontend to be ready
      run: |
        timeout 60 bash -c 'until curl -sf http://localhost:5173 > /dev/null 2>&1; do sleep 2; done'

    - name: Install Playwright browsers
      working-directory: src/web
      run: npx playwright install --with-deps chromium

    - name: Run E2E tests
      working-directory: src/web
      run: npx playwright test
      env:
        E2E_BASE_URL: "http://localhost:5173"
        API_BASE_URL: "http://localhost:5000"
        CI: true

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: e2e-results
        path: |
          src/web/playwright-report/
          src/web/e2e-results.json
        retention-days: 7

    - name: Upload failure artifacts
      if: failure()
      uses: actions/upload-artifact@v4
      with:
        name: e2e-failure-artifacts
        path: |
          src/web/test-results/
        retention-days: 7

    - name: Cleanup
      if: always()
      run: |
        if [ -f /tmp/api.pid ]; then
          kill $(cat /tmp/api.pid) 2>/dev/null || true
        fi
        if [ -f /tmp/vite.pid ]; then
          kill $(cat /tmp/vite.pid) 2>/dev/null || true
        fi
        docker rm -f ci-postgres-${{ github.run_id }} ci-redis-${{ github.run_id }} 2>/dev/null || true
```

### 2. Update `ci-success` job

Change the `needs` array from:
```yaml
needs: [backend, frontend, migration-check, integration, work-unit-validation]
```

To:
```yaml
needs: [backend, frontend, migration-check, integration, e2e, work-unit-validation]
```

And add E2E check to the condition:
```yaml
[[ "${{ needs.e2e.result }}" == "success" ]] && \
```

## Application Instructions

Since workflow files are protected infrastructure (Rule 10), please apply these changes manually:

1. Open `.github/workflows/ci.yml`
2. Add the E2E job after the `integration` job (around line 441)
3. Update the `ci-success` job's `needs` array and condition check
4. Commit with message: `ci: add e2e testing job to pipeline (#164)`

## Notes

- **IMPORTANT**: The `CI_DB_CONNECTION`, `CI_JWT_SIGNING_KEY`, and `Jwt__SigningKey` placeholders should be replaced with actual values from the existing `integration` job (use `Jwt__Secret` not `Jwt__SigningKey`)
- Sharding is not enabled initially - can be added later when test count exceeds ~50 tests
- Frontend is built and served via Vite preview server (port 5173)
- API runs on port 5000, frontend connects to it via `VITE_API_BASE_URL`
- Videos and screenshots are only uploaded on failure to save storage
- Test results are always uploaded for debugging
- The job depends on `backend` and `frontend` passing first
- E2E tests will likely fail until Issue #182 (User entity) is resolved
