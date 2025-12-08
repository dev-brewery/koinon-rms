/**
 * Breadcrumb Navigation
 * Shows hierarchical navigation path
 */

import { Link, useLocation } from 'react-router-dom';

export interface BreadcrumbItem {
  label: string;
  path?: string;
}

export interface BreadcrumbProps {
  items?: BreadcrumbItem[];
}

/**
 * Map of paths to readable labels
 */
const pathLabels: Record<string, string> = {
  admin: 'Admin',
  dashboard: 'Dashboard',
  people: 'People',
  families: 'Families',
  groups: 'Groups',
  schedules: 'Schedules',
  settings: 'Settings',
};

/**
 * Auto-generate breadcrumb items from current path
 */
function generateBreadcrumbsFromPath(pathname: string): BreadcrumbItem[] {
  const segments = pathname.split('/').filter(Boolean);
  const breadcrumbs: BreadcrumbItem[] = [];

  let currentPath = '';
  segments.forEach((segment, index) => {
    currentPath += `/${segment}`;
    const label = pathLabels[segment] || segment;

    // Don't link the last item (current page)
    const isLast = index === segments.length - 1;

    breadcrumbs.push({
      label,
      path: isLast ? undefined : currentPath,
    });
  });

  return breadcrumbs;
}

export function Breadcrumb({ items }: BreadcrumbProps) {
  const location = useLocation();
  const breadcrumbItems = items || generateBreadcrumbsFromPath(location.pathname);

  // Don't show breadcrumbs if only one item (top level)
  if (breadcrumbItems.length <= 1) {
    return null;
  }

  return (
    <nav className="flex items-center space-x-2 text-sm text-gray-600 px-4 py-3 bg-gray-50 border-b border-gray-200">
      <Link
        to="/"
        aria-label="Home"
        className="hover:text-gray-900 transition-colors"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
        </svg>
      </Link>

      {breadcrumbItems.map((item) => (
        <div key={item.path || item.label} className="flex items-center space-x-2">
          <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>

          {item.path ? (
            <Link
              to={item.path}
              className="hover:text-gray-900 transition-colors"
            >
              {item.label}
            </Link>
          ) : (
            <span className="text-gray-900 font-medium">{item.label}</span>
          )}
        </div>
      ))}
    </nav>
  );
}
