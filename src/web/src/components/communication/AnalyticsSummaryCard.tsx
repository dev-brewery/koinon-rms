/**
 * Analytics Summary Card Component
 * Displays aggregate communication statistics
 */

import React from 'react';
import { useAnalyticsSummary } from '@/features/communication/hooks';

interface AnalyticsSummaryCardProps {
  startDate?: Date;
  endDate?: Date;
  type?: string;
}

export const AnalyticsSummaryCard: React.FC<AnalyticsSummaryCardProps> = ({
  startDate,
  endDate,
  type,
}) => {
  const { data: summary, isLoading, error } = useAnalyticsSummary({
    startDate,
    endDate,
    type,
  });

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-1/3"></div>
          <div className="h-8 bg-gray-200 rounded w-1/2"></div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <div className="text-red-600">
          Error loading summary: {error instanceof Error ? error.message : 'Unknown error'}
        </div>
      </div>
    );
  }

  if (!summary) {
    return null;
  }

  return (
    <div className="bg-white rounded-lg shadow">
      <div className="p-6">
        <h2 className="text-xl font-bold mb-4">Communication Overview</h2>
        <div className="text-sm text-gray-600 mb-6">
          {new Date(summary.startDate).toLocaleDateString()} -{' '}
          {new Date(summary.endDate).toLocaleDateString()}
        </div>

        {/* Key Metrics */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <MetricItem
            label="Total Communications"
            value={summary.totalCommunications}
          />
          <MetricItem
            label="Total Recipients"
            value={summary.totalRecipients}
          />
          <MetricItem
            label="Delivery Rate"
            value={`${summary.deliveryRate.toFixed(1)}%`}
          />
          <MetricItem
            label="Open Rate"
            value={`${summary.openRate.toFixed(1)}%`}
            subtitle="Email only"
          />
        </div>

        {/* Breakdown by Type */}
        <div className="border-t pt-6">
          <h3 className="text-lg font-semibold mb-4">By Type</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <TypeBreakdown
              title="Email"
              stats={summary.byType.email}
              showEngagement
            />
            <TypeBreakdown
              title="SMS"
              stats={summary.byType.sms}
              showEngagement={false}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

interface MetricItemProps {
  label: string;
  value: string | number;
  subtitle?: string;
}

const MetricItem: React.FC<MetricItemProps> = ({ label, value, subtitle }) => (
  <div>
    <div className="text-sm text-gray-600">{label}</div>
    <div className="text-2xl font-bold text-gray-900">{value}</div>
    {subtitle && <div className="text-xs text-gray-500">{subtitle}</div>}
  </div>
);

interface TypeBreakdownProps {
  title: string;
  stats: {
    count: number;
    recipients: number;
    delivered: number;
    opened: number;
    clicked: number;
  };
  showEngagement: boolean;
}

const TypeBreakdown: React.FC<TypeBreakdownProps> = ({
  title,
  stats,
  showEngagement,
}) => {
  const deliveryRate =
    stats.recipients > 0 ? (stats.delivered / stats.recipients) * 100 : 0;
  const openRate =
    stats.delivered > 0 ? (stats.opened / stats.delivered) * 100 : 0;
  const clickRate =
    stats.delivered > 0 ? (stats.clicked / stats.delivered) * 100 : 0;

  return (
    <div className="bg-gray-50 rounded-lg p-4">
      <h4 className="font-semibold mb-3">{title}</h4>
      <dl className="space-y-2 text-sm">
        <div className="flex justify-between">
          <dt className="text-gray-600">Communications:</dt>
          <dd className="font-medium">{stats.count}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-gray-600">Recipients:</dt>
          <dd className="font-medium">{stats.recipients}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-gray-600">Delivered:</dt>
          <dd className="font-medium">
            {stats.delivered} ({deliveryRate.toFixed(1)}%)
          </dd>
        </div>
        {showEngagement && (
          <>
            <div className="flex justify-between">
              <dt className="text-gray-600">Opened:</dt>
              <dd className="font-medium">
                {stats.opened} ({openRate.toFixed(1)}%)
              </dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-600">Clicked:</dt>
              <dd className="font-medium">
                {stats.clicked} ({clickRate.toFixed(1)}%)
              </dd>
            </div>
          </>
        )}
      </dl>
    </div>
  );
};
