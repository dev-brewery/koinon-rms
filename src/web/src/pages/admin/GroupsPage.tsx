/**
 * Groups Management Page
 * Main entry point - redirects to tree view
 */

import { Navigate } from 'react-router-dom';

export function GroupsPage() {
  // Redirect to the tree view by default
  return <Navigate to="/admin/groups/tree" replace />;
}
