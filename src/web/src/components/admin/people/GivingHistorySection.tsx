import { Link } from 'react-router-dom';
import { usePersonGivingSummary } from '@/hooks/usePeople';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import type { RecentContributionDto } from '@/services/api/types';

interface GivingHistorySectionProps {
  personIdKey: string;
}

const usdFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
});

function formatDate(isoString: string): string {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(isoString));
}

function GivingSummarySkeleton() {
  return (
    <div className="space-y-4" role="status" aria-label="Loading giving history">
      <div className="flex gap-8">
        <div className="space-y-1">
          <Skeleton variant="text" height={14} width={80} />
          <Skeleton variant="text" height={28} width={120} />
        </div>
        <div className="space-y-1">
          <Skeleton variant="text" height={14} width={100} />
          <Skeleton variant="text" height={20} width={100} />
        </div>
      </div>
      <div className="space-y-2 mt-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex gap-4 py-3 border-b border-gray-100">
            <Skeleton variant="text" height={16} width="20%" />
            <Skeleton variant="text" height={16} width="15%" />
            <Skeleton variant="text" height={16} width="30%" />
            <Skeleton variant="text" height={16} width="20%" />
          </div>
        ))}
      </div>
    </div>
  );
}

interface ContributionsTableProps {
  contributions: RecentContributionDto[];
}

function ContributionsTable({ contributions }: ContributionsTableProps) {
  return (
    <div className="overflow-x-auto mt-4">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-200 text-left">
            <th className="pb-2 pr-4 font-medium text-gray-600">Date</th>
            <th className="pb-2 pr-4 font-medium text-gray-600">Amount</th>
            <th className="pb-2 pr-4 font-medium text-gray-600">Fund</th>
            <th className="pb-2 font-medium text-gray-600">Type</th>
          </tr>
        </thead>
        <tbody>
          {contributions.map((contribution) => (
            <tr
              key={contribution.idKey}
              className="border-b border-gray-100 hover:bg-gray-50"
            >
              <td className="py-3 pr-4 text-gray-900">
                {formatDate(contribution.transactionDateTime)}
              </td>
              <td className="py-3 pr-4 text-gray-900 font-medium tabular-nums">
                {usdFormatter.format(contribution.amount)}
              </td>
              <td className="py-3 pr-4 text-gray-700">{contribution.fundName}</td>
              <td className="py-3 text-gray-500">
                {contribution.transactionType ?? (
                  <span className="text-gray-400">—</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function GivingHistorySection({ personIdKey }: GivingHistorySectionProps) {
  const { data: summary, isLoading, isError } = usePersonGivingSummary(personIdKey);

  return (
    <section className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-900">Giving History</h2>
        <Link
          to="/admin/giving/statements"
          className="text-sm text-primary-600 hover:text-primary-700"
        >
          View statements
        </Link>
      </div>

      {isLoading && <GivingSummarySkeleton />}

      {isError && (
        <EmptyState
          title="Failed to load giving history"
          description="There was a problem loading this person's giving records. Please try again."
        />
      )}

      {!isLoading && !isError && summary !== undefined && (
        <>
          {/* Summary stats */}
          <div className="flex flex-wrap gap-8 mb-2">
            <div>
              <p className="text-sm font-medium text-gray-500">
                Year-to-Date Total
              </p>
              <p className="text-2xl font-bold text-gray-900 tabular-nums">
                {usdFormatter.format(summary.yearToDateTotal)}
              </p>
            </div>
            <div>
              <p className="text-sm font-medium text-gray-500">
                Last Contribution
              </p>
              <p className="text-base text-gray-900">
                {summary.lastContributionDate
                  ? formatDate(summary.lastContributionDate)
                  : <span className="text-gray-400">None on record</span>}
              </p>
            </div>
          </div>

          {/* Recent contributions table */}
          {summary.recentContributions.length === 0 ? (
            <EmptyState
              title="No giving history found"
              description="This person has no contribution records."
            />
          ) : (
            <ContributionsTable contributions={summary.recentContributions} />
          )}
        </>
      )}
    </section>
  );
}
