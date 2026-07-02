# Koinon RMS — QA Playbook (Canonical)

The testing handbook for this repo: what to test, where, how, and the traps
that only show up in production kiosks. Companion to
`docs/reference/conventions.md`. The `koinon-e2e` skill loads this; the
feature-slice checklist requires it. Change via ADR like any other canon.

## Who runs what

| Role | Command | Gets |
|------|---------|------|
| PO / PM / end user | `.\tools\qa\run-e2e-demo.ps1` (Windows) or `./tools/qa/run-e2e-demo.sh` | Golden-path smoke against the Docker stack + HTML report with videos of failures. Zero setup beyond Docker Desktop + Node. |
| Junior QA / dev, feature focus | `.\tools\qa\run-e2e-demo.ps1 -Grep "checkin"` | One feature area against the stack |
| Dev, inner loop | `cd src/web && npm run e2e:ui` | Playwright UI mode against the vite dev server (auto-started) |
| CI | `npx playwright test` in the pipeline | Full suite, 2 retries, traces on retry |

The runner scripts are self-healing: stack down → they start it; demo login
broken → they reseed; deps missing → they install. A failing run always ends
with an HTML report a non-engineer can read (screenshot + video + trace per
failure).

## The test pyramid here

1. **Unit** — Vitest (`src/web`, colocated `__tests__`) and xUnit (`tests/*`).
   Logic, validators, hooks. Fast, no I/O.
2. **Feature E2E with mocked API** — `src/web/e2e/tests/**`. Playwright with
   `page.route()` mocks (see `checkin-complete-flow.spec.ts` for the house
   style). Tests UI logic and flows deterministically without a backend.
3. **Smoke E2E against the real stack** — tagged `@smoke`, no API mocking,
   seeded data only. Proves the integrated system works. `e2e/tests/smoke.spec.ts`
   is the golden path; feature specs may add one `@smoke` test each.

Every new feature ships with (1) always, (2) for any nontrivial UI flow, and
(3) exactly one golden-path test if the feature is user-facing.

## House rules for browser tests

- **Selectors:** `getByRole` / `getByLabel` / `getByPlaceholder` — user-visible
  semantics. No CSS/XPath/test-id unless the a11y tree genuinely lacks a handle
  (then fix the a11y first — it's a product bug).
- **Data:** seeded only, via `e2e/fixtures/test-data.ts` (constants match
  `tools/Koinon.TestDataSeeder` — deterministic GUIDs). Tests that create data
  must tolerate `global-setup.ts` cleanup and never depend on other tests.
- **Auth:** `e2e/fixtures/auth.fixture.ts` (`loginAsAdmin` — note it accepts
  the setup-wizard redirect on a campus-less stack).
- **Fresh-stack truths:** the seeder creates no campus, so `/admin` redirects
  to `/admin/setup-wizard` (hiding the admin sidebar). `global-setup.ts`
  inserts a "Main Campus" row if none exists so navigation tests work; the
  auth fixture still tolerates both URLs. Specs marked
  `test.skip(!!process.env.E2E_BASE_URL, ...)` are dev-server-harness specs
  (SW offline simulation, perf baselines, fake camera) not yet validated
  against the containerized stack — triage before removing the skip.
- **Waits:** web-first assertions (`expect(locator).toBeVisible()`), never
  `waitForTimeout`. A sleep in a PR is a review rejection.
- **Tags:** `@smoke` = golden path, real stack, no mocks. Keep the smoke tier
  under ~2 minutes total or people stop running it.

## Printers and the check-in kiosk (read before touching check-in)

The kiosk prints child-security labels through the **Print Bridge**
(`tools/print-bridge`) — a Windows tray app on `localhost:9632` driving Zebra
(ZPL) and Dymo (GDI) printers. Hard-won rules:

1. **Never let a browser test touch a real driver.** Drivers are stateful,
   machine-specific, and fail in ways a test can't reset (spooler wedged,
   calibration lost, USB re-enumeration). All E2E printing goes through
   `e2e/fixtures/print-bridge.fixture.ts`, which mocks the bridge at the
   network layer and **records every payload** the app sends.
2. **Assert the payload, not the printout.** The contract is the JSON/ZPL the
   app POSTs to `/api/print` and `/api/print/batch`. If the ZPL says the right
   thing (`^XA...^XZ`, correct security code, child + pickup labels), the app
   is correct; whatever the printer does with it is the bridge team's problem.
3. **Test the failure modes — parents hit them weekly.** The fixture supports
   `setMode('offline')` (bridge not running — the most common real-world
   state), `'printer-error'` (jam / out of labels), `'no-printers'`. The kiosk
   must complete check-in and show its degraded-print path in ALL of them.
   A check-in that fails because printing failed is a P1: the child is present,
   the record must exist.
4. **Install the mock BEFORE `page.goto('/checkin')`** — the kiosk probes
   printer availability on mount; a late mock means the "no printer" UI path.
5. Real-hardware verification is a manual, release-time checklist (one Zebra +
   one Dymo, test print from the bridge tray icon, then one live check-in) —
   never a CI gate.

## Debugging failed runs

- HTML report: `npx playwright show-report` — every failure has a screenshot,
  video, and (on retry) a trace. Traces open with `npx playwright show-trace`.
- One test, headed: `npx playwright test -g "test name" --headed --project=chromium`.
- Against the stack, remember the app is the CONTAINER build — if you changed
  frontend code, rebuild: `docker compose -f docker-compose.full.yml up -d --build web`.
- Flake policy: a test that fails intermittently gets fixed or quarantined
  (`test.fixme()`) the same day. A red suite nobody trusts is worse than no suite.

## Adding a feature? (checklist hook)

The `koinon-feature-slice` skill's step 15 requires the E2E deliverables.
Write the feature spec next to its area (`e2e/tests/<area>/`), reuse the
page-object pattern (`e2e/fixtures/page-objects/`), add exactly one `@smoke`
test if user-facing, and run both modes before the PR:
dev-server (`npm run e2e -- --grep <area>`) and stack
(`.\tools\qa\run-e2e-demo.ps1 -Grep <area>`).
