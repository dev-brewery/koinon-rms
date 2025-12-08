/**
 * Pager Search Component
 * Allows supervisors to search for pagers by number or child name
 */

import { useState, useEffect } from 'react';
import { usePagerSearch } from './hooks';
import type { PagerAssignment } from './api';

export interface PagerSearchProps {
  onSelectPager: (pager: PagerAssignment) => void;
}

/**
 * Debounce hook for search input
 */
function useDebouncedValue<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

/**
 * Normalize pager number input - accepts "P-127" or "127" formats
 */
function normalizePagerInput(input: string): string {
  // Remove "P-" prefix if present (case insensitive)
  return input.replace(/^p-?/i, '');
}

export function PagerSearch({ onSelectPager }: PagerSearchProps) {
  const [searchInput, setSearchInput] = useState('');

  // Debounce search input to avoid excessive API calls
  const debouncedSearch = useDebouncedValue(searchInput, 300);

  // Normalize the search term (remove P- prefix)
  const normalizedSearch = debouncedSearch ? normalizePagerInput(debouncedSearch) : undefined;

  const { data: pagers, isLoading, error } = usePagerSearch(normalizedSearch);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value);
  };

  return (
    <div className="space-y-4">
      {/* Search Input */}
      <div className="relative">
        <input
          type="text"
          placeholder="Search by pager number (127 or P-127) or child name..."
          value={searchInput}
          onChange={handleInputChange}
          className="w-full pl-12 pr-4 py-4 text-lg border-2 border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 min-h-[56px]"
          autoFocus
        />
        <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
          <svg
            className="w-6 h-6 text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-8">
          <div className="flex items-center gap-3 text-gray-600">
            <svg
              className="animate-spin h-6 w-6"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              />
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              />
            </svg>
            <span className="text-lg">Searching...</span>
          </div>
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-800 text-center">
            Failed to search pagers. Please try again.
          </p>
        </div>
      )}

      {/* Results */}
      {!isLoading && !error && pagers && (
        <div className="space-y-2">
          {pagers.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p className="text-lg">
                {searchInput
                  ? 'No pagers found matching your search'
                  : 'No active pagers today'}
              </p>
            </div>
          ) : (
            <>
              <p className="text-sm text-gray-600">
                {pagers.length} pager{pagers.length === 1 ? '' : 's'} found
              </p>
              <div className="space-y-2 max-h-[500px] overflow-y-auto">
                {pagers.map((pager) => (
                  <button
                    key={pager.idKey}
                    onClick={() => onSelectPager(pager)}
                    className="w-full p-4 bg-white border-2 border-gray-200 rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors text-left min-h-[80px]"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <span className="inline-block px-3 py-1 bg-blue-600 text-white font-bold rounded text-lg">
                            P-{pager.pagerNumber}
                          </span>
                          <span className="text-lg font-semibold text-gray-900">
                            {pager.childName}
                          </span>
                        </div>
                        <div className="text-sm text-gray-600">
                          <span className="font-medium">{pager.groupName}</span>
                          {' · '}
                          <span>{pager.locationName}</span>
                        </div>
                        {pager.parentPhoneNumber && (
                          <div className="text-sm text-gray-500 mt-1">
                            <svg
                              className="inline w-4 h-4 mr-1"
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                              aria-hidden="true"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"
                              />
                            </svg>
                            {pager.parentPhoneNumber}
                          </div>
                        )}
                        <div className="text-xs text-gray-500 mt-1">
                          Checked in at{' '}
                          {new Date(pager.checkedInAt).toLocaleTimeString([], {
                            hour: '2-digit',
                            minute: '2-digit',
                          })}
                        </div>
                      </div>
                      <div className="flex flex-col items-end gap-2">
                        {pager.messagesSentCount > 0 && (
                          <span
                            className={`inline-block px-2 py-1 rounded text-xs font-semibold ${
                              pager.messagesSentCount >= 2
                                ? 'bg-orange-100 text-orange-800'
                                : 'bg-gray-100 text-gray-800'
                            }`}
                          >
                            {pager.messagesSentCount} message
                            {pager.messagesSentCount === 1 ? '' : 's'} sent
                          </span>
                        )}
                        {!pager.parentPhoneNumber && (
                          <span className="inline-block px-2 py-1 bg-yellow-100 text-yellow-800 rounded text-xs font-semibold">
                            No phone number
                          </span>
                        )}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}
