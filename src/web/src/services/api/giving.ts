/**
 * Giving API service
 */

import { get, post, put, del } from './client';
import type { PaginationMeta } from './types';
import type {
  FundDto,
  ContributionBatchDto,
  BatchSummaryDto,
  CreateBatchRequest,
  ContributionDto,
  AddContributionRequest,
  UpdateContributionRequest,
} from '@/types/giving';

/**
 * Filter parameters for batch search
 */
export interface BatchFilterParams {
  /** Filter by status (Open, Closed, Posted) */
  status?: string;
  /** Filter by campus IdKey */
  campusIdKey?: string;
  /** Filter by start date (inclusive) */
  startDate?: string;
  /** Filter by end date (inclusive) */
  endDate?: string;
  /** Page number (1-based) */
  page?: number;
  /** Items per page */
  pageSize?: number;
}

/**
 * Response envelope for paginated batch results
 */
export interface BatchListResponse {
  data: ContributionBatchDto[];
  meta: PaginationMeta;
}

// ============================================================================
// Funds
// ============================================================================

/**
 * Gets all active funds for contribution entry.
 * Requires authentication
 */
export async function getActiveFunds(): Promise<FundDto[]> {
  const response = await get<{ data: FundDto[] }>('/giving/funds');
  return response.data;
}

/**
 * Gets a fund by IdKey.
 * Requires authentication
 */
export async function getFund(idKey: string): Promise<FundDto> {
  const response = await get<{ data: FundDto }>(`/giving/funds/${idKey}`);
  return response.data;
}

// ============================================================================
// Batches
// ============================================================================

/**
 * Gets a paginated list of batches with optional filters.
 * Requires authentication
 */
export async function getBatches(
  filter?: BatchFilterParams
): Promise<BatchListResponse> {
  const params = new URLSearchParams();

  if (filter?.status) {
    params.set('status', filter.status);
  }
  if (filter?.campusIdKey) {
    params.set('campusIdKey', filter.campusIdKey);
  }
  if (filter?.startDate) {
    params.set('startDate', filter.startDate);
  }
  if (filter?.endDate) {
    params.set('endDate', filter.endDate);
  }
  if (filter?.page) {
    params.set('page', filter.page.toString());
  }
  if (filter?.pageSize) {
    params.set('pageSize', filter.pageSize.toString());
  }

  const queryString = params.toString();
  const url = `/giving/batches${queryString ? `?${queryString}` : ''}`;

  const response = await get<{ data: ContributionBatchDto[]; meta: PaginationMeta }>(url);
  return {
    data: response.data,
    meta: response.meta,
  };
}

/**
 * Gets a batch by IdKey.
 * Requires authentication
 */
export async function getBatch(idKey: string): Promise<ContributionBatchDto> {
  const response = await get<{ data: ContributionBatchDto }>(`/giving/batches/${idKey}`);
  return response.data;
}

/**
 * Gets a batch summary with reconciliation status.
 * Requires authentication
 */
export async function getBatchSummary(idKey: string): Promise<BatchSummaryDto> {
  const response = await get<{ data: BatchSummaryDto }>(
    `/giving/batches/${idKey}/summary`
  );
  return response.data;
}

/**
 * Creates a new contribution batch.
 * Requires authentication
 */
export async function createBatch(
  request: CreateBatchRequest
): Promise<ContributionBatchDto> {
  // POST returns 201 Created with body directly (not wrapped in data)
  return post<ContributionBatchDto>('/giving/batches', request);
}

/**
 * Opens a batch for editing.
 * Requires authentication
 */
export async function openBatch(idKey: string): Promise<{ message: string }> {
  return post<{ message: string }>(`/giving/batches/${idKey}/open`, {});
}

/**
 * Closes a batch.
 * Requires authentication
 */
export async function closeBatch(idKey: string): Promise<{ message: string }> {
  return post<{ message: string }>(`/giving/batches/${idKey}/close`, {});
}

// ============================================================================
// Contributions
// ============================================================================

/**
 * Gets all contributions in a batch.
 * Requires authentication
 */
export async function getBatchContributions(
  batchIdKey: string
): Promise<ContributionDto[]> {
  const response = await get<{ data: ContributionDto[] }>(
    `/giving/batches/${batchIdKey}/contributions`
  );
  return response.data;
}

/**
 * Adds a contribution to a batch.
 * Requires authentication
 */
export async function addContribution(
  batchIdKey: string,
  request: AddContributionRequest
): Promise<ContributionDto> {
  // POST returns 201 Created with body directly (not wrapped in data)
  return post<ContributionDto>(
    `/giving/batches/${batchIdKey}/contributions`,
    request
  );
}

/**
 * Gets a contribution by IdKey.
 * Requires authentication
 */
export async function getContribution(idKey: string): Promise<ContributionDto> {
  const response = await get<{ data: ContributionDto }>(
    `/giving/contributions/${idKey}`
  );
  return response.data;
}

/**
 * Updates an existing contribution.
 * Requires authentication
 */
export async function updateContribution(
  idKey: string,
  request: UpdateContributionRequest
): Promise<ContributionDto> {
  const response = await put<{ data: ContributionDto }>(
    `/giving/contributions/${idKey}`,
    request
  );
  return response.data;
}

/**
 * Deletes a contribution.
 * Requires authentication
 */
export async function deleteContribution(idKey: string): Promise<void> {
  await del<void>(`/giving/contributions/${idKey}`);
}
