/**
 * Person Lookup Component
 * Autocomplete search component for selecting a person
 */

import { useState, useEffect } from 'react';
import { usePeople } from '@/hooks/usePeople';
import type { PersonSummaryDto } from '@/services/api/types';

export interface PersonLookupProps {
  value: PersonSummaryDto | null;
  onChange: (person: PersonSummaryDto | null) => void;
  disabled?: boolean;
  placeholder?: string;
}

export function PersonLookup({
  value,
  onChange,
  disabled = false,
  placeholder = 'Search for contributor...',
}: PersonLookupProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [showResults, setShowResults] = useState(false);

  // Debounce search query (300ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(searchQuery);
    }, 300);

    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Fetch people when debounced query has 2+ chars
  const { data, isLoading } = usePeople({
    q: debouncedQuery.length >= 2 ? debouncedQuery : undefined,
    pageSize: 10,
  });

  const people = data?.data || [];

  // Show results when typing and query is 2+ chars
  useEffect(() => {
    setShowResults(searchQuery.length >= 2 && !value);
  }, [searchQuery, value]);

  const handleSelect = (person: PersonSummaryDto) => {
    onChange(person);
    setSearchQuery('');
    setShowResults(false);
  };

  const handleClear = () => {
    onChange(null);
    setSearchQuery('');
    setShowResults(false);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
  };

  // Show selected person card
  if (value) {
    return (
      <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
        <div className="flex items-center gap-3">
          {value.photoUrl ? (
            <img
              src={value.photoUrl}
              alt={value.fullName}
              className="w-10 h-10 rounded-full object-cover"
            />
          ) : (
            <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
              <svg
                className="w-5 h-5 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-gray-900">
              {value.fullName}
            </div>
            {value.email && (
              <div className="text-xs text-gray-600">{value.email}</div>
            )}
          </div>
          {!disabled && (
            <button
              onClick={handleClear}
              className="text-gray-400 hover:text-gray-600"
              aria-label="Clear selection"
            >
              <svg
                className="w-5 h-5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          )}
        </div>
      </div>
    );
  }

  // Show search input
  return (
    <div className="relative">
      <div className="relative">
        <input
          type="text"
          placeholder={placeholder}
          value={searchQuery}
          onChange={handleInputChange}
          disabled={disabled}
          className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <svg
            className="w-5 h-5 text-gray-400"
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

      {/* Dropdown Results */}
      {showResults && (
        <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-60 overflow-y-auto">
          {isLoading ? (
            <div className="p-4 text-center text-gray-500">Searching...</div>
          ) : people.length === 0 ? (
            <div className="p-4 text-center text-gray-500">
              No people found
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {people.map((person) => (
                <button
                  key={person.idKey}
                  onClick={() => handleSelect(person)}
                  className="w-full p-3 hover:bg-gray-50 text-left transition-colors"
                >
                  <div className="flex items-center gap-3">
                    {person.photoUrl ? (
                      <img
                        src={person.photoUrl}
                        alt={person.fullName}
                        className="w-10 h-10 rounded-full object-cover"
                      />
                    ) : (
                      <div className="w-10 h-10 rounded-full bg-gray-200 flex items-center justify-center">
                        <svg
                          className="w-5 h-5 text-gray-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                          aria-hidden="true"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                          />
                        </svg>
                      </div>
                    )}
                    <div className="flex-1 min-w-0">
                      <div className="text-sm font-medium text-gray-900 truncate">
                        {person.fullName}
                      </div>
                      {person.email && (
                        <div className="text-xs text-gray-500 truncate">
                          {person.email}
                        </div>
                      )}
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
