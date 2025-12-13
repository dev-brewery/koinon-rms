/**
 * Route Error Boundary Component
 * Catches errors within specific routes and provides contextual error UI
 */

import { useRouteError, isRouteErrorResponse, Link } from 'react-router-dom';

export function RouteErrorBoundary() {
  const error = useRouteError();

  // Handle React Router errors (404, etc.)
  if (isRouteErrorResponse(error)) {
    if (error.status === 404) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <div className="text-center max-w-md px-4">
            <h1 className="text-6xl font-bold text-gray-900 mb-4">404</h1>
            <p className="text-xl text-gray-600 mb-2">Page Not Found</p>
            <p className="text-gray-500 mb-8">
              The page you are looking for does not exist or has been moved.
            </p>
            <div className="flex gap-4 justify-center">
              <Link
                to="/admin"
                className="px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
              >
                Go to Dashboard
              </Link>
              <Link
                to="/"
                className="px-6 py-3 bg-gray-600 text-white font-medium rounded-lg hover:bg-gray-700 transition-colors"
              >
                Go Home
              </Link>
            </div>
          </div>
        </div>
      );
    }

    if (error.status === 403) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <div className="text-center max-w-md px-4">
            <h1 className="text-6xl font-bold text-gray-900 mb-4">403</h1>
            <p className="text-xl text-gray-600 mb-2">Access Denied</p>
            <p className="text-gray-500 mb-8">
              You do not have permission to access this page.
            </p>
            <Link
              to="/admin"
              className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
            >
              Go to Dashboard
            </Link>
          </div>
        </div>
      );
    }

    if (error.status === 500) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <div className="text-center max-w-md px-4">
            <h1 className="text-6xl font-bold text-gray-900 mb-4">500</h1>
            <p className="text-xl text-gray-600 mb-2">Server Error</p>
            <p className="text-gray-500 mb-8">
              An error occurred on the server. Please try again later.
            </p>
            <div className="flex gap-4 justify-center">
              <button
                onClick={() => window.location.reload()}
                className="px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
              >
                Reload Page
              </button>
              <Link
                to="/admin"
                className="px-6 py-3 bg-gray-600 text-white font-medium rounded-lg hover:bg-gray-700 transition-colors"
              >
                Go to Dashboard
              </Link>
            </div>
          </div>
        </div>
      );
    }

    // Generic HTTP error
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center max-w-md px-4">
          <h1 className="text-6xl font-bold text-gray-900 mb-4">{error.status}</h1>
          <p className="text-xl text-gray-600 mb-2">{error.statusText}</p>
          {error.data?.message && (
            <p className="text-gray-500 mb-8">{error.data.message}</p>
          )}
          <Link
            to="/admin"
            className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            Go to Dashboard
          </Link>
        </div>
      </div>
    );
  }

  // Handle JavaScript errors
  const jsError = error instanceof Error ? error : new Error('Unknown error');

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full bg-white shadow-lg rounded-lg p-8">
        <div className="text-center mb-6">
          <div className="text-6xl mb-4">⚠️</div>
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            Something went wrong
          </h1>
          <p className="text-gray-600">
            An unexpected error occurred while loading this page.
          </p>
        </div>

        {import.meta.env.DEV && (
          <div className="mb-6">
            <details className="text-left">
              <summary className="cursor-pointer text-sm font-medium text-gray-700 mb-2">
                Error Details (Development Only)
              </summary>
              <div className="bg-red-50 border border-red-200 rounded p-4 overflow-auto">
                <p className="text-sm font-mono text-red-800 mb-2">
                  {jsError.toString()}
                </p>
                {jsError.stack && (
                  <pre className="text-xs text-red-700 whitespace-pre-wrap">
                    {jsError.stack}
                  </pre>
                )}
              </div>
            </details>
          </div>
        )}

        <div className="flex gap-4">
          <button
            onClick={() => window.location.reload()}
            className="flex-1 px-4 py-2 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            Reload Page
          </button>
          <Link
            to="/admin"
            className="flex-1 px-4 py-2 bg-gray-600 text-white font-medium rounded-lg hover:bg-gray-700 transition-colors text-center"
          >
            Go to Dashboard
          </Link>
        </div>
      </div>
    </div>
  );
}
