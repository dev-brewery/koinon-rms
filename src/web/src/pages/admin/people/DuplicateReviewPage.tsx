/**
 * Duplicate Review Page
 * Review and manage potential duplicate people
 */

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDuplicates, useIgnoreDuplicate } from '@/hooks/usePersonMerge';
import { Loading, EmptyState, ErrorState } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';
import type { DuplicateMatchDto } from '@/types/personMerge';

export function DuplicateReviewPage() {
  const navigate = useNavigate();
  const { success: showSuccess, error: showError } = useToast();
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const { data, isLoading, error, refetch } = useDuplicates(currentPage, pageSize);
  const ignoreMutation = useIgnoreDuplicate();

  const duplicates = data?.data || [];
  const meta = data?.meta;

  const handleCompare = (match: DuplicateMatchDto) => {
    navigate(`/admin/people/compare?person1=${match.person1IdKey}&person2=${match.person2IdKey}`);
  };

  const handleMerge = (match: DuplicateMatchDto) => {
    navigate(`/admin/people/merge?person1=${match.person1IdKey}&person2=${match.person2IdKey}`);
  };

  const handleIgnore = async (match: DuplicateMatchDto) => {
    try {
      await ignoreMutation.mutateAsync({
        person1IdKey: match.person1IdKey,
        person2IdKey: match.person2IdKey,
      });
      showSuccess('Success', 'Duplicate pair ignored');
    } catch (err) {
      showError('Error', 'Failed to ignore duplicate');
    }
  };

  const getMatchScoreColor = (score: number): string => {
    if (score >= 0.9) return 'bg-red-100 text-red-800';
    if (score >= 0.7) return 'bg-orange-100 text-orange-800';
    return 'bg-yellow-100 text-yellow-800';
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Duplicate Review</h1>
          <p className="mt-2 text-gray-600">
            Review and merge potential duplicate people
          </p>
        </div>
        <button
          onClick={() => navigate('/admin/people/merge-history')}
          className="px-4 py-2 text-primary-600 border border-primary-600 rounded-lg hover:bg-primary-50 transition-colors"
        >
          View Merge History
        </button>
      </div>

      {/* Results */}
      <div className="bg-white rounded-lg border border-gray-200">
        {isLoading ? (
          <Loading text="Loading duplicates..." />
        ) : error ? (
          <ErrorState
            title="Failed to load duplicates"
            message={error instanceof Error ? error.message : 'Unknown error'}
            onRetry={() => refetch()}
          />
        ) : duplicates.length === 0 ? (
          <EmptyState
            icon={
              <svg
                className="w-12 h-12 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            }
            title="No duplicates found"
            description="All people records are unique"
          />
        ) : (
          <>
            {/* List */}
            <div className="divide-y divide-gray-200">
              {duplicates.map((match, index) => (
                <div key={index} className="p-6 hover:bg-gray-50 transition-colors">
                  <div className="flex items-start justify-between gap-6">
                    {/* Person 1 */}
                    <div className="flex items-start gap-4 flex-1">
                      {match.person1PhotoUrl ? (
                        <img
                          src={match.person1PhotoUrl}
                          alt={match.person1Name}
                          className="w-16 h-16 rounded-full object-cover"
                        />
                      ) : (
                        <div className="w-16 h-16 rounded-full bg-gray-200 flex items-center justify-center">
                          <span className="text-2xl font-semibold text-gray-500">
                            {match.person1Name.charAt(0)}
                          </span>
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        <h3 className="text-lg font-semibold text-gray-900 truncate">
                          {match.person1Name}
                        </h3>
                        {match.person1Email && (
                          <p className="text-sm text-gray-600 truncate">{match.person1Email}</p>
                        )}
                        {match.person1Phone && (
                          <p className="text-sm text-gray-600">{match.person1Phone}</p>
                        )}
                      </div>
                    </div>

                    {/* Match Score and Reasons */}
                    <div className="flex flex-col items-center gap-2 px-4">
                      <div className={`px-3 py-1 rounded-full text-sm font-semibold ${getMatchScoreColor(match.matchScore)}`}>
                        {Math.round(match.matchScore * 100)}%
                      </div>
                      <div className="flex flex-wrap gap-1 justify-center max-w-xs">
                        {match.matchReasons.map((reason, i) => (
                          <span
                            key={i}
                            className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded"
                          >
                            {reason}
                          </span>
                        ))}
                      </div>
                    </div>

                    {/* Person 2 */}
                    <div className="flex items-start gap-4 flex-1">
                      {match.person2PhotoUrl ? (
                        <img
                          src={match.person2PhotoUrl}
                          alt={match.person2Name}
                          className="w-16 h-16 rounded-full object-cover"
                        />
                      ) : (
                        <div className="w-16 h-16 rounded-full bg-gray-200 flex items-center justify-center">
                          <span className="text-2xl font-semibold text-gray-500">
                            {match.person2Name.charAt(0)}
                          </span>
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        <h3 className="text-lg font-semibold text-gray-900 truncate">
                          {match.person2Name}
                        </h3>
                        {match.person2Email && (
                          <p className="text-sm text-gray-600 truncate">{match.person2Email}</p>
                        )}
                        {match.person2Phone && (
                          <p className="text-sm text-gray-600">{match.person2Phone}</p>
                        )}
                      </div>
                    </div>

                    {/* Actions */}
                    <div className="flex flex-col gap-2">
                      <button
                        onClick={() => handleCompare(match)}
                        className="px-4 py-2 text-sm bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors whitespace-nowrap"
                      >
                        Compare
                      </button>
                      <button
                        onClick={() => handleMerge(match)}
                        className="px-4 py-2 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors whitespace-nowrap"
                      >
                        Merge
                      </button>
                      <button
                        onClick={() => handleIgnore(match)}
                        disabled={ignoreMutation.isPending}
                        className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors whitespace-nowrap disabled:opacity-50"
                      >
                        Not Duplicates
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Pagination */}
            {meta && meta.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <span>Show</span>
                  <select
                    value={pageSize}
                    onChange={(e) => {
                      setPageSize(Number(e.target.value));
                      setCurrentPage(1);
                    }}
                    className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  >
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                    <option value={100}>100</option>
                  </select>
                  <span>of {meta.totalCount} total</span>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                    disabled={currentPage === 1}
                    className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Previous
                  </button>
                  <span className="text-sm text-gray-700">
                    Page {currentPage} of {meta.totalPages}
                  </span>
                  <button
                    onClick={() => setCurrentPage((p) => Math.min(meta.totalPages, p + 1))}
                    disabled={currentPage === meta.totalPages}
                    className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Next
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
