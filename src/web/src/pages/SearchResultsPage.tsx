/**
 * SearchResultsPage
 * Full-page search results with pagination and category filtering
 */

import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useGlobalSearch } from '@/hooks/useGlobalSearch';
import type { GlobalSearchResult } from '@/types/search';
import { cn } from '@/lib/utils';

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

const CATEGORIES = ['All', 'People', 'Families', 'Groups'] as const;

export function SearchResultsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const queryParam = searchParams.get('q') || '';
  const categoryParam = searchParams.get('category') || 'All';
  const pageParam = parseInt(searchParams.get('page') || '1', 10);

  const {
    query,
    setQuery,
    setCategory,
    page,
    setPage,
    results,
    totalCount,
    categoryCounts,
    isLoading,
    error,
  } = useGlobalSearch();

  // Sync URL params to hook state
  useEffect(() => {
    if (queryParam) {
      setQuery(queryParam);
    }
  }, [queryParam, setQuery]);

  useEffect(() => {
    if (categoryParam !== 'All') {
      setCategory(categoryParam as 'People' | 'Families' | 'Groups');
    } else {
      setCategory(undefined);
    }
  }, [categoryParam, setCategory]);

  useEffect(() => {
    setPage(pageParam);
  }, [pageParam, setPage]);

  // Update URL when search changes
  const handleSearchChange = (newQuery: string) => {
    setQuery(newQuery);
    const params = new URLSearchParams(searchParams);
    if (newQuery) {
      params.set('q', newQuery);
    } else {
      params.delete('q');
    }
    params.delete('page');
    setSearchParams(params);
  };

  const handleCategoryChange = (newCategory: string) => {
    const params = new URLSearchParams(searchParams);
    if (newCategory !== 'All') {
      params.set('category', newCategory);
    } else {
      params.delete('category');
    }
    params.delete('page');
    setSearchParams(params);
  };

  const handlePageChange = (newPage: number) => {
    const params = new URLSearchParams(searchParams);
    params.set('page', newPage.toString());
    setSearchParams(params);
  };

  const handleResultClick = (result: GlobalSearchResult) => {
    const route = `${CATEGORY_ROUTES[result.category]}/${result.idKey}`;
    navigate(route);
  };

  const totalPages = Math.ceil(totalCount / 20);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Search Results</h1>
        {query && totalCount > 0 && (
          <p className="text-sm text-gray-500 mt-1">
            Found {totalCount} result{totalCount !== 1 ? 's' : ''} for "{query}"
          </p>
        )}
      </div>

      {/* Search Input */}
      <div className="relative max-w-xl">
        <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
          <i className="fa fa-search text-gray-400" />
        </div>
        <input
          type="text"
          value={query}
          onChange={(e) => handleSearchChange(e.target.value)}
          placeholder="Search people, families, groups..."
          className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
        />
        {isLoading && (
          <div className="absolute inset-y-0 right-0 flex items-center pr-3">
            <i className="fa fa-spinner fa-spin text-gray-400" />
          </div>
        )}
      </div>

      {/* Category Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8" aria-label="Category filter">
          {CATEGORIES.map((cat) => {
            const isActive = cat === categoryParam || (cat === 'All' && !categoryParam);
            const count = cat === 'All'
              ? totalCount
              : (categoryCounts[cat] || 0);

            return (
              <button
                key={cat}
                type="button"
                onClick={() => handleCategoryChange(cat)}
                className={cn(
                  'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors',
                  isActive
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                )}
              >
                {cat}
                {query.length >= 2 && (
                  <span className={cn(
                    'ml-2 py-0.5 px-2 rounded-full text-xs',
                    isActive ? 'bg-primary-100 text-primary-600' : 'bg-gray-100 text-gray-500'
                  )}>
                    {count}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
          <i className="fa fa-exclamation-circle text-red-500 text-3xl mb-3" />
          <p className="text-red-800 mb-4">Failed to load search results. Please try again.</p>
          <button
            type="button"
            onClick={() => setQuery(query)}
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
          >
            Retry
          </button>
        </div>
      )}

      {/* Loading State */}
      {isLoading && query.length >= 2 && (
        <div className="space-y-4">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="animate-pulse flex items-center gap-4 p-4 bg-gray-50 rounded-lg">
              <div className="w-12 h-12 bg-gray-200 rounded-full" />
              <div className="flex-1">
                <div className="h-4 bg-gray-200 rounded w-1/3 mb-2" />
                <div className="h-3 bg-gray-200 rounded w-1/4" />
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Empty Query State */}
      {query.length < 2 && !isLoading && (
        <div className="text-center py-12">
          <i className="fa fa-search text-gray-300 text-5xl mb-4" />
          <p className="text-gray-500">
            {query.length === 0
              ? 'Enter a search term to find people, families, and groups'
              : 'Type at least 2 characters to search'}
          </p>
        </div>
      )}

      {/* No Results State */}
      {query.length >= 2 && !isLoading && !error && results.length === 0 && (
        <div className="text-center py-12">
          <i className="fa fa-search text-gray-300 text-5xl mb-4" />
          <p className="text-gray-900 font-medium mb-2">
            No results found for "{query}"
          </p>
          <p className="text-gray-500 text-sm">
            Try different keywords or check your spelling
          </p>
        </div>
      )}

      {/* Results List */}
      {query.length >= 2 && !isLoading && !error && results.length > 0 && (
        <div className="space-y-2">
          {results.map((result) => (
            <button
              key={`${result.category}-${result.idKey}`}
              type="button"
              onClick={() => handleResultClick(result)}
              className="w-full flex items-center gap-4 p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 hover:border-gray-300 transition-colors text-left"
            >
              {/* Avatar/Image */}
              {result.imageUrl ? (
                <img
                  src={result.imageUrl}
                  alt={result.title}
                  className="w-12 h-12 rounded-full object-cover"
                />
              ) : (
                <div className="w-12 h-12 rounded-full bg-gray-100 flex items-center justify-center">
                  <i className={cn(CATEGORY_ICONS[result.category], 'text-gray-500 text-lg')} />
                </div>
              )}

              {/* Content */}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 truncate">
                  {result.title}
                </p>
                {result.subtitle && (
                  <p className="text-sm text-gray-500 truncate">
                    {result.subtitle}
                  </p>
                )}
              </div>

              {/* Category Badge */}
              <span className="px-2 py-1 text-xs font-medium text-gray-600 bg-gray-100 rounded-full">
                {result.category}
              </span>

              {/* Arrow */}
              <i className="fa fa-chevron-right text-gray-400" />
            </button>
          ))}
        </div>
      )}

      {/* Pagination */}
      {query.length >= 2 && !isLoading && !error && totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-gray-200 pt-4">
          <div className="text-sm text-gray-500">
            Showing {((page - 1) * 20) + 1} to {Math.min(page * 20, totalCount)} of {totalCount} results
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => handlePageChange(page - 1)}
              disabled={page <= 1}
              className={cn(
                'px-3 py-2 text-sm font-medium rounded-lg transition-colors',
                page <= 1
                  ? 'text-gray-300 cursor-not-allowed'
                  : 'text-gray-700 hover:bg-gray-100'
              )}
            >
              <i className="fa fa-chevron-left mr-1" />
              Previous
            </button>

            {/* Page Numbers */}
            <div className="flex items-center gap-1">
              {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                let pageNum: number;
                if (totalPages <= 5) {
                  pageNum = i + 1;
                } else if (page <= 3) {
                  pageNum = i + 1;
                } else if (page >= totalPages - 2) {
                  pageNum = totalPages - 4 + i;
                } else {
                  pageNum = page - 2 + i;
                }

                return (
                  <button
                    key={pageNum}
                    type="button"
                    onClick={() => handlePageChange(pageNum)}
                    className={cn(
                      'w-8 h-8 text-sm font-medium rounded-lg transition-colors',
                      page === pageNum
                        ? 'bg-primary-600 text-white'
                        : 'text-gray-700 hover:bg-gray-100'
                    )}
                  >
                    {pageNum}
                  </button>
                );
              })}
            </div>

            <button
              type="button"
              onClick={() => handlePageChange(page + 1)}
              disabled={page >= totalPages}
              className={cn(
                'px-3 py-2 text-sm font-medium rounded-lg transition-colors',
                page >= totalPages
                  ? 'text-gray-300 cursor-not-allowed'
                  : 'text-gray-700 hover:bg-gray-100'
              )}
            >
              Next
              <i className="fa fa-chevron-right ml-1" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
