/**
 * Giving and Financial TypeScript types
 * Maps C# DTOs from Financial/Contribution domain
 */

import type { IdKey, DateTime } from '@/services/api/types';

// ============================================================================
// Enums
// ============================================================================

/**
 * Status of a contribution batch
 * Maps to BatchStatus enum in C#
 */
export enum BatchStatus {
  Open = 0,
  Closed = 1,
  Posted = 2,
}

// ============================================================================
// Fund Types
// ============================================================================

/**
 * Fund for financial contributions
 */
export interface FundDto {
  idKey: IdKey;
  name: string;
  publicName?: string;
  isActive: boolean;
  isPublic: boolean;
}

// ============================================================================
// Contribution Batch Types
// ============================================================================

/**
 * Contribution batch for grouping multiple contributions
 */
export interface ContributionBatchDto {
  idKey: IdKey;
  name: string;
  batchDate: DateTime;
  status: string;
  controlAmount?: number;
  controlItemCount?: number;
  campusIdKey?: string;
  note?: string;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * Summary information for a contribution batch
 * Includes variance calculations and balancing status
 */
export interface BatchSummaryDto {
  idKey: IdKey;
  name: string;
  status: string;
  controlAmount?: number;
  controlItemCount?: number;
  actualAmount: number;
  contributionCount: number;
  itemCountVariance?: number;
  variance: number;
  isBalanced: boolean;
}

/**
 * Request to create a new contribution batch
 */
export interface CreateBatchRequest {
  name: string;
  batchDate: DateTime;
  controlAmount?: number;
  controlItemCount?: number;
  campusIdKey?: string;
  note?: string;
}

// ============================================================================
// Contribution Types
// ============================================================================

/**
 * Financial contribution with one or more fund details
 */
export interface ContributionDto {
  idKey: IdKey;
  personIdKey?: string;
  personName?: string;
  batchIdKey?: string;
  transactionDateTime: DateTime;
  transactionCode?: string;
  transactionTypeValueIdKey: string;
  sourceTypeValueIdKey: string;
  summary?: string;
  campusIdKey?: string;
  details: ContributionDetailDto[];
  totalAmount: number;
}

/**
 * Individual fund detail within a contribution
 */
export interface ContributionDetailDto {
  idKey: IdKey;
  fundIdKey: string;
  fundName: string;
  amount: number;
  summary?: string;
}

/**
 * Request to add a new contribution
 */
export interface AddContributionRequest {
  personIdKey?: string;
  transactionDateTime: DateTime;
  transactionCode?: string;
  transactionTypeValueIdKey: string;
  details: ContributionDetailRequest[];
  summary?: string;
}

/**
 * Request to update an existing contribution
 */
export interface UpdateContributionRequest {
  personIdKey?: string;
  transactionDateTime: DateTime;
  transactionCode?: string;
  transactionTypeValueIdKey: string;
  details: ContributionDetailRequest[];
  summary?: string;
}

/**
 * Fund detail for contribution requests
 */
export interface ContributionDetailRequest {
  fundIdKey: string;
  amount: number;
  summary?: string;
}

// ============================================================================
// Person Lookup Types
// ============================================================================

/**
 * Simplified person information for contributor lookup
 */
export interface PersonLookupDto {
  idKey: IdKey;
  fullName: string;
  email?: string;
}

// ============================================================================
// Contribution Statement Types
// ============================================================================

/**
 * Contribution statement record
 */
export interface ContributionStatementDto {
  idKey: IdKey;
  personIdKey: string;
  personName: string;
  startDate: DateTime;
  endDate: DateTime;
  totalAmount: number;
  contributionCount: number;
  generatedDateTime: DateTime;
}

/**
 * Individual contribution for statement detail
 */
export interface StatementContributionDto {
  date: DateTime;
  fundName: string;
  amount: number;
  checkNumber?: string;
}

/**
 * Request to generate a new statement
 */
export interface GenerateStatementRequest {
  personIdKey: string;
  startDate: DateTime;
  endDate: DateTime;
}

/**
 * Statement preview with full details
 */
export interface StatementPreviewDto {
  personIdKey: string;
  personName: string;
  personAddress: string;
  startDate: DateTime;
  endDate: DateTime;
  totalAmount: number;
  contributions: StatementContributionDto[];
  churchName: string;
  churchAddress: string;
}

/**
 * Eligible person for statement generation
 */
export interface EligiblePersonDto {
  personIdKey: string;
  personName: string;
  totalAmount: number;
  contributionCount: number;
}

// ============================================================================
// Batch Filter and Response Types
// ============================================================================

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
  meta: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}
