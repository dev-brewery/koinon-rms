/**
 * Jest configuration for frontend graph generator tests.
 *
 * Uses CommonJS since generate-frontend.js uses require().
 */

module.exports = {
  testEnvironment: 'node',

  // Test file pattern
  testMatch: [
    '**/tests/**/*.test.js',
    '**/__tests__/**/*.test.js'
  ],

  // Coverage configuration
  collectCoverageFrom: [
    'generate-frontend.js',
    '!**/node_modules/**',
    '!**/tests/**',
    '!**/__tests__/**'
  ],

  coverageThreshold: {
    global: {
      branches: 75,    // Slightly lower due to main() function branches
      functions: 80,
      lines: 80,
      statements: 80
    }
  },

  // Coverage reporters
  coverageReporters: [
    'text',
    'text-summary',
    'html',
    'lcov'
  ],

  // Ignore node_modules and Python files
  testPathIgnorePatterns: [
    '/node_modules/',
    '\\.py$'
  ],

  // Clear mocks between tests
  clearMocks: true,

  // Verbose output
  verbose: true,

  // Timeout for long-running tests
  testTimeout: 10000
};
