/**
 * Admin Layout
 * Main layout for admin pages with sidebar and header
 */

import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar, Header, Breadcrumb } from '../components/admin';
import type { BreadcrumbItem } from '../components/admin';

export interface AdminLayoutProps {
  breadcrumbs?: BreadcrumbItem[];
}

export function AdminLayout({ breadcrumbs }: AdminLayoutProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  const toggleSidebar = () => setIsSidebarOpen(prev => !prev);
  const closeSidebar = () => setIsSidebarOpen(false);

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Sidebar */}
      <Sidebar isOpen={isSidebarOpen} onClose={closeSidebar} />

      {/* Main content area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Header */}
        <Header onMenuClick={toggleSidebar} />

        {/* Breadcrumb */}
        <Breadcrumb items={breadcrumbs} />

        {/* Page content */}
        <main className="flex-1 overflow-x-hidden overflow-y-auto">
          <div className="container mx-auto px-4 py-6">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
