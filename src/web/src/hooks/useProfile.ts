/**
 * Profile management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as profileApi from '@/services/api/profile';
import type {
  UpdateMyProfileRequest,
  UpdateFamilyMemberRequest,
} from '@/types/profile';

/**
 * Get the current user's profile
 */
export function useMyProfile() {
  return useQuery({
    queryKey: ['profile', 'me'],
    queryFn: () => profileApi.getMyProfile(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Update the current user's profile
 */
export function useUpdateMyProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateMyProfileRequest) => profileApi.updateMyProfile(data),
    onSuccess: () => {
      // Invalidate profile to refetch
      queryClient.invalidateQueries({ queryKey: ['profile', 'me'] });
    },
  });
}

/**
 * Get the current user's family members
 */
export function useMyFamily() {
  return useQuery({
    queryKey: ['profile', 'me', 'family'],
    queryFn: () => profileApi.getMyFamily(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Update a family member's editable fields
 */
export function useUpdateFamilyMember() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ personIdKey, data }: { personIdKey: string; data: UpdateFamilyMemberRequest }) =>
      profileApi.updateFamilyMember(personIdKey, data),
    onSuccess: () => {
      // Invalidate family list to refetch
      queryClient.invalidateQueries({ queryKey: ['profile', 'me', 'family'] });
    },
  });
}

/**
 * Get the current user's involvement (groups and attendance)
 */
export function useMyInvolvement() {
  return useQuery({
    queryKey: ['profile', 'me', 'involvement'],
    queryFn: () => profileApi.getMyInvolvement(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
