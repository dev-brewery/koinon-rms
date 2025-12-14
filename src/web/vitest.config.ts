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
      exclude: [
        'node_modules/',
        'src/test/',
        '**/*.test.ts',
        '**/*.test.tsx',
      ],
      thresholds: {
        global: {
          lines: 70,
          statements: 70,
          functions: 70,
          branches: 60,
        },
        // Critical path: offline services (current baseline: ~56% lines, 19% branches)
        // TODO(#165): Incrementally increase to 85% as test coverage improves
        'services/offline/**/*.ts': {
          lines: 55,
          statements: 56,
          functions: 90,
          branches: 19,
        },
        // Critical path: checkin hooks - not yet tested
        // TODO(#165): Add tests for useCheckin.ts and useOfflineCheckin.ts, then set 85% thresholds
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
