/**
 * Admin Dashboard Page
 * Overview page with key metrics and quick actions
 */

import { useDashboardStats } from '@/hooks/useDashboard';
import { StatCard, QuickActions, UpcomingSchedules } from '@/components/admin/dashboard';

export function DashboardPage() {
  const { data: stats, isLoading, error } = useDashboardStats();

  // Calculate trend for check-ins
  const calculateTrend = () => {
    if (!stats) return undefined;

    const today = stats.todayCheckIns;
    const lastWeek = stats.lastWeekCheckIns;

    if (lastWeek === 0) {
      return today > 0 ? { trend: 'up' as const, value: 'New activity' } : undefined;
    }

    const percentChange = ((today - lastWeek) / lastWeek) * 100;
    const formattedPercent = Math.abs(percentChange).toFixed(0);

    if (percentChange > 5) {
      return { trend: 'up' as const, value: `+${formattedPercent}% vs last week` };
    } else if (percentChange < -5) {
      return { trend: 'down' as const, value: `-${formattedPercent}% vs last week` };
    } else {
      return { trend: 'neutral' as const, value: 'Similar to last week' };
    }
  };

  const checkInTrend = calculateTrend();

  // Show error state
  if (error) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
          <p className="mt-2 text-gray-600">Welcome to Koinon RMS admin dashboard</p>
        </div>
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
          <svg
            className="w-12 h-12 text-red-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <p className="text-red-700 font-medium">Failed to load dashboard data</p>
          <p className="text-red-600 text-sm mt-1">
            {error instanceof Error ? error.message : 'An unknown error occurred'}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-2 text-gray-600">Welcome to Koinon RMS admin dashboard</p>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total People"
          value={isLoading ? '--' : stats?.totalPeople.toLocaleString() ?? '--'}
          icon={
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
              />
            </svg>
          }
          color="blue"
        />

        <StatCard
          title="Families"
          value={isLoading ? '--' : stats?.totalFamilies.toLocaleString() ?? '--'}
          icon={
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
              />
            </svg>
          }
          color="green"
        />

        <StatCard
          title="Active Groups"
          value={isLoading ? '--' : stats?.activeGroups.toLocaleString() ?? '--'}
          icon={
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
              />
            </svg>
          }
          color="purple"
        />

        <StatCard
          title="Today"
          value={isLoading ? '--' : stats?.todayCheckIns.toLocaleString() ?? '--'}
          subtitle="Check-ins"
          icon={
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          }
          color="indigo"
          trend={checkInTrend?.trend}
          trendValue={checkInTrend?.value}
        />
      </div>

      {/* Quick actions */}
      <QuickActions />

      {/* Upcoming schedules */}
      <UpcomingSchedules
        schedules={stats?.upcomingSchedules ?? []}
        isLoading={isLoading}
      />
    </div>
  );
}
