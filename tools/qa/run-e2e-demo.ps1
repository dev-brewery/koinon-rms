# run-e2e-demo.ps1 — one-command browser tests against the Koinon demo stack.
#
# For anyone: junior QA, product owner, PM, or a developer verifying a feature.
# Starts the Docker stack if it's down, seeds demo data if the demo login
# fails, runs the Playwright suite against http://localhost:3000, and opens
# the HTML report.
#
# Usage:
#   .\tools\qa\run-e2e-demo.ps1            # smoke suite (fast golden path)
#   .\tools\qa\run-e2e-demo.ps1 -All       # full E2E suite
#   .\tools\qa\run-e2e-demo.ps1 -Grep "checkin"   # one feature area
#
# Requirements: Docker Desktop running, Node.js >= 18.

param(
    [switch]$All,
    [string]$Grep = ""
)

$ErrorActionPreference = "Stop"
$repo = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $repo

function Test-Health {
    try { (Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 }
    catch { $false }
}

function Test-DemoLogin {
    try {
        $body = '{"email":"john.smith@example.com","password":"admin123"}'
        $r = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 10
        [bool]$r.data.accessToken
    } catch { $false }
}

# 1. Stack up?
if (-not (Test-Health)) {
    Write-Host "Stack is down - starting Docker demo stack (first build can take ~5 min)..." -ForegroundColor Yellow
    docker compose -f docker-compose.full.yml up -d --build
    if ($LASTEXITCODE -ne 0) { throw "docker compose failed - is Docker Desktop running?" }
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Health)) {
        if ((Get-Date) -gt $deadline) { throw "API did not become healthy within 5 minutes. Check: docker logs koinon-api" }
        Start-Sleep -Seconds 5
    }
}
Write-Host "API healthy." -ForegroundColor Green

# 2. Seeded?
if (-not (Test-DemoLogin)) {
    Write-Host "Demo login failed - seeding test data (one-off container)..." -ForegroundColor Yellow
    docker run --rm -v "${repo}:/repo" -w /repo --network koinon_network `
        mcr.microsoft.com/dotnet/sdk:8.0-alpine `
        dotnet run --project tools/Koinon.TestDataSeeder -- seed `
        --connection "Host=postgres;Port=5432;Database=koinon;Username=koinon;Password=koinon"
    if (-not (Test-DemoLogin)) { throw "Seeding completed but demo login still fails. Check: docker logs koinon-api" }
}
Write-Host "Demo data present (john.smith@example.com works)." -ForegroundColor Green

# 3. Frontend deps + browser binaries
Set-Location (Join-Path $repo "src\web")
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing frontend dependencies (npm ci)..." -ForegroundColor Yellow
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed" }
}
npx playwright install chromium
if ($LASTEXITCODE -ne 0) { throw "playwright browser install failed" }

# 4. Run
$env:E2E_BASE_URL = "http://localhost:3000"
$args_ = @("playwright", "test", "--project=chromium")
if ($Grep) { $args_ += @("--grep", $Grep) }
elseif (-not $All) { $args_ += @("--grep", "@smoke") }

Write-Host "Running: npx $($args_ -join ' ') against $env:E2E_BASE_URL" -ForegroundColor Cyan
& npx @args_
$testExit = $LASTEXITCODE

# 5. Report (opens in browser regardless of pass/fail)
Write-Host "Opening HTML report..." -ForegroundColor Cyan
Start-Process "npx" -ArgumentList "playwright", "show-report" -NoNewWindow

if ($testExit -ne 0) {
    Write-Host "SOME TESTS FAILED - see the report. Traces/videos are attached to each failure." -ForegroundColor Red
    exit $testExit
}
Write-Host "ALL TESTS PASSED." -ForegroundColor Green
