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
        // Honest-denominator floor. Wave 2 (#498) added tests for contexts,
        // UI primitives, data-display components, and check-in flows against
        // the full honest denominator. No new exclusions were added — every
        // point of movement comes from real tests. Achieved floor:
        //   lines 18.21, statements 18.23, functions 14.27, branches 15.33
        // Thresholds set just below the achieved number (small buffer so
        // minor churn does not break CI). Raise further in wave 3.
        lines: 17,
        statements: 17,
        functions: 13,
        branches: 14,
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
