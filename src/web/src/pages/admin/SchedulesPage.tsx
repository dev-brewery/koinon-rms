/**
 * Schedules Management Page
 * Main entry point - redirects to list page
 */

import { Navigate } from 'react-router-dom';

export function SchedulesPage() {
  return <Navigate to="/admin/schedules" replace />;
}
