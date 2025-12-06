import { Routes, Route, Navigate } from 'react-router-dom';
import { LoginForm, ProtectedRoute } from './components/auth';
import { useAuth } from './hooks/useAuth';
import { CheckinPage } from './pages/CheckinPage';
import { ErrorBoundary } from './components/ErrorBoundary';

function HomePage() {
  const { isAuthenticated } = useAuth();

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="text-center">
        <h1 className="text-5xl font-bold text-gray-900 mb-4">
          Koinon RMS
        </h1>
        <p className="text-xl text-gray-600 mb-8">
          Modern Church Management System
        </p>
        <div className="space-x-4">
          {isAuthenticated ? (
            <>
              <a
                href="/people"
                className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors touch-target"
              >
                People
              </a>
              <a
                href="/checkin"
                className="inline-block px-6 py-3 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 transition-colors touch-target"
              >
                Check-in
              </a>
            </>
          ) : (
            <a
              href="/login"
              className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors touch-target"
            >
              Sign In
            </a>
          )}
        </div>
      </div>
    </div>
  );
}

function LoginPage() {
  const { isAuthenticated } = useAuth();

  // Redirect to home if already authenticated
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="w-full max-w-md p-8 bg-white rounded-lg shadow-lg">
        <h1 className="text-3xl font-bold text-gray-900 mb-6 text-center">
          Sign In
        </h1>
        <LoginForm />
      </div>
    </div>
  );
}

function PeoplePage() {
  const { user, logout } = useAuth();

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-4">
        <h1 className="text-3xl font-bold">People</h1>
        <div className="flex items-center gap-4">
          {user && (
            <span className="text-gray-600">
              {user.firstName} {user.lastName}
            </span>
          )}
          <button
            onClick={logout}
            className="px-4 py-2 text-sm text-gray-700 hover:text-gray-900 border border-gray-300 rounded-md hover:bg-gray-50"
          >
            Sign Out
          </button>
        </div>
      </div>
      <p className="text-gray-600">People management coming soon...</p>
    </div>
  );
}

function NotFoundPage() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-6xl font-bold text-gray-900 mb-4">404</h1>
        <p className="text-xl text-gray-600 mb-8">Page not found</p>
        <a
          href="/"
          className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors"
        >
          Go Home
        </a>
      </div>
    </div>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/people"
          element={
            <ProtectedRoute>
              <PeoplePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/checkin"
          element={
            <ProtectedRoute>
              <CheckinPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </ErrorBoundary>
  );
}

export default App;
