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
        // Honest-denominator floor. Previous config excluded ~81% of the FE
        // (pages, components, features, contexts, api, layouts, printing,
        // errorTracking) from the denominator and enforced 70/70/70/60 against
        // only ~16% of the codebase. Those exclusions have been restored to
        // the coverage measurement; the threshold here is the honest achieved
        // floor (lines 10.87, statements 10.90, functions 7.17, branches 7.45)
        // minus a small buffer. Raise as real coverage grows.
        lines: 9,
        statements: 9,
        functions: 6,
        branches: 6,
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
