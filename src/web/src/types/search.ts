/**
 * Global search types
 */

export interface GlobalSearchResult {
  category: 'People' | 'Families' | 'Groups';
  idKey: string;
  title: string;
  subtitle: string | null;
  imageUrl: string | null;
}

export interface GlobalSearchResponse {
  results: GlobalSearchResult[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  categoryCounts: Record<string, number>;
}

export interface SearchParams {
  query: string;
  category?: 'People' | 'Families' | 'Groups';
  pageNumber?: number;
  pageSize?: number;
}
