/**
 * People management hooks using TanStack Query
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as peopleApi from '@/services/api/people';
import type {
  PersonSearchParams,
  CreatePersonRequest,
  UpdatePersonRequest,
  CreatePersonNoteRequest,
  UpdatePersonNoteRequest,
} from '@/services/api/types';

/**
 * Search for people with filters
 */
export function usePeople(params: PersonSearchParams = {}) {
  return useQuery({
    queryKey: ['people', params],
    queryFn: () => peopleApi.searchPeople(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get a single person by IdKey
 */
export function usePerson(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey],
    queryFn: () => peopleApi.getPersonByIdKey(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Create a new person
 */
export function useCreatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePersonRequest) => peopleApi.createPerson(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Update an existing person
 */
export function useUpdatePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, request }: { idKey: string; request: UpdatePersonRequest }) =>
      peopleApi.updatePerson(idKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Delete (soft-delete) a person
 */
export function useDeletePerson() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (idKey: string) => peopleApi.deletePerson(idKey),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Upload a photo for a person
 */
export function useUploadPersonPhoto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ idKey, file }: { idKey: string; file: File }) =>
      peopleApi.uploadPersonPhoto(idKey, file),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.idKey] });
      queryClient.invalidateQueries({ queryKey: ['people'] });
    },
  });
}

/**
 * Get person's family members
 */
export function usePersonFamily(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey, 'family'],
    queryFn: () => peopleApi.getPersonFamily(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get groups the person belongs to
 */
export function usePersonGroups(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey, 'groups'],
    queryFn: () => peopleApi.getPersonGroups(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get attendance history for a person
 */
export function usePersonAttendance(personIdKey?: string, days = 90) {
  return useQuery({
    queryKey: ['person-attendance', personIdKey, days],
    queryFn: () => peopleApi.getPersonAttendance(personIdKey!, days),
    enabled: !!personIdKey,
  });
}

/**
 * Get giving summary for a person (YTD total, last contribution date, recent contributions)
 */
export function usePersonGivingSummary(idKey?: string) {
  return useQuery({
    queryKey: ['people', idKey, 'giving-summary'],
    queryFn: () => peopleApi.getPersonGivingSummary(idKey!),
    enabled: !!idKey,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Get notes for a person
 */
export function usePersonNotes(personIdKey?: string) {
  return useQuery({
    queryKey: ['people', personIdKey, 'notes'],
    queryFn: () => peopleApi.getPersonNotes(personIdKey!),
    enabled: !!personIdKey,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Create a note for a person
 */
export function useCreatePersonNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      personIdKey,
      request,
    }: {
      personIdKey: string;
      request: CreatePersonNoteRequest;
    }) => peopleApi.createPersonNote(personIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.personIdKey, 'notes'] });
    },
  });
}

/**
 * Update a note for a person
 */
export function useUpdatePersonNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      personIdKey,
      noteIdKey,
      request,
    }: {
      personIdKey: string;
      noteIdKey: string;
      request: UpdatePersonNoteRequest;
    }) => peopleApi.updatePersonNote(personIdKey, noteIdKey, request),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.personIdKey, 'notes'] });
    },
  });
}

/**
 * Delete a note from a person
 */
export function useDeletePersonNote() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      personIdKey,
      noteIdKey,
    }: {
      personIdKey: string;
      noteIdKey: string;
    }) => peopleApi.deletePersonNote(personIdKey, noteIdKey),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['people', variables.personIdKey, 'notes'] });
    },
  });
}
