/**
 * Communications Analytics Page
 * Dashboard for viewing communication statistics and performance
 */

import React, { useState } from 'react';
import { AnalyticsSummaryCard } from '@/components/communication/AnalyticsSummaryCard';

export const CommunicationsAnalyticsPage: React.FC = () => {
  const [dateRange, setDateRange] = useState<{
    start: Date;
    end: Date;
  }>({
    start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
    end: new Date(),
  });

  const [filterType, setFilterType] = useState<string>('');

  const handleDateRangeChange = (range: 'week' | 'month' | 'quarter' | 'year') => {
    const end = new Date();
    let start: Date;

    switch (range) {
      case 'week':
        start = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
        break;
      case 'month':
        start = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
        break;
      case 'quarter':
        start = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000);
        break;
      case 'year':
        start = new Date(Date.now() - 365 * 24 * 60 * 60 * 1000);
        break;
      default:
        start = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    }

    setDateRange({ start, end });
  };

  return (
    <div className="container mx-auto px-4 py-8 max-w-7xl">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Communication Analytics
        </h1>
        <p className="text-gray-600">
          Track the performance and engagement of your email and SMS communications
        </p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4 mb-6">
        <div className="flex flex-wrap gap-4 items-center">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Time Period
            </label>
            <div className="flex gap-2">
              <button
                onClick={() => handleDateRangeChange('week')}
                className="px-3 py-1 text-sm border rounded hover:bg-gray-50"
              >
                Last Week
              </button>
              <button
                onClick={() => handleDateRangeChange('month')}
                className="px-3 py-1 text-sm border rounded hover:bg-gray-50"
              >
                Last Month
              </button>
              <button
                onClick={() => handleDateRangeChange('quarter')}
                className="px-3 py-1 text-sm border rounded hover:bg-gray-50"
              >
                Last Quarter
              </button>
              <button
                onClick={() => handleDateRangeChange('year')}
                className="px-3 py-1 text-sm border rounded hover:bg-gray-50"
              >
                Last Year
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Type
            </label>
            <select
              value={filterType}
              onChange={(e) => setFilterType(e.target.value)}
              className="px-3 py-1 border rounded text-sm"
            >
              <option value="">All Types</option>
              <option value="Email">Email</option>
              <option value="Sms">SMS</option>
            </select>
          </div>

          <div className="ml-auto">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Custom Range
            </label>
            <div className="flex gap-2">
              <input
                type="date"
                value={dateRange.start.toISOString().split('T')[0]}
                onChange={(e) =>
                  setDateRange((prev) => ({
                    ...prev,
                    start: new Date(e.target.value),
                  }))
                }
                className="px-3 py-1 border rounded text-sm"
              />
              <span className="self-center">to</span>
              <input
                type="date"
                value={dateRange.end.toISOString().split('T')[0]}
                onChange={(e) =>
                  setDateRange((prev) => ({
                    ...prev,
                    end: new Date(e.target.value),
                  }))
                }
                className="px-3 py-1 border rounded text-sm"
              />
            </div>
          </div>
        </div>
      </div>

      {/* Summary Card */}
      <AnalyticsSummaryCard
        startDate={dateRange.start}
        endDate={dateRange.end}
        type={filterType || undefined}
      />

      {/* Additional Insights */}
      <div className="mt-6 bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold mb-4">Insights</h3>
        <div className="space-y-2 text-sm text-gray-600">
          <p>
            Use this dashboard to track the effectiveness of your communications.
          </p>
          <ul className="list-disc list-inside space-y-1">
            <li>
              <strong>Delivery Rate:</strong> Percentage of messages successfully delivered
            </li>
            <li>
              <strong>Open Rate:</strong> Percentage of delivered emails that were opened
            </li>
            <li>
              <strong>Click Rate:</strong> Percentage of delivered emails where links were clicked
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
};
