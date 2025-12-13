/**
 * Network Utility Functions
 * Centralized network error detection
 */

/**
 * Check if an error is a network error
 * Centralized detection to avoid duplication
 */
export function isNetworkError(error: unknown): boolean {
  if (error instanceof TypeError) {
    return (
      error.message.includes('fetch') ||
      error.message.includes('network') ||
      error.message.includes('Failed to fetch')
    );
  }
  return false;
}
