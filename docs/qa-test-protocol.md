# QA Test Protocol

End-to-end testing guide for Koinon RMS using Playwright.

## Prerequisites

### System Dependencies (WSL/Linux)

```bash
sudo apt-get install -y libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 libasound2t64 libpango-1.0-0 libcairo2 libatspi2.0-0 libgtk-3-0 libgdk-pixbuf2.0-0 libxshmfence1 libglu1-mesa libx11-xcb1 libxcb-dri3-0 libxcursor1 libxi6 libxtst6 fonts-liberation libu2f-udev xdg-utils
```

### Install Playwright Browsers

```bash
cd src/web
npx playwright install
```

## Directory Structure

```
src/web/
├── e2e/
│   ├── tests/                    # Test specs organized by feature
│   │   ├── auth/
│   │   │   └── login.spec.ts
│   │   └── checkin/
│   │       └── checkin-flow.spec.ts
│   ├── fixtures/
│   │   └── page-objects/         # Page Object classes
│   │       ├── login.page.ts
│   │       └── checkin.page.ts
│   └── metadata/
│       └── test-coverage.json    # Selector registry
├── playwright.config.ts          # Playwright configuration
└── e2e-results.json             # Test results (generated)
```

## Running Tests

All commands run from `src/web/`:

| Command | Description |
|---------|-------------|
| `npm run e2e` | Run all tests headless |
| `npm run e2e:headed` | Run with visible browser |
| `npm run e2e:ui` | Open Playwright UI mode |
| `npm run e2e:codegen` | Open test recorder |
| `npm run e2e:debug` | Run with debugger |
| `npm run e2e:report` | View last test report |

### Run Specific Tests

```bash
# Run single test file
npx playwright test tests/auth/login.spec.ts

# Run tests matching pattern
npx playwright test --grep "login"

# Run smoke tests only
npx playwright test --grep "@smoke"

# Run on specific browser
npx playwright test --project=chromium
npx playwright test --project=kiosk
```

## Recording a New Test

### Step 1: Start the Dev Server

In one terminal:
```bash
cd src/web
npm run dev
```

### Step 2: Launch the Recorder

In another terminal:
```bash
cd src/web
npm run e2e:codegen
```

A browser window opens at `http://localhost:5173` with the Playwright Inspector panel.

### Step 3: Record Your Actions

1. Interact with the app in the browser window
2. Watch the Inspector panel generate code in real-time
3. Use "Pick locator" tool to refine selectors
4. Click "Assert visibility" to add assertions

### Step 4: Copy the Generated Code

**IMPORTANT:** Before closing the browser, copy the generated code from the Inspector panel. Playwright does not save it automatically.

### Step 5: Close the Browser

Close the browser window to end the recording session.

### Step 6: Create the Test File

1. Create a new file in the appropriate directory:
   ```
   e2e/tests/<feature>/<name>.spec.ts
   ```

2. Paste and clean up the recorded code (see below)

3. Update `e2e/metadata/test-coverage.json`

## Writing Tests

### Test File Template

```typescript
import { test, expect } from '@playwright/test';
import { MyPage } from '../../fixtures/page-objects/my.page';

test.describe('Feature Name', () => {
  test.beforeEach(async ({ page }) => {
    const myPage = new MyPage(page);
    await myPage.goto();
  });

  test('should do something specific', async ({ page }) => {
    const myPage = new MyPage(page);

    // Actions
    await myPage.doSomething();

    // Assertions
    await expect(myPage.element).toBeVisible();
  });

  test('@smoke should complete critical flow', async ({ page }) => {
    // Smoke tests use @smoke tag
    const myPage = new MyPage(page);
    await myPage.completeCriticalFlow();
    await myPage.expectSuccess();
  });
});
```

### Page Object Template

```typescript
import { type Page, type Locator, expect } from '@playwright/test';

export class MyPage {
  readonly page: Page;
  readonly someInput: Locator;
  readonly submitButton: Locator;
  readonly successMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    // Prefer these locator strategies (in order):
    this.someInput = page.getByLabel('Field Label');           // 1. Accessible name
    this.submitButton = page.getByRole('button', { name: 'Submit' }); // 2. Role + name
    this.successMessage = page.getByTestId('success-message'); // 3. Test ID
  }

  async goto() {
    await this.page.goto('/my-route');
  }

  async fillForm(value: string) {
    await this.someInput.fill(value);
  }

  async submit() {
    await this.submitButton.click();
  }

  async expectSuccess() {
    await expect(this.successMessage).toBeVisible();
  }
}
```

### Locator Priority

Use locators in this order of preference:

1. **Accessible name:** `getByLabel()`, `getByRole()`, `getByText()`
2. **Test ID:** `getByTestId()` - for elements without good accessible names
3. **CSS selectors:** Last resort only

### Adding Test IDs to Components

```tsx
// In React component
<div data-testid="family-member-card">...</div>
```

## Cleaning Recorded Code

Playwright codegen generates verbose code. Clean it up:

### Before (recorded)

```typescript
test('test', async ({ page }) => {
  await page.goto('http://localhost:5173/');
  await page.getByRole('link', { name: 'Sign In' }).click();
  await page.getByRole('textbox', { name: 'Username' }).click();
  await page.getByRole('textbox', { name: 'Username' }).fill('admin');
  await page.getByRole('textbox', { name: 'Username' }).press('Tab');
  await page.getByRole('textbox', { name: 'Password' }).fill('admin123');
  await page.getByRole('button', { name: 'Sign In' }).click();
});
```

### After (cleaned)

```typescript
test('should login with valid credentials', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();

  await loginPage.login('admin', 'admin123');
  await loginPage.expectLoggedIn();
});
```

**Remove:**
- Unnecessary `.click()` before `.fill()`
- `.press('Tab')` actions
- Hardcoded `localhost:5173` URLs
- Duplicate actions

**Add:**
- Descriptive test name
- Page Object usage
- Meaningful assertions

## Updating test-coverage.json

After adding a test, update the metadata:

```json
{
  "tests": {
    "feature/my-test.spec.ts": {
      "covers": [
        "Description of what this test covers"
      ],
      "routes": ["/my-route"],
      "components": ["MyComponent"],
      "selectors": [
        { "selector": "[data-testid='my-element']", "purpose": "Description" }
      ],
      "tags": ["feature", "smoke"]
    }
  },
  "selectorRegistry": {
    "[data-testid='my-element']": ["feature/my-test.spec.ts"]
  }
}
```

## Bug Discovery Protocol

When tests fail or you discover bugs during recording:

### 1. Verify It's a Real Bug

- Not a test environment issue
- Not a flaky test
- Reproducible manually

### 2. Check for Duplicates

Search existing issues before creating:
```bash
gh issue list --label QA-Discovered
```

### 3. Create GitHub Issue

Use the `QA-Discovered` label:

```bash
gh issue create \
  --title "Brief description of the bug" \
  --body "## Description
What happened

## Steps to Reproduce
1. Step one
2. Step two

## Expected Behavior
What should happen

## Actual Behavior
What actually happens

## Discovery Method
Found during E2E test recording/execution" \
  --label "QA-Discovered,bug"
```

### 4. Add Failing Test (Optional)

If appropriate, add a test that documents the expected behavior:

```typescript
test.fixme('should submit form on Enter key', async ({ page }) => {
  // This test documents expected behavior
  // Remove .fixme when bug is fixed
  await page.getByLabel('Password').press('Enter');
  await expect(page).toHaveURL('/dashboard');
});
```

## Test Tags

| Tag | Purpose |
|-----|---------|
| `@smoke` | Critical path tests, run frequently |
| `@slow` | Tests that take longer than usual |
| `@flaky` | Known flaky tests (investigate and fix) |

## Configuration Reference

Key settings in `playwright.config.ts`:

| Setting | Value | Purpose |
|---------|-------|---------|
| `testDir` | `./e2e/tests` | Test file location |
| `fullyParallel` | `true` | Run tests in parallel |
| `retries` | 2 (CI only) | Retry failed tests in CI |
| `trace` | `on-first-retry` | Capture trace on failure |
| `screenshot` | `only-on-failure` | Screenshot failed tests |
| `video` | `retain-on-failure` | Video of failed tests |

### Test Projects

| Project | Use Case |
|---------|----------|
| `chromium` | Desktop admin workflows |
| `kiosk` | Tablet check-in (iPad Pro, touch enabled) |

## Troubleshooting

### Browser Won't Launch

Install system dependencies (see Prerequisites).

### Tests Timeout

1. Increase timeout in test: `test.setTimeout(60000)`
2. Check if dev server is running
3. Check network/API issues

### Flaky Tests

1. Add explicit waits: `await expect(element).toBeVisible()`
2. Avoid timing-based assertions
3. Use `test.retry(2)` for known flaky tests
4. Investigate and fix root cause

### Can't Find Element

1. Check selector in browser DevTools
2. Element may not be rendered yet - add wait
3. Element may be in shadow DOM
4. Selector may have changed - update test
