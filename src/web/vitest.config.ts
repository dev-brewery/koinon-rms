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
        'src/**/types.ts',
        'src/types/**',
        // Entry points: validated by build / E2E.
        'src/main.tsx',
        'src/App.tsx',
        'src/vite-env.d.ts',
      ],
      thresholds: {
        // Honest-denominator floor. Wave 4 (#498) reached the realistic unit
        // ceiling by testing the remaining branch-rich, low-dependency code:
        //   - services/printing/PrintBridgeClient.ts (7 methods, 24 branches)
        //   - api/client.ts (auth + 401 handling)
        //   - services/errorTracking.ts (Sentry guards)
        //   - components/RouteErrorBoundary.tsx (4 status branches + JS path)
        //   - features/followups/{FollowUpCard,FollowUpQueue}.tsx
        //   - features/pager/{PagerSearch,PageHistory,SendPageDialog}.tsx
        //   - features/authorized-pickup/{AuthorizedPickupList,
        //     PickupHistoryPanel}.tsx
        //   - components/groups/{GroupCard,PendingRequestsList,
        //     RequestToJoinModal}.tsx
        //   - components/common/PhotoUpload.tsx
        //   - components/notifications/NotificationList.tsx
        //   - hooks/usePrintBridgeConnection.ts
        //
        // Achieved wave-4 floor:
        //   lines 37.60, statements 37.84, functions 41.09, branches 25.50
        // Thresholds set ~2 pts below achieved (small buffer so minor churn
        // does not break CI). No coverage.exclude changes — every point of
        // movement comes from real tests.
        //
        // The remaining gap to higher coverage is E2E territory: full admin
        // pages (pages/admin/**, pages/communications/**, pages/settings/**)
        // and large stateful modals that require a router + multiple hook
        // providers.
        lines: 35,
        statements: 35,
        functions: 38,
        branches: 23,
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
