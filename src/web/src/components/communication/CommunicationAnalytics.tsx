/**
 * Communication Analytics Component
 * Displays detailed analytics for a single communication
 */

import React from 'react';
import type { IdKey } from '@/services/api/types';
import { useCommunicationAnalytics } from '@/features/communication/hooks';

interface CommunicationAnalyticsProps {
  communicationIdKey: IdKey;
}

export const CommunicationAnalytics: React.FC<CommunicationAnalyticsProps> = ({
  communicationIdKey,
}) => {
  const { data: analytics, isLoading, error } = useCommunicationAnalytics(communicationIdKey);

  if (isLoading) {
    return <div className="animate-pulse">Loading analytics...</div>;
  }

  if (error) {
    return (
      <div className="text-red-600">
        Error loading analytics: {error instanceof Error ? error.message : 'Unknown error'}
      </div>
    );
  }

  if (!analytics) {
    return <div>No analytics available</div>;
  }

  const isEmail = analytics.communicationType === 'Email';

  return (
    <div className="space-y-6">
      {/* Summary Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <StatCard
          label="Total Recipients"
          value={analytics.totalRecipients}
          color="blue"
        />
        <StatCard
          label="Delivered"
          value={analytics.delivered}
          percentage={analytics.deliveryRate}
          color="green"
        />
        <StatCard
          label="Failed"
          value={analytics.failed}
          color="red"
        />
        {isEmail && (
          <>
            <StatCard
              label="Opened"
              value={analytics.opened}
              percentage={analytics.openRate}
              color="purple"
            />
            <StatCard
              label="Clicked"
              value={analytics.clicked}
              percentage={analytics.clickRate}
              color="indigo"
            />
            <StatCard
              label="Click-Through Rate"
              value={`${analytics.clickThroughRate.toFixed(1)}%`}
              subtitle={`${analytics.clicked} of ${analytics.opened} opens`}
              color="violet"
            />
          </>
        )}
      </div>

      {/* Status Breakdown */}
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-semibold mb-4">Delivery Status</h3>
        <div className="space-y-2">
          <StatusBar
            label="Pending"
            value={analytics.statusBreakdown.pending}
            total={analytics.totalRecipients}
            color="bg-gray-400"
          />
          <StatusBar
            label="Delivered"
            value={analytics.statusBreakdown.delivered}
            total={analytics.totalRecipients}
            color="bg-green-500"
          />
          <StatusBar
            label="Failed"
            value={analytics.statusBreakdown.failed}
            total={analytics.totalRecipients}
            color="bg-red-500"
          />
          {isEmail && (
            <StatusBar
              label="Opened"
              value={analytics.statusBreakdown.opened}
              total={analytics.totalRecipients}
              color="bg-purple-500"
            />
          )}
        </div>
      </div>

      {/* Sent Date */}
      {analytics.sentDateTime && (
        <div className="text-sm text-gray-600">
          Sent: {new Date(analytics.sentDateTime).toLocaleString()}
        </div>
      )}
    </div>
  );
};

interface StatCardProps {
  label: string;
  value: number | string;
  percentage?: number;
  subtitle?: string;
  color: 'blue' | 'green' | 'red' | 'purple' | 'indigo' | 'violet';
}

const StatCard: React.FC<StatCardProps> = ({
  label,
  value,
  percentage,
  subtitle,
  color,
}) => {
  const colorClasses = {
    blue: 'bg-blue-50 text-blue-700',
    green: 'bg-green-50 text-green-700',
    red: 'bg-red-50 text-red-700',
    purple: 'bg-purple-50 text-purple-700',
    indigo: 'bg-indigo-50 text-indigo-700',
    violet: 'bg-violet-50 text-violet-700',
  };

  return (
    <div className={`rounded-lg p-4 ${colorClasses[color]}`}>
      <div className="text-sm font-medium mb-1">{label}</div>
      <div className="text-2xl font-bold">
        {value}
        {percentage !== undefined && (
          <span className="text-sm ml-2">({percentage.toFixed(1)}%)</span>
        )}
      </div>
      {subtitle && <div className="text-xs mt-1">{subtitle}</div>}
    </div>
  );
};

interface StatusBarProps {
  label: string;
  value: number;
  total: number;
  color: string;
}

const StatusBar: React.FC<StatusBarProps> = ({ label, value, total, color }) => {
  const percentage = total > 0 ? (value / total) * 100 : 0;

  return (
    <div>
      <div className="flex justify-between text-sm mb-1">
        <span>{label}</span>
        <span className="font-medium">
          {value} ({percentage.toFixed(1)}%)
        </span>
      </div>
      <div className="w-full bg-gray-200 rounded-full h-2">
        <div
          className={`${color} h-2 rounded-full transition-all duration-300`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
};
