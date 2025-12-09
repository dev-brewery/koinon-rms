/**
 * React Query hooks for group meeting RSVPs
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getMeetingRsvps, getMyRsvps, updateMyRsvp, sendRsvpRequests } from './api';
import type { IdKey } from '@/services/api/types';
import type { UpdateRsvpRequest } from './api';

// ============================================================================
// Query Keys
// ============================================================================

export const meetingKeys = {
  all: ['meetings'] as const,
  rsvps: (groupIdKey: IdKey, meetingDate: string) => 
    [...meetingKeys.all, 'rsvps', groupIdKey, meetingDate] as const,
  myRsvps: (startDate?: string, endDate?: string) =>
    [...meetingKeys.all, 'my-rsvps', startDate, endDate] as const,
};

// ============================================================================
// Hooks
// ============================================================================

export function useMeetingRsvps(groupIdKey: IdKey, meetingDate: string) {
  return useQuery({
    queryKey: meetingKeys.rsvps(groupIdKey, meetingDate),
    queryFn: () => getMeetingRsvps(groupIdKey, meetingDate),
  });
}

export function useMyRsvps(startDate?: string, endDate?: string) {
  return useQuery({
    queryKey: meetingKeys.myRsvps(startDate, endDate),
    queryFn: () => getMyRsvps(startDate, endDate),
  });
}

export function useUpdateMyRsvp() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ 
      groupIdKey, 
      meetingDate, 
      request 
    }: { 
      groupIdKey: IdKey; 
      meetingDate: string; 
      request: UpdateRsvpRequest;
    }) => updateMyRsvp(groupIdKey, meetingDate, request),
    onSuccess: (_data, variables) => {
      // Invalidate both the meeting RSVPs and my RSVPs
      queryClient.invalidateQueries({ 
        queryKey: meetingKeys.rsvps(variables.groupIdKey, variables.meetingDate) 
      });
      queryClient.invalidateQueries({ 
        queryKey: meetingKeys.all 
      });
    },
  });
}

export function useSendRsvpRequests() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ 
      groupIdKey, 
      meetingDate 
    }: { 
      groupIdKey: IdKey; 
      meetingDate: string;
    }) => sendRsvpRequests(groupIdKey, meetingDate),
    onSuccess: (_data, variables) => {
      // Invalidate the meeting RSVPs
      queryClient.invalidateQueries({ 
        queryKey: meetingKeys.rsvps(variables.groupIdKey, variables.meetingDate) 
      });
    },
  });
}
