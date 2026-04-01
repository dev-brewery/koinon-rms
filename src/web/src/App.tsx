import { lazy, Suspense, useEffect, useRef } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useRegisterSW } from 'virtual:pwa-register/react';
import { LoginForm, ProtectedRoute } from './components/auth';
import { useAuth } from './hooks/useAuth';
import { ErrorBoundary } from './components/ErrorBoundary';
import { RouteErrorBoundary } from './components/RouteErrorBoundary';
import { AdminLayout } from './layouts/AdminLayout';
import { ToastProvider } from './contexts/ToastContext';
import { ToastContainer } from './components/ui';
import { DashboardPage } from './pages/admin/DashboardPage';
import { PWAUpdatePrompt, InstallPrompt } from './components/pwa';

// Lazy-loaded pages — only loaded when their route is visited
const CheckinPage = lazy(() => import('./pages/CheckinPage').then(m => ({ default: m.CheckinPage })));
const SettingsPage = lazy(() => import('./pages/admin').then(m => ({ default: m.SettingsPage })));
const AnalyticsPage = lazy(() => import('./pages/admin').then(m => ({ default: m.AnalyticsPage })));
const GroupsTreePage = lazy(() => import('./pages/admin/groups').then(m => ({ default: m.GroupsTreePage })));
const GroupDetailPage = lazy(() => import('./pages/admin/groups').then(m => ({ default: m.GroupDetailPage })));
const GroupFormPage = lazy(() => import('./pages/admin/groups').then(m => ({ default: m.GroupFormPage })));
const PeopleListPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.PeopleListPage })));
const PersonDetailPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.PersonDetailPage })));
const PersonFormPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.PersonFormPage })));
const DuplicateReviewPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.DuplicateReviewPage })));
const PersonComparisonPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.PersonComparisonPage })));
const PersonMergePage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.PersonMergePage })));
const MergeHistoryPage = lazy(() => import('./pages/admin/people').then(m => ({ default: m.MergeHistoryPage })));
const FamilyListPage = lazy(() => import('./pages/admin/families').then(m => ({ default: m.FamilyListPage })));
const FamilyDetailPage = lazy(() => import('./pages/admin/families').then(m => ({ default: m.FamilyDetailPage })));
const FamilyFormPage = lazy(() => import('./pages/admin/families').then(m => ({ default: m.FamilyFormPage })));
const ScheduleListPage = lazy(() => import('./pages/admin/schedules').then(m => ({ default: m.ScheduleListPage })));
const ScheduleDetailPage = lazy(() => import('./pages/admin/schedules').then(m => ({ default: m.ScheduleDetailPage })));
const ScheduleFormPage = lazy(() => import('./pages/admin/schedules').then(m => ({ default: m.ScheduleFormPage })));
const GroupTypesPage = lazy(() => import('./pages/admin/settings/GroupTypesPage').then(m => ({ default: m.GroupTypesPage })));
const ImportSettingsPage = lazy(() => import('./pages/admin/settings/ImportSettingsPage').then(m => ({ default: m.ImportSettingsPage })));
const CampusesPage = lazy(() => import('./pages/admin/settings/CampusesPage').then(m => ({ default: m.CampusesPage })));
const LocationsPage = lazy(() => import('./pages/admin/settings/LocationsPage').then(m => ({ default: m.LocationsPage })));
const AuditLogsPage = lazy(() => import('./pages/admin/settings/AuditLogsPage').then(m => ({ default: m.AuditLogsPage })));
const GroupFinderPage = lazy(() => import('./pages/public/GroupFinderPage').then(m => ({ default: m.GroupFinderPage })));
const MyGroupsPage = lazy(() => import('./pages/MyGroupsPage').then(m => ({ default: m.MyGroupsPage })));
const CommunicationsPage = lazy(() => import('./pages/communications/CommunicationsPage').then(m => ({ default: m.CommunicationsPage })));
const CommunicationDetailPage = lazy(() => import('./pages/communications/CommunicationDetailPage').then(m => ({ default: m.CommunicationDetailPage })));
const TemplatesPage = lazy(() => import('./pages/communications/TemplatesPage').then(m => ({ default: m.TemplatesPage })));
const TemplateFormPage = lazy(() => import('./pages/communications/TemplateFormPage').then(m => ({ default: m.TemplateFormPage })));
const MyProfilePage = lazy(() => import('./pages/profile').then(m => ({ default: m.MyProfilePage })));
const UserSettingsPage = lazy(() => import('./pages/settings/UserSettingsPage').then(m => ({ default: m.UserSettingsPage })));
const RosterPage = lazy(() => import('./pages/admin/RosterPage').then(m => ({ default: m.RosterPage })));
const BatchListPage = lazy(() => import('./pages/admin/giving').then(m => ({ default: m.BatchListPage })));
const BatchDetailPage = lazy(() => import('./pages/admin/giving').then(m => ({ default: m.BatchDetailPage })));
const BatchFormPage = lazy(() => import('./pages/admin/giving').then(m => ({ default: m.BatchFormPage })));
const StatementsPage = lazy(() => import('./pages/admin/giving').then(m => ({ default: m.StatementsPage })));
const DataExportsPage = lazy(() => import('./features/admin/DataExportsPage').then(m => ({ default: m.DataExportsPage })));
const PeopleImportPage = lazy(() => import('./pages/admin/import/PeopleImportPage').then(m => ({ default: m.PeopleImportPage })));
const FamiliesImportPage = lazy(() => import('./pages/admin/import/FamiliesImportPage').then(m => ({ default: m.FamiliesImportPage })));
const ImportHistoryPage = lazy(() => import('./pages/admin/import/ImportHistoryPage').then(m => ({ default: m.ImportHistoryPage })));
const SearchResultsPage = lazy(() => import('./pages/SearchResultsPage').then(m => ({ default: m.SearchResultsPage })));
const CheckinConfigPage = lazy(() => import('./pages/admin/checkin/CheckinConfigPage').then(m => ({ default: m.CheckinConfigPage })));

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
    return <Navigate to="/admin" replace />;
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
      <ToastProvider>
        <PWAUpdatePrompt onUpdate={handleUpdate} offlineReady={needRefresh} />
        <InstallPrompt />
        <ToastContainer />
        <Suspense fallback={null}>
        <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />

        {/* Public routes (no auth required) */}
        <Route path="/groups" element={<GroupFinderPage />} />

        {/* My Groups - Protected route for group leaders */}
        <Route
          path="/my-groups"
          element={
            <ProtectedRoute>
              <MyGroupsPage />
            </ProtectedRoute>
          }
        />

        {/* My Profile - Protected route for authenticated users */}
        <Route
          path="/my-profile"
          element={
            <ProtectedRoute>
              <MyProfilePage />
            </ProtectedRoute>
          }
        />

        {/* User Settings - Protected route for authenticated users */}
        <Route
          path="/settings"
          element={
            <ProtectedRoute>
              <UserSettingsPage />
            </ProtectedRoute>
          }
        />

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
          errorElement={<RouteErrorBoundary />}
        >
          <Route index element={<DashboardPage />} />
          <Route path="search" element={<SearchResultsPage />} />
          <Route path="people" element={<PeopleListPage />} />
          <Route path="people/new" element={<PersonFormPage />} />
          <Route path="people/duplicates" element={<DuplicateReviewPage />} />
          <Route path="people/compare" element={<PersonComparisonPage />} />
          <Route path="people/merge" element={<PersonMergePage />} />
          <Route path="people/merge-history" element={<MergeHistoryPage />} />
          <Route path="people/:idKey" element={<PersonDetailPage />} />
          <Route path="people/:idKey/edit" element={<PersonFormPage />} />
          <Route path="families" element={<FamilyListPage />} />
          <Route path="families/new" element={<FamilyFormPage />} />
          <Route path="families/:idKey" element={<FamilyDetailPage />} />
          <Route path="families/:idKey/edit" element={<FamilyFormPage />} />
          <Route path="groups" element={<GroupsTreePage />} />
          <Route path="groups/tree" element={<Navigate to="/admin/groups" replace />} />
          <Route path="groups/new" element={<GroupFormPage />} />
          <Route path="groups/:idKey" element={<GroupDetailPage />} />
          <Route path="groups/:idKey/edit" element={<GroupFormPage />} />
          <Route path="schedules" element={<ScheduleListPage />} />
          <Route path="schedules/new" element={<ScheduleFormPage />} />
          <Route path="schedules/:idKey" element={<ScheduleDetailPage />} />
          <Route path="schedules/:idKey/edit" element={<ScheduleFormPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
          <Route path="communications" element={<CommunicationsPage />} />
          <Route path="communications/:idKey" element={<CommunicationDetailPage />} />
          <Route path="communications/templates" element={<TemplatesPage />} />
          <Route path="communications/templates/new" element={<TemplateFormPage />} />
          <Route path="communications/templates/:idKey/edit" element={<TemplateFormPage />} />
          <Route path="roster" element={<RosterPage />} />
          <Route path="giving" element={<BatchListPage />} />
          <Route path="giving/new" element={<BatchFormPage />} />
          <Route path="giving/:idKey" element={<BatchDetailPage />} />
          <Route path="giving/statements" element={<StatementsPage />} />
          <Route path="settings" element={<SettingsPage />} />
          <Route path="settings/group-types" element={<GroupTypesPage />} />
          <Route path="settings/import" element={<ImportSettingsPage />} />
          <Route path="settings/campuses" element={<CampusesPage />} />
          <Route path="settings/locations" element={<LocationsPage />} />
          <Route path="settings/audit-logs" element={<AuditLogsPage />} />
          <Route path="exports" element={<DataExportsPage />} />
          <Route path="import/history" element={<ImportHistoryPage />} />
          <Route path="import/people" element={<PeopleImportPage />} />
          <Route path="import/families" element={<FamiliesImportPage />} />
          <Route path="checkin" element={<CheckinConfigPage />} />
        </Route>

        {/* Check-in kiosk — uses its own kiosk auth, not ProtectedRoute */}
        <Route path="/checkin" element={<CheckinPage />} />

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
      </Suspense>
      </ToastProvider>
    </ErrorBoundary>
  );
}

export default App;
