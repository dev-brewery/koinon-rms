/**
 * Test setup file
 * Runs before all tests
 */

import { afterEach, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
// Official vitest integration - automatically extends expect with jest-dom matchers
import '@testing-library/jest-dom/vitest';
// IndexedDB polyfill for tests
import 'fake-indexeddb/auto';

// Cleanup after each test
afterEach(() => {
  cleanup();
});

// Mock environment variables
vi.stubEnv('VITE_API_URL', 'http://localhost:5000/api/v1');
