/**
 * React Query hooks for the Security Roles admin UI.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import * as securityRolesApi from '@/services/api/securityRoles';
import type {
  AssignClaimRequest,
  AssignPersonRequest,
  CreateSecurityRoleRequest,
  UpdateSecurityRoleRequest,
} from '@/services/api/securityRoles';

const ROLES_KEY = ['security-roles'] as const;
const CLAIMS_KEY = ['security-claims'] as const;

export function useSecurityRoles() {
  return useQuery({
    queryKey: ROLES_KEY,
    queryFn: () => securityRolesApi.getSecurityRoles(),
    staleTime: 30 * 1000,
  });
}

export function useSecurityRole(idKey?: string) {
  return useQuery({
    queryKey: [...ROLES_KEY, idKey],
    queryFn: () => securityRolesApi.getSecurityRole(idKey!),
    enabled: !!idKey,
    staleTime: 15 * 1000,
  });
}

export function useSecurityClaims() {
  return useQuery({
    queryKey: CLAIMS_KEY,
    queryFn: () => securityRolesApi.getSecurityClaims(),
    staleTime: 5 * 60 * 1000,
  });
}

export function useCreateSecurityRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateSecurityRoleRequest) =>
      securityRolesApi.createSecurityRole(request),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
    },
  });
}

export function useUpdateSecurityRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdateSecurityRoleRequest }) =>
      securityRolesApi.updateSecurityRole(idKey, request),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      qc.invalidateQueries({ queryKey: [...ROLES_KEY, variables.idKey] });
    },
  });
}

export function useDeleteSecurityRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (idKey: string) => securityRolesApi.deleteSecurityRole(idKey),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
    },
  });
}

export function useAssignRoleClaim() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: AssignClaimRequest }) =>
      securityRolesApi.assignRoleClaim(idKey, request),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      qc.invalidateQueries({ queryKey: [...ROLES_KEY, variables.idKey] });
    },
  });
}

export function useRemoveRoleClaim() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, claimIdKey }: { idKey: string; claimIdKey: string }) =>
      securityRolesApi.removeRoleClaim(idKey, claimIdKey),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      qc.invalidateQueries({ queryKey: [...ROLES_KEY, variables.idKey] });
    },
  });
}

export function useAddRoleMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: AssignPersonRequest }) =>
      securityRolesApi.addRoleMember(idKey, request),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      qc.invalidateQueries({ queryKey: [...ROLES_KEY, variables.idKey] });
    },
  });
}

export function useRemoveRoleMember() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ idKey, personIdKey }: { idKey: string; personIdKey: string }) =>
      securityRolesApi.removeRoleMember(idKey, personIdKey),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      qc.invalidateQueries({ queryKey: [...ROLES_KEY, variables.idKey] });
    },
  });
}
