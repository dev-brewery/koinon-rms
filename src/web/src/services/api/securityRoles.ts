/**
 * Security Roles admin API service.
 * Provides CRUD for security roles, role claims, and role members,
 * plus a listing of all available claims for the role-claim picker.
 */

import { get, post, put, del } from './client';

const BASE_URL = '/admin/security-roles';
const CLAIMS_URL = '/admin/security-claims';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface SecurityRoleDto {
  idKey: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  claimCount: number;
  memberCount: number;
}

export interface RoleSecurityClaimDto {
  claimIdKey: string;
  claimType: string;
  claimValue: string;
  description: string | null;
  allowOrDeny: 'A' | 'D';
}

export interface PersonSecurityRoleMemberDto {
  personIdKey: string;
  personName: string;
  email: string | null;
  expiresDateTime: string | null;
}

export interface SecurityRoleDetailDto {
  idKey: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  isActive: boolean;
  claims: RoleSecurityClaimDto[];
  members: PersonSecurityRoleMemberDto[];
}

export interface SecurityClaimDto {
  idKey: string;
  claimType: string;
  claimValue: string;
  description: string | null;
}

export interface CreateSecurityRoleRequest {
  name: string;
  description?: string | null;
  isActive?: boolean;
}

export interface UpdateSecurityRoleRequest {
  name: string;
  description?: string | null;
  isActive: boolean;
}

export interface AssignClaimRequest {
  claimIdKey: string;
  allowOrDeny: 'A' | 'D';
}

export interface AssignPersonRequest {
  personIdKey: string;
  expiresDateTime?: string | null;
}

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

export async function getSecurityRoles(): Promise<SecurityRoleDto[]> {
  const response = await get<{ data: SecurityRoleDto[] }>(BASE_URL);
  return response.data;
}

export async function getSecurityRole(idKey: string): Promise<SecurityRoleDetailDto> {
  const response = await get<{ data: SecurityRoleDetailDto }>(`${BASE_URL}/${idKey}`);
  return response.data;
}

export async function getSecurityClaims(): Promise<SecurityClaimDto[]> {
  const response = await get<{ data: SecurityClaimDto[] }>(CLAIMS_URL);
  return response.data;
}

export async function createSecurityRole(
  request: CreateSecurityRoleRequest
): Promise<SecurityRoleDto> {
  const response = await post<{ data: SecurityRoleDto }>(BASE_URL, request);
  return response.data;
}

export async function updateSecurityRole(
  idKey: string,
  request: UpdateSecurityRoleRequest
): Promise<SecurityRoleDto> {
  const response = await put<{ data: SecurityRoleDto }>(`${BASE_URL}/${idKey}`, request);
  return response.data;
}

export async function deleteSecurityRole(idKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}`);
}

export async function assignRoleClaim(
  idKey: string,
  request: AssignClaimRequest
): Promise<void> {
  await post<void>(`${BASE_URL}/${idKey}/claims`, request);
}

export async function removeRoleClaim(idKey: string, claimIdKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}/claims/${claimIdKey}`);
}

export async function addRoleMember(
  idKey: string,
  request: AssignPersonRequest
): Promise<void> {
  await post<void>(`${BASE_URL}/${idKey}/members`, request);
}

export async function removeRoleMember(idKey: string, personIdKey: string): Promise<void> {
  await del<void>(`${BASE_URL}/${idKey}/members/${personIdKey}`);
}
