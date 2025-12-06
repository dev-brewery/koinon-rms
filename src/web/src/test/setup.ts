/**
 * Test setup file
 * Runs before all tests
 */

import { expect, afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import * as matchers from '@testing-library/jest-dom/matchers';

// Extend vitest expect with jest-dom matchers
expect.extend(matchers);

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock environment variables
vi.stubEnv('VITE_API_URL', 'http://localhost:5000/api/v1');

// Extend expect with custom matchers if needed
declare global {
  interface Window {
    fetch: typeof fetch;
  }
}
