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
        // Honest-denominator floor. Wave 3 (#498) added a shared
        // renderHookWithQuery harness and tests for ~24 TanStack Query hook
        // modules (campuses/families/people/groups/schedules/locations/
        // giving/settings/profile/notifications/security-roles/auditLogs/
        // devices/comms/comm-templates/checkin/personMerge/publicGroups/
        // supervisorMode/supervisorAttendance/globalSearch/import/import-jobs/
        // myGroups/checkinOperations/mutationWithToast + the 4 features/*
        // api+hooks pairs (admin/defined-types, followups, pager,
        // authorized-pickup) + deeper services/api coverage.
        //   lines 32.15, statements 32.07, functions 36.82, branches 19.35
        // Thresholds set ~1-2 pts below the achieved number (small buffer so
        // minor churn does not break CI). No coverage.exclude changes — every
        // point of movement comes from real tests.
        lines: 30,
        statements: 30,
        functions: 34,
        branches: 18,
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
