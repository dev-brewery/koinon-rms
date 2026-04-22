import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    include: ['src/**/*.{test,spec}.{js,ts,tsx}'],
    globals: true,
    environment: 'happy-dom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      // Count all source files, not just imported ones, so we see the true picture.
      all: true,
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'node_modules/',
        'src/test/',
        '**/*.test.ts',
        '**/*.test.tsx',
        // Type declarations and barrel files carry no executable logic.
        '**/*.d.ts',
        '**/index.ts',
        'src/types/**',
        // Entry points and layout shells are validated by E2E / build.
        'src/main.tsx',
        'src/App.tsx',
        'src/vite-env.d.ts',
        'src/layouts/**',
        // Pages are covered end-to-end by Playwright (src/web/e2e).
        'src/pages/**',
        'src/features/**',
        // Pure TypeScript type modules under services/api (re-exports only).
        'src/services/api/types.ts',
        // Hardware-bridge client - requires a running print bridge to test.
        'src/services/printing/**',
        // UI components: full flows are Playwright-tested. Individual high-value
        // components (PhoneSearch, CheckinConfirmation, IdleWarningModal,
        // FamilyMemberList, MergeFieldPicker) have targeted unit tests; the
        // remaining leaf/page-like components are covered by e2e.
        'src/components/**',
        // Route-provider contexts (AuthContext, ToastContext) are wired in
        // App.tsx and validated by e2e login / toast flows.
        'src/contexts/**',
        // Legacy src/api/client.ts has been superseded by src/services/api.
        // Kept in the tree pending removal; not part of the active surface.
        'src/api/**',
        // Sentry bootstrap wiring — tested by E2E error flows.
        'src/services/errorTracking.ts',
      ],
      thresholds: {
        // Alpha-readiness floor. Branches lower because exhaustive branch
        // coverage is unrealistic and brittle. Adjust upward as coverage grows.
        lines: 50,
        functions: 50,
        statements: 50,
        branches: 40,
        // Critical path: offline services must stay well tested.
        // These are higher than the global floor because offline queue bugs
        // cause silent check-in data loss.
        'src/services/offline/**/*.ts': {
          lines: 80,
          statements: 80,
          functions: 85,
          branches: 50,
        },
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
