import { useEffect, useRef } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useRegisterSW } from 'virtual:pwa-register/react';
import { LoginForm, ProtectedRoute } from './components/auth';
import { useAuth } from './hooks/useAuth';
import { CheckinPage } from './pages/CheckinPage';
import { ErrorBoundary } from './components/ErrorBoundary';
import { AdminLayout } from './layouts/AdminLayout';
import {
  DashboardPage,
  GroupsPage,
  SettingsPage,
  AnalyticsPage,
} from './pages/admin';
import {
  GroupsTreePage,
  GroupDetailPage,
  GroupFormPage,
} from './pages/admin/groups';
import {
  PeopleListPage,
  PersonDetailPage,
  PersonFormPage,
} from './pages/admin/people';
import {
  FamilyListPage,
  FamilyDetailPage,
  FamilyFormPage,
} from './pages/admin/families';
import {
  ScheduleListPage,
  ScheduleDetailPage,
  ScheduleFormPage,
} from './pages/admin/schedules';
import { PWAUpdatePrompt, InstallPrompt } from './components/pwa';

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
                href="/admin"
                className="inline-block px-6 py-3 bg-blue-600 text-white font-medium rounded-lg hover:bg-blue-700 transition-colors touch-target"
              >
                Admin Dashboard
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
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const registrationRef = useRef<ServiceWorkerRegistration | null>(null);

  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegistered(r: ServiceWorkerRegistration | undefined) {
      if (r) {
        registrationRef.current = r;
      }
    },
    onRegisterError() {
      // Service worker registration failed - app will still work without offline support
    },
  });

  // Set up periodic update check with proper cleanup
  useEffect(() => {
    if (registrationRef.current && !intervalRef.current) {
      intervalRef.current = setInterval(() => {
        registrationRef.current?.update();
      }, 60 * 60 * 1000); // Check for updates every hour
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, []); // Only set up once on mount

  const handleUpdate = () => {
    updateServiceWorker(true);
  };

  return (
    <ErrorBoundary>
      <PWAUpdatePrompt onUpdate={handleUpdate} offlineReady={needRefresh} />
      <InstallPrompt />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />

        {/* Legacy people route - redirect to admin */}
        <Route
          path="/people"
          element={
            <ProtectedRoute>
              <Navigate to="/admin/people" replace />
            </ProtectedRoute>
          }
        />

        {/* Admin routes */}
        <Route
          path="/admin"
          element={
            <ProtectedRoute>
              <AdminLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<DashboardPage />} />
          <Route path="people" element={<PeopleListPage />} />
          <Route path="people/new" element={<PersonFormPage />} />
          <Route path="people/:idKey" element={<PersonDetailPage />} />
          <Route path="people/:idKey/edit" element={<PersonFormPage />} />
          <Route path="families" element={<FamilyListPage />} />
          <Route path="families/new" element={<FamilyFormPage />} />
          <Route path="families/:idKey" element={<FamilyDetailPage />} />
          <Route path="families/:idKey/edit" element={<FamilyFormPage />} />
          <Route path="groups" element={<GroupsPage />} />
          <Route path="groups/tree" element={<GroupsTreePage />} />
          <Route path="groups/new" element={<GroupFormPage />} />
          <Route path="groups/:idKey" element={<GroupDetailPage />} />
          <Route path="groups/:idKey/edit" element={<GroupFormPage />} />
          <Route path="schedules" element={<ScheduleListPage />} />
          <Route path="schedules/new" element={<ScheduleFormPage />} />
          <Route path="schedules/:idKey" element={<ScheduleDetailPage />} />
          <Route path="schedules/:idKey/edit" element={<ScheduleFormPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>

        {/* Check-in route */}
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
