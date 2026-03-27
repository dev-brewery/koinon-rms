/**
 * GivingHistorySection Component
 * Displays year-to-date giving total and recent contribution history for a person
 */

import { Link } from 'react-router-dom';
import { usePersonGivingSummary } from '@/hooks/usePeople';

interface GivingHistorySectionProps {
  personIdKey: string;
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
}

function formatDate(dateTime: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(dateTime));
}

export function GivingHistorySection({ personIdKey }: GivingHistorySectionProps) {
  const { data: summary, isLoading, error } = usePersonGivingSummary(personIdKey);

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Giving History</h2>
        <div className="flex items-center justify-center py-8">
          <div className="w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Giving History</h2>
        <p className="text-red-600 text-sm">Failed to load giving history</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Giving History</h2>
        <Link
          to="/admin/giving/statements"
          className="text-sm text-primary-600 hover:text-primary-700"
        >
          View Statements
        </Link>
      </div>

      {/* Summary row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-6 p-4 bg-gray-50 rounded-lg">
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">
            Year-to-Date Total
          </p>
          <p className="mt-1 text-2xl font-semibold text-gray-900">
            {formatCurrency(summary?.yearToDateTotal ?? 0)}
          </p>
        </div>
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">
            Last Contribution
          </p>
          <p className="mt-1 text-base text-gray-900">
            {summary?.lastContributionDate
              ? formatDate(summary.lastContributionDate)
              : 'None on record'}
          </p>
        </div>
      </div>

      {/* Recent contributions table */}
      {!summary?.recentContributions.length ? (
        <p className="text-sm text-gray-500 text-center py-6">No contributions on record.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200">
                <th className="pb-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                  Date
                </th>
                <th className="pb-2 text-right text-xs font-medium text-gray-500 uppercase tracking-wide">
                  Amount
                </th>
                <th className="pb-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wide pl-4">
                  Fund
                </th>
                <th className="pb-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wide pl-4">
                  Type
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {summary.recentContributions.map((contribution) => (
                <tr key={contribution.idKey}>
                  <td className="py-3 text-gray-900 whitespace-nowrap">
                    {formatDate(contribution.transactionDateTime)}
                  </td>
                  <td className="py-3 text-right text-gray-900 font-medium whitespace-nowrap">
                    {formatCurrency(contribution.amount)}
                  </td>
                  <td className="py-3 text-gray-700 pl-4">{contribution.fundName}</td>
                  <td className="py-3 text-gray-500 pl-4">
                    {contribution.transactionType ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
