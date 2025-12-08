/**
 * Attendance Trend Chart Component
 * Displays attendance trends over time using Recharts
 */

import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import type { AttendanceTrend } from '@/services/api/analytics';

export interface AttendanceTrendChartProps {
  trends: AttendanceTrend[] | undefined;
  isLoading: boolean;
}

export function AttendanceTrendChart({ trends, isLoading }: AttendanceTrendChartProps) {
  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Attendance Trends</h3>
        <div className="flex items-center justify-center h-80">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <p className="mt-2 text-sm text-gray-500">Loading chart data...</p>
          </div>
        </div>
      </div>
    );
  }

  if (!trends || trends.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Attendance Trends</h3>
        <div className="flex items-center justify-center h-80">
          <div className="text-center text-gray-500">
            <svg
              className="w-16 h-16 mx-auto mb-4 text-gray-300"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
            <p>No attendance data for selected date range</p>
          </div>
        </div>
      </div>
    );
  }

  // Format date for display (e.g., "2024-01-15" -> "Jan 15")
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  // Transform data for chart
  const chartData = trends.map((trend) => ({
    date: formatDate(trend.date),
    Total: trend.count,
    'First-Time': trend.firstTime,
    Returning: trend.returning,
  }));

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Attendance Trends</h3>
      <ResponsiveContainer width="100%" height={400}>
        <LineChart
          data={chartData}
          margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
        >
          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis
            dataKey="date"
            stroke="#6b7280"
            style={{ fontSize: '0.875rem' }}
          />
          <YAxis
            stroke="#6b7280"
            style={{ fontSize: '0.875rem' }}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: '#fff',
              border: '1px solid #e5e7eb',
              borderRadius: '0.5rem',
              boxShadow: '0 1px 3px 0 rgb(0 0 0 / 0.1)',
            }}
          />
          <Legend
            wrapperStyle={{ paddingTop: '1rem' }}
            iconType="line"
          />
          <Line
            type="monotone"
            dataKey="Total"
            stroke="#2563eb"
            strokeWidth={2}
            dot={{ fill: '#2563eb', r: 4 }}
            activeDot={{ r: 6 }}
          />
          <Line
            type="monotone"
            dataKey="First-Time"
            stroke="#7c3aed"
            strokeWidth={2}
            dot={{ fill: '#7c3aed', r: 4 }}
            activeDot={{ r: 6 }}
          />
          <Line
            type="monotone"
            dataKey="Returning"
            stroke="#059669"
            strokeWidth={2}
            dot={{ fill: '#059669', r: 4 }}
            activeDot={{ r: 6 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
