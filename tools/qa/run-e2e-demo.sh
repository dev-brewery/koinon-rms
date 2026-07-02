#!/usr/bin/env bash
# run-e2e-demo.sh — one-command browser tests against the Koinon demo stack.
# See run-e2e-demo.ps1 for the Windows equivalent and full docs.
#
# Usage:
#   ./tools/qa/run-e2e-demo.sh              # smoke suite
#   ./tools/qa/run-e2e-demo.sh --all        # full suite
#   ./tools/qa/run-e2e-demo.sh --grep checkin
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO"

GREP=""
ALL=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --all) ALL=1; shift ;;
    --grep) GREP="$2"; shift 2 ;;
    *) echo "Unknown arg: $1"; exit 2 ;;
  esac
done

health() { curl -sf -m 3 http://localhost:5000/health >/dev/null 2>&1; }
demo_login() {
  curl -sf -m 10 -X POST http://localhost:5000/api/v1/auth/login \
    -H 'Content-Type: application/json' \
    -d '{"email":"john.smith@example.com","password":"admin123"}' \
    | grep -q accessToken
}

if ! health; then
  echo ">> Stack is down — starting Docker demo stack (first build ~5 min)..."
  docker compose -f docker-compose.full.yml up -d --build
  for _ in $(seq 1 60); do health && break; sleep 5; done
  health || { echo "API not healthy after 5 min. Check: docker logs koinon-api"; exit 1; }
fi
echo ">> API healthy."

if ! demo_login; then
  echo ">> Demo login failed — seeding test data..."
  docker run --rm -v "$REPO:/repo" -w /repo --network koinon_network \
    mcr.microsoft.com/dotnet/sdk:8.0-alpine \
    dotnet run --project tools/Koinon.TestDataSeeder -- seed \
    --connection "Host=postgres;Port=5432;Database=koinon;Username=koinon;Password=koinon"
  demo_login || { echo "Seeding done but login still fails. Check: docker logs koinon-api"; exit 1; }
fi
echo ">> Demo data present."

cd "$REPO/src/web"
[[ -d node_modules ]] || npm ci
npx playwright install chromium

ARGS=(playwright test --project=chromium)
if [[ -n "$GREP" ]]; then ARGS+=(--grep "$GREP")
elif [[ "$ALL" -ne 1 ]]; then ARGS+=(--grep "@smoke"); fi

echo ">> Running: npx ${ARGS[*]} against http://localhost:3000"
E2E_BASE_URL=http://localhost:3000 npx "${ARGS[@]}" && PASS=1 || PASS=0

npx playwright show-report &

if [[ "$PASS" -ne 1 ]]; then
  echo ">> SOME TESTS FAILED — see the report (traces/videos attached to failures)."
  exit 1
fi
echo ">> ALL TESTS PASSED."
