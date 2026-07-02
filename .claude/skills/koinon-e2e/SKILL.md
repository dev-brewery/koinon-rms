---
name: koinon-e2e
description: >
  Write and run automated browser (Playwright) tests for Koinon RMS features —
  required for every user-facing feature. Use when: implementing or reviewing
  a feature with UI, testing the check-in kiosk or label printing, adding
  smoke coverage, running E2E against the Docker stack, or debugging a failed
  browser test. Triggers: "e2e", "playwright", "browser test", "smoke test",
  "test the kiosk", "printer test", "QA", "does the feature work in the browser".
---

# Koinon E2E Testing

Canonical reference: `docs/reference/qa-playbook.md` — read it for the full
doctrine (roles, pyramid, printer rules, flake policy). This skill is the
working checklist.

## Running

| Goal | Command |
|------|---------|
| Smoke vs Docker stack (anyone) | `.\tools\qa\run-e2e-demo.ps1` / `./tools/qa/run-e2e-demo.sh` |
| One feature vs stack | `.\tools\qa\run-e2e-demo.ps1 -Grep "checkin"` |
| Full suite vs stack | `.\tools\qa\run-e2e-demo.ps1 -All` |
| Dev inner loop (from `src/web`) | `npm run e2e:ui` (starts vite automatically) |
| Report | `npx playwright show-report` (screenshots/videos/traces per failure) |

`E2E_BASE_URL` set → tests target that URL and skip the vite webServer;
unset → vite dev server on :5173 auto-starts.

## Writing a spec for a new feature (required by koinon-feature-slice step 15)

1. Location: `src/web/e2e/tests/<area>/<feature>.spec.ts`. Study the house
   style in `e2e/tests/checkin/checkin-complete-flow.spec.ts` first.
2. Import `test, expect` from `../../fixtures/auth.fixture` (gives
   `loginAsAdmin`; tolerates the setup-wizard redirect on campus-less stacks).
3. Seeded data only, from `e2e/fixtures/test-data.ts` (deterministic GUIDs
   matching tools/Koinon.TestDataSeeder). Never depend on other tests' data.
4. Selectors: `getByRole`/`getByLabel`/`getByPlaceholder`. Missing a11y handle
   = product bug, fix the component. No CSS/XPath. No `waitForTimeout`, ever —
   web-first assertions only.
5. Feature logic → mock the API with `page.route()` (house style). Golden
   path → ONE additional test tagged `@smoke`, no mocks, real seeded stack.
6. Page objects for reused flows: `e2e/fixtures/page-objects/`.
7. Verify both modes before PR: `npm run e2e -- --grep <area>` and
   `.\tools\qa\run-e2e-demo.ps1 -Grep <area>`.

## Check-in kiosk & printing (the tricky part)

Labels print via the Print Bridge (`localhost:9632`, Windows tray app —
Zebra ZPL / Dymo GDI). Browser tests NEVER touch real drivers:

```ts
import { test, expect } from '../../fixtures/print-bridge.fixture';

test('check-in prints child + pickup labels', async ({ page, printBridge }) => {
  // fixture installs the mock BEFORE navigation — the kiosk probes the
  // bridge on mount, a late mock lands you on the "no printer" UI path
  await page.goto('/checkin');
  // ... complete check-in ...
  expect(printBridge.jobs[0].labels[0].zpl).toContain('^XA');
});
```

- Assert the **payload** (ZPL/JSON sent to `/api/print[.batch]`), never the printout.
- Cover failure modes — `printBridge.setMode('offline' | 'printer-error' | 'no-printers')`.
  Check-in MUST still complete when printing fails (child is present; the
  attendance record is the security artifact). That path failing is a P1.
- Real hardware = manual release checklist only, never CI.

## Debugging

- `npx playwright test -g "name" --headed` to watch it; `--debug` to step.
- Against the stack: the app is the CONTAINER build — rebuild after frontend
  changes (`docker compose -f docker-compose.full.yml up -d --build web`).
- Intermittent failure → fix or `test.fixme()` same day (playbook flake policy).
