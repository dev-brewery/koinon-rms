/**
 * GroupFinderPage
 * Public page for browsing and searching available groups
 */

import { useState } from 'react';
import { usePublicGroups } from '@/hooks/usePublicGroups';
import { GroupCard } from '@/components/groups/GroupCard';
import { GroupFilters } from '@/components/groups/GroupFilters';
import type { PublicGroupSearchParams, PublicGroupDto } from '@/services/api/publicGroups';

export function GroupFinderPage() {
  const [filters, setFilters] = useState<PublicGroupSearchParams>({
    pageNumber: 1,
    pageSize: 12,
  });

  const { data, isLoading, error } = usePublicGroups(filters);

  const groups = data?.data || [];
  const totalCount = data?.meta?.totalCount || 0;
  const totalPages = data?.meta?.totalPages || 1;
  const currentPage = filters.pageNumber || 1;

  const handleFiltersChange = (newFilters: PublicGroupSearchParams) => {
    setFilters({ ...newFilters, pageNumber: 1, pageSize: filters.pageSize });
  };

  const handleGroupClick = (group: PublicGroupDto) => {
    // Group detail navigation will be added in future sprint
    void(group);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center">
            <h1 className="text-4xl font-bold text-gray-900 mb-2">
              Find a Group
            </h1>
            <p className="text-lg text-gray-600">
              Connect with others through small groups, Bible studies, and communities
            </p>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="space-y-6">
          {/* Filters */}
          <GroupFilters filters={filters} onFiltersChange={handleFiltersChange} />

          {/* Results */}
          <div>
            {isLoading ? (
              <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
                <div className="inline-block w-12 h-12 border-4 border-gray-200 border-t-blue-600 rounded-full animate-spin" />
                <p className="mt-4 text-gray-500">Loading groups...</p>
              </div>
            ) : error ? (
              <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
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
                <p className="text-red-600">Failed to load groups</p>
                <p className="text-sm text-gray-500 mt-2">
                  {error instanceof Error ? error.message : 'Unknown error'}
                </p>
              </div>
            ) : groups.length === 0 ? (
              <div className="bg-white rounded-lg border border-gray-200 p-12 text-center">
                <svg
                  className="w-12 h-12 text-gray-400 mx-auto mb-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                  />
                </svg>
                <p className="text-gray-500 text-lg mb-2">No groups found</p>
                <p className="text-sm text-gray-400">
                  Try adjusting your filters or check back later
                </p>
              </div>
            ) : (
              <>
                {/* Results Count */}
                <div className="mb-4">
                  <p className="text-sm text-gray-600">
                    Showing {groups.length} of {totalCount} groups
                  </p>
                </div>

                {/* Groups Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {groups.map((group) => (
                    <GroupCard
                      key={group.idKey}
                      group={group}
                      onClick={handleGroupClick}
                    />
                  ))}
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="mt-8 flex flex-col items-center gap-4">
                    <div className="text-sm text-gray-600">
                      Page {currentPage} of {totalPages}
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setFilters({ ...filters, pageNumber: currentPage - 1 })}
                        disabled={currentPage === 1}
                        className="px-4 py-2 border border-gray-300 rounded-lg bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                      >
                        Previous
                      </button>

                      {/* Page Numbers (show up to 5 pages) */}
                      <div className="flex items-center gap-1">
                        {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                          let pageNum: number;
                          if (totalPages <= 5) {
                            pageNum = i + 1;
                          } else if (currentPage <= 3) {
                            pageNum = i + 1;
                          } else if (currentPage >= totalPages - 2) {
                            pageNum = totalPages - 4 + i;
                          } else {
                            pageNum = currentPage - 2 + i;
                          }

                          return (
                            <button
                              key={pageNum}
                              onClick={() => setFilters({ ...filters, pageNumber: pageNum })}
                              className={`w-10 h-10 rounded-lg transition-colors ${
                                currentPage === pageNum
                                  ? 'bg-blue-600 text-white'
                                  : 'bg-white border border-gray-300 hover:bg-gray-50'
                              }`}
                            >
                              {pageNum}
                            </button>
                          );
                        })}
                      </div>

                      <button
                        onClick={() => setFilters({ ...filters, pageNumber: currentPage + 1 })}
                        disabled={currentPage === totalPages}
                        className="px-4 py-2 border border-gray-300 rounded-lg bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
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
      </div>

      {/* Footer */}
      <div className="mt-12 bg-white border-t border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="text-center text-sm text-gray-500">
            <p>
              Questions about joining a group?{' '}
              <span className="text-blue-600 font-medium">contact your church office</span>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
