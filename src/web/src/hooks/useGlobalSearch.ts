/**
 * Global search hook with debouncing and recent searches
 */

import { useState, useCallback, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useDebounce } from './useDebounce';
import * as searchApi from '@/services/api/search';
import type { GlobalSearchResult } from '@/types/search';

const RECENT_SEARCHES_KEY = 'koinon:recent-searches';
const MAX_RECENT_SEARCHES = 5;
const MIN_QUERY_LENGTH = 2;

/**
 * Get recent searches from localStorage
 */
function getRecentSearches(): string[] {
  try {
    const stored = localStorage.getItem(RECENT_SEARCHES_KEY);
    if (!stored) return [];
    
    const parsed = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed : [];
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to load recent searches:', error);
    }
    return [];
  }
}

/**
 * Save recent searches to localStorage
 */
function saveRecentSearches(searches: string[]): void {
  try {
    localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(searches));
  } catch (error) {
    if (import.meta.env.DEV) {
      console.error('Failed to save recent searches:', error);
    }
  }
}

/**
 * Add a search query to recent searches
 */
function addToRecentSearches(query: string): string[] {
  const trimmedQuery = query.trim();
  if (!trimmedQuery || trimmedQuery.length < MIN_QUERY_LENGTH) {
    return getRecentSearches();
  }

  const current = getRecentSearches();
  
  // Remove if already exists (to move to front)
  const filtered = current.filter((q) => q.toLowerCase() !== trimmedQuery.toLowerCase());
  
  // Add to front
  const updated = [trimmedQuery, ...filtered].slice(0, MAX_RECENT_SEARCHES);
  
  saveRecentSearches(updated);
  return updated;
}

/**
 * Global search hook with debouncing, recent searches, category filtering, and pagination
 */
export function useGlobalSearch() {
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState<'People' | 'Families' | 'Groups' | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [recentSearches, setRecentSearches] = useState<string[]>(() => getRecentSearches());

  // Debounce the query to reduce API calls
  const debouncedQuery = useDebounce(query, 300);

  // Only search if query is long enough
  const shouldSearch = debouncedQuery.trim().length >= MIN_QUERY_LENGTH;

  // Reset page when query or category changes
  useEffect(() => {
    setPage(1);
  }, [debouncedQuery, category]);

  // Perform the search query
  const {
    data,
    isLoading,
    error,
    isFetching,
  } = useQuery({
    queryKey: ['search', debouncedQuery, category, page],
    queryFn: () =>
      searchApi.globalSearch({
        query: debouncedQuery,
        category: category,
        pageNumber: page,
        pageSize: 20,
      }),
    enabled: shouldSearch,
    staleTime: 30 * 1000, // 30 seconds
    gcTime: 5 * 60 * 1000, // 5 minutes (formerly cacheTime)
  });

  // Add successful search to recent searches
  useEffect(() => {
    if (data && debouncedQuery.trim()) {
      setRecentSearches(addToRecentSearches(debouncedQuery));
    }
  }, [data, debouncedQuery]);

  // Clear recent searches
  const clearRecentSearches = useCallback(() => {
    localStorage.removeItem(RECENT_SEARCHES_KEY);
    setRecentSearches([]);
  }, []);

  // Manual add to recent searches (for clicking on a recent search)
  const addToRecent = useCallback((searchQuery: string) => {
    setRecentSearches(addToRecentSearches(searchQuery));
  }, []);

  return {
    // Search state
    query,
    setQuery,

    // Results
    results: (data?.results || []) as GlobalSearchResult[],
    totalCount: data?.totalCount || 0,
    categoryCounts: data?.categoryCounts || {},

    // Pagination
    page,
    setPage,
    pageSize: data?.pageSize || 20,

    // Loading/error state
    isLoading: isLoading || isFetching,
    error: error as Error | null,

    // Category filter
    category,
    setCategory,

    // Recent searches
    recentSearches,
    addToRecentSearches: addToRecent,
    clearRecentSearches,
  };
}
