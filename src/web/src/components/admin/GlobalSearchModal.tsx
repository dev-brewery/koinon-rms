/**
 * GlobalSearchModal Component
 * Global search modal with keyboard shortcuts, category grouping, and recent searches
 */

import { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useGlobalSearch } from '@/hooks/useGlobalSearch';
import type { GlobalSearchResult } from '@/types/search';
import { cn } from '@/lib/utils';

interface GlobalSearchModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const RESULTS_PER_CATEGORY = 5;

const CATEGORY_ICONS: Record<string, string> = {
  People: 'fa fa-user',
  Families: 'fa fa-users',
  Groups: 'fa fa-user-friends',
};

const CATEGORY_ROUTES: Record<string, string> = {
  People: '/admin/people',
  Families: '/admin/families',
  Groups: '/admin/groups',
};

export function GlobalSearchModal({ isOpen, onClose }: GlobalSearchModalProps) {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  const [selectedIndex, setSelectedIndex] = useState(0);
  
  const {
    query,
    setQuery,
    results,
    isLoading,
    error,
    recentSearches,
    addToRecentSearches,
    clearRecentSearches,
  } = useGlobalSearch();

  // Auto-focus input when modal opens
  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isOpen]);

  // Reset state when modal closes
  useEffect(() => {
    if (!isOpen) {
      setQuery('');
      setSelectedIndex(0);
    }
  }, [isOpen, setQuery]);

  // Handle escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
    };
  }, [isOpen, onClose]);

  // Group results by category (memoized to prevent recalculation)
  const groupedResults = useMemo(() => {
    return results.reduce((acc, result) => {
      if (!acc[result.category]) {
        acc[result.category] = [];
      }
      acc[result.category].push(result);
      return acc;
    }, {} as Record<string, GlobalSearchResult[]>);
  }, [results]);

  // Flatten results for keyboard navigation (memoized for stable reference)
  const flattenedResults = useMemo(() => {
    const flattened: Array<{ type: 'result' | 'viewAll'; category: string; result?: GlobalSearchResult }> = [];
    Object.entries(groupedResults).forEach(([category, categoryResults]) => {
      categoryResults.slice(0, RESULTS_PER_CATEGORY).forEach((result) => {
        flattened.push({ type: 'result', category, result });
      });
      if (categoryResults.length > RESULTS_PER_CATEGORY) {
        flattened.push({ type: 'viewAll', category });
      }
    });
    return flattened;
  }, [groupedResults]);

  // Navigate to result detail page
  const handleResultClick = useCallback(
    (result: GlobalSearchResult) => {
      const route = `${CATEGORY_ROUTES[result.category]}/${result.idKey}`;
      navigate(route);
      onClose();
    },
    [navigate, onClose]
  );

  // Navigate to search results page filtered by category
  const handleViewAllClick = useCallback(
    (category: string) => {
      const route = `${CATEGORY_ROUTES[category]}?search=${encodeURIComponent(query)}`;
      navigate(route);
      onClose();
    },
    [navigate, onClose, query]
  );

  // Handle keyboard navigation
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (query.length < 2) {
        return; // No navigation for recent searches or empty state
      }

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setSelectedIndex((prev) => (prev + 1) % flattenedResults.length);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setSelectedIndex((prev) => (prev - 1 + flattenedResults.length) % flattenedResults.length);
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const selected = flattenedResults[selectedIndex];
        if (selected) {
          if (selected.type === 'result' && selected.result) {
            handleResultClick(selected.result);
          } else if (selected.type === 'viewAll') {
            handleViewAllClick(selected.category);
          }
        }
      }
    },
    [query, flattenedResults, selectedIndex, handleResultClick, handleViewAllClick]
  );

  // Handle recent search click
  const handleRecentSearchClick = useCallback(
    (searchQuery: string) => {
      setQuery(searchQuery);
      addToRecentSearches(searchQuery);
    },
    [setQuery, addToRecentSearches]
  );

  // Handle clear input
  const handleClearInput = useCallback(() => {
    setQuery('');
    setSelectedIndex(0);
    inputRef.current?.focus();
  }, [setQuery]);

  // Handle backdrop click
  const handleBackdropClick = useCallback(
    (e: React.MouseEvent) => {
      if (e.target === e.currentTarget) {
        onClose();
      }
    },
    [onClose]
  );

  if (!isOpen) {
    return null;
  }

  const showRecentSearches = query.length < 2 && recentSearches.length > 0;
  const showMinimumCharactersMessage = query.length > 0 && query.length < 2;
  const showNoResults = query.length >= 2 && !isLoading && results.length === 0 && !error;
  const showResults = query.length >= 2 && results.length > 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black bg-opacity-50 pt-20"
      onClick={handleBackdropClick}
    >
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[600px] overflow-hidden">
        {/* Search Input */}
        <div className="p-4 border-b border-gray-200">
          <div className="relative">
            <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
              <i className="fa fa-search text-gray-400" />
            </div>
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Search people, families, groups..."
              className="w-full pl-10 pr-10 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
            />
            {query.length > 0 && (
              <button
                type="button"
                onClick={handleClearInput}
                className="absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400 hover:text-gray-600"
              >
                <i className="fa fa-times" />
              </button>
            )}
            {isLoading && (
              <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
                <i className="fa fa-spinner fa-spin text-gray-400" />
              </div>
            )}
          </div>
          
          {/* Keyboard hint */}
          <div className="mt-2 text-xs text-gray-500 flex items-center gap-4">
            <span>
              <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 bg-gray-100 border border-gray-200 rounded">
                ↑↓
              </kbd>{' '}
              to navigate
            </span>
            <span>
              <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 bg-gray-100 border border-gray-200 rounded">
                ↵
              </kbd>{' '}
              to select
            </span>
            <span>
              <kbd className="px-2 py-1 text-xs font-semibold text-gray-800 bg-gray-100 border border-gray-200 rounded">
                Esc
              </kbd>{' '}
              to close
            </span>
          </div>
        </div>

        {/* Results Container */}
        <div className="overflow-y-auto max-h-[440px]">
          {/* Recent Searches */}
          {showRecentSearches && (
            <div className="p-4">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-gray-700">Recent Searches</h3>
                <button
                  type="button"
                  onClick={clearRecentSearches}
                  className="text-xs text-gray-500 hover:text-gray-700"
                >
                  Clear all
                </button>
              </div>
              <div className="space-y-2">
                {recentSearches.map((searchQuery, index) => (
                  <button
                    key={index}
                    type="button"
                    onClick={() => handleRecentSearchClick(searchQuery)}
                    className="w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-gray-50 rounded-lg transition-colors"
                  >
                    <i className="fa fa-clock-rotate-left text-gray-400" />
                    <span className="text-sm text-gray-700">{searchQuery}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Minimum Characters Message */}
          {showMinimumCharactersMessage && (
            <div className="p-8 text-center text-gray-500">
              <i className="fa fa-info-circle text-3xl mb-2" />
              <p className="text-sm">Type at least 2 characters to search</p>
            </div>
          )}

          {/* Error State */}
          {error && (
            <div className="p-8 text-center">
              <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <i className="fa fa-exclamation-circle text-red-500 text-2xl mb-2" />
                <p className="text-sm text-red-800 mb-3">
                  Failed to perform search. Please try again.
                </p>
                <button
                  type="button"
                  onClick={() => setQuery(query)} // Trigger re-fetch
                  className="px-4 py-2 bg-red-600 text-white text-sm rounded-lg hover:bg-red-700"
                >
                  Retry
                </button>
              </div>
            </div>
          )}

          {/* No Results */}
          {showNoResults && (
            <div className="p-8 text-center text-gray-500">
              <i className="fa fa-search text-3xl mb-2" />
              <p className="text-sm">
                No results found for <span className="font-semibold">'{query}'</span>
              </p>
            </div>
          )}

          {/* Search Results */}
          {showResults && (
            <div className="divide-y divide-gray-200">
              {Object.entries(groupedResults).map(([category, categoryResults]) => (
                <div key={category} className="p-4">
                  {/* Category Header */}
                  <div className="flex items-center gap-2 mb-3">
                    <i className={cn(CATEGORY_ICONS[category], 'text-gray-500')} />
                    <h3 className="text-sm font-semibold text-gray-700">
                      {category}
                    </h3>
                    <span className="text-xs text-gray-500">
                      ({categoryResults.length})
                    </span>
                  </div>

                  {/* Category Results */}
                  <div className="space-y-1">
                    {categoryResults.slice(0, RESULTS_PER_CATEGORY).map((result) => {
                      const globalIndex = flattenedResults.findIndex(
                        (item) => item.type === 'result' && item.result?.idKey === result.idKey
                      );
                      const isSelected = globalIndex === selectedIndex;

                      return (
                        <button
                          key={result.idKey}
                          type="button"
                          onClick={() => handleResultClick(result)}
                          className={cn(
                            'w-full flex items-center gap-3 px-3 py-2 text-left rounded-lg transition-colors',
                            isSelected
                              ? 'bg-primary-50 border border-primary-200'
                              : 'hover:bg-gray-50 border border-transparent'
                          )}
                        >
                          {/* Avatar/Image */}
                          {result.imageUrl ? (
                            <img
                              src={result.imageUrl}
                              alt={result.title}
                              className="w-8 h-8 rounded-full object-cover"
                            />
                          ) : (
                            <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center">
                              <i className={cn(CATEGORY_ICONS[category], 'text-gray-500 text-sm')} />
                            </div>
                          )}

                          {/* Text */}
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium text-gray-900 truncate">
                              {result.title}
                            </p>
                            {result.subtitle && (
                              <p className="text-xs text-gray-500 truncate">
                                {result.subtitle}
                              </p>
                            )}
                          </div>

                          {/* Selected Indicator */}
                          {isSelected && (
                            <i className="fa fa-arrow-right text-primary-600" />
                          )}
                        </button>
                      );
                    })}

                    {/* View All Link */}
                    {categoryResults.length > RESULTS_PER_CATEGORY && (
                      <button
                        type="button"
                        onClick={() => handleViewAllClick(category)}
                        className={cn(
                          'w-full px-3 py-2 text-left text-sm text-primary-600 hover:text-primary-700 hover:bg-primary-50 rounded-lg transition-colors',
                          flattenedResults.findIndex(
                            (item) => item.type === 'viewAll' && item.category === category
                          ) === selectedIndex && 'bg-primary-50'
                        )}
                      >
                        View all {categoryResults.length} {category.toLowerCase()} results →
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
