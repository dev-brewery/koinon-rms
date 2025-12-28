/**
 * TypeScript types for testing graph generation.
 * Follows project conventions: proper typing, no any.
 */

export type IdKey = string;
export type DateOnly = string;
export type DateTime = string;
export type Guid = string;

/**
 * Response envelope for paginated results
 */
export interface PagedResult<T> {
  data: T[];
  meta: PaginationMeta;
}

/**
 * Pagination metadata
 */
export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * API response wrapper
 */
export interface ApiResponse<T> {
  data: T;
}

/**
 * Person search parameters
 */
export interface PersonSearchParams {
  q?: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Person summary DTO
 */
export interface PersonSummaryDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  age?: number;
}

/**
 * Person detail DTO
 */
export interface PersonDetailDto {
  idKey: IdKey;
  guid: Guid;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  birthDate?: DateOnly;
  age?: number;
  phoneNumbers: PhoneNumberDto[];
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * Phone number DTO
 */
export interface PhoneNumberDto {
  idKey: IdKey;
  number: string;
  numberFormatted: string;
}

/**
 * Create person request
 */
export interface CreatePersonRequest {
  firstName: string;
  lastName: string;
  email?: string;
}

/**
 * Update person request
 */
export interface UpdatePersonRequest {
  firstName?: string;
  lastName?: string;
  email?: string | null;
}
