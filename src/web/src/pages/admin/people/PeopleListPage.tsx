/**
 * People List Page
 * Main page for viewing and searching people
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePeople } from '@/hooks/usePeople';
import { PersonSearchBar } from '@/components/admin/people/PersonSearchBar';
import { PersonCard } from '@/components/admin/people/PersonCard';
import type { PersonSearchParams } from '@/services/api/types';

export function PeopleListPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [pageSize, setPageSize] = useState(25);
  const [currentPage, setCurrentPage] = useState(1);
  const [connectionStatusId, setConnectionStatusId] = useState<string | undefined>();
  const [recordStatusId, setRecordStatusId] = useState<string | undefined>();
  const [campusId, setCampusId] = useState<string | undefined>();

  const params: PersonSearchParams = {
    q: searchQuery || undefined,
    connectionStatusId,
    recordStatusId,
    campusId,
    page: currentPage,
    pageSize,
    includeInactive: false,
  };

  const { data, isLoading, error } = usePeople(params);

  const people = data?.data || [];
  const meta = data?.meta;

  const handleSearchChange = (value: string) => {
    setSearchQuery(value);
    setCurrentPage(1);
  };

  const handlePageSizeChange = (newSize: number) => {
    setPageSize(newSize);
    setCurrentPage(1);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">People</h1>
          <p className="mt-2 text-gray-600">
            Search and manage person records
          </p>
        </div>
        <Link
          to="/admin/people/new"
          className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          Add Person
        </Link>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="space-y-4">
          {/* Search */}
          <div className="max-w-md">
            <PersonSearchBar
              value={searchQuery}
              onChange={handleSearchChange}
              placeholder="Search by name, email, or phone..."
            />
          </div>

          {/* Filter dropdowns - simplified for now */}
          <div className="flex flex-wrap gap-4">
            <div className="w-48">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Connection Status
              </label>
              <select
                value={connectionStatusId || ''}
                onChange={(e) => {
                  setConnectionStatusId(e.target.value || undefined);
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="">All</option>
              </select>
            </div>

            <div className="w-48">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Record Status
              </label>
              <select
                value={recordStatusId || ''}
                onChange={(e) => {
                  setRecordStatusId(e.target.value || undefined);
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="">All</option>
              </select>
            </div>

            <div className="w-48">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Campus
              </label>
              <select
                value={campusId || ''}
                onChange={(e) => {
                  setCampusId(e.target.value || undefined);
                  setCurrentPage(1);
                }}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              >
                <option value="">All</option>
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Results */}
      <div className="bg-white rounded-lg border border-gray-200">
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
            <p className="mt-4 text-gray-500">Loading people...</p>
          </div>
        ) : error ? (
          <div className="p-12 text-center">
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
            <p className="text-red-600">Failed to load people</p>
            <p className="text-sm text-gray-500 mt-2">
              {error instanceof Error ? error.message : 'Unknown error'}
            </p>
          </div>
        ) : people.length === 0 ? (
          <div className="p-12 text-center">
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
            <p className="text-gray-500">
              {searchQuery ? 'No people found matching your search' : 'No people yet'}
            </p>
            {!searchQuery && (
              <Link
                to="/admin/people/new"
                className="inline-block mt-4 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                Add your first person
              </Link>
            )}
          </div>
        ) : (
          <>
            {/* List */}
            <div className="divide-y divide-gray-200">
              {people.map((person) => (
                <PersonCard key={person.idKey} person={person} />
              ))}
            </div>

            {/* Pagination */}
            {meta && meta.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-gray-700">
                  <span>Show</span>
                  <select
                    value={pageSize}
                    onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                    className="px-2 py-1 border border-gray-300 rounded focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  >
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                    <option value={100}>100</option>
                  </select>
                  <span>
                    of {meta.totalCount} total
                  </span>
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
