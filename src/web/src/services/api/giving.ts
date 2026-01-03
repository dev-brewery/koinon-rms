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
  BatchFilterParams,
  BatchListResponse,
  ContributionStatementDto,
  GenerateStatementRequest,
  StatementPreviewDto,
  EligiblePersonDto,
} from '@/types/giving';

// Re-export for backward compatibility
export type { BatchFilterParams, BatchListResponse };

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

// ============================================================================
// Contribution Statements
// ============================================================================

/**
 * Gets paginated list of contribution statements.
 * Requires authentication
 */
export async function getStatements(
  page = 1,
  pageSize = 25
): Promise<{ data: ContributionStatementDto[]; meta: PaginationMeta }> {
  const params = new URLSearchParams();
  params.set('page', page.toString());
  params.set('pageSize', pageSize.toString());

  const url = `/giving/statements?${params.toString()}`;
  const response = await get<{ data: ContributionStatementDto[]; meta: PaginationMeta }>(url);
  return {
    data: response.data,
    meta: response.meta,
  };
}

/**
 * Gets a single contribution statement by IdKey.
 * Requires authentication
 */
export async function getStatement(idKey: string): Promise<ContributionStatementDto> {
  const response = await get<{ data: ContributionStatementDto }>(`/giving/statements/${idKey}`);
  return response.data;
}

/**
 * Previews a statement before generation.
 * Requires authentication
 */
export async function previewStatement(
  request: GenerateStatementRequest
): Promise<StatementPreviewDto> {
  const response = await post<{ data: StatementPreviewDto }>(
    '/giving/statements/preview',
    request
  );
  return response.data;
}

/**
 * Generates a new contribution statement.
 * Requires authentication
 */
export async function generateStatement(
  request: GenerateStatementRequest
): Promise<ContributionStatementDto> {
  // POST returns 201 Created with body directly (not wrapped in data)
  return post<ContributionStatementDto>('/giving/statements', request);
}

/**
 * Downloads statement PDF.
 * Requires authentication
 */
export async function downloadStatementPdf(idKey: string): Promise<Blob> {
  const response = await fetch(`/api/v1/giving/statements/${idKey}/pdf`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${localStorage.getItem('token')}`,
    },
  });

  if (!response.ok) {
    throw new Error('Failed to download statement PDF');
  }

  return response.blob();
}

/**
 * Gets eligible people for statement generation.
 * Requires authentication
 */
export async function getEligiblePeople(
  startDate: string,
  endDate: string,
  minimumAmount?: number
): Promise<EligiblePersonDto[]> {
  const params = new URLSearchParams();
  params.set('startDate', startDate);
  params.set('endDate', endDate);
  if (minimumAmount !== undefined) {
    params.set('minimumAmount', minimumAmount.toString());
  }

  const url = `/giving/statements/eligible?${params.toString()}`;
  const response = await get<{ data: EligiblePersonDto[] }>(url);
  return response.data;
}
