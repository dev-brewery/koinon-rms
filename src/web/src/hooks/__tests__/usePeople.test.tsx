/**
 * usePeople + friends: TanStack Query wrappers for services/api/people.
 *
 * Checked behaviors:
 *   - usePerson / usePersonFamily / usePersonGroups / usePersonAttendance
 *     / usePersonGivingSummary / usePersonNotes are disabled when no idKey.
 *   - usePersonAttendance default days = 90; respects overrides.
 *   - Mutations (create/update/delete/uploadPhoto + note CRUD) invalidate
 *     the right keys.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/people', () => ({
  searchPeople: vi.fn(),
  getPersonByIdKey: vi.fn(),
  createPerson: vi.fn(),
  updatePerson: vi.fn(),
  deletePerson: vi.fn(),
  uploadPersonPhoto: vi.fn(),
  getPersonFamily: vi.fn(),
  getPersonGroups: vi.fn(),
  getPersonAttendance: vi.fn(),
  getPersonGivingSummary: vi.fn(),
  getPersonNotes: vi.fn(),
  createPersonNote: vi.fn(),
  updatePersonNote: vi.fn(),
  deletePersonNote: vi.fn(),
}));

import * as peopleApi from '@/services/api/people';
import {
  useCreatePerson,
  useCreatePersonNote,
  useDeletePerson,
  useDeletePersonNote,
  usePeople,
  usePerson,
  usePersonAttendance,
  usePersonFamily,
  usePersonGivingSummary,
  usePersonGroups,
  usePersonNotes,
  useUpdatePerson,
  useUpdatePersonNote,
  useUploadPersonPhoto,
} from '../usePeople';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('usePeople', () => {
  it('forwards params', async () => {
    vi.mocked(peopleApi.searchPeople).mockResolvedValueOnce({ data: [] } as never);
    const { result } = renderHookWithQuery(() => usePeople({ q: 'jo' }));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.searchPeople).toHaveBeenCalledWith({ q: 'jo' });
  });
});

describe('usePerson', () => {
  it('is idle without idKey', () => {
    const { result } = renderHookWithQuery(() => usePerson());
    expect(result.current.fetchStatus).toBe('idle');
    expect(peopleApi.getPersonByIdKey).not.toHaveBeenCalled();
  });

  it('fetches when idKey present', async () => {
    vi.mocked(peopleApi.getPersonByIdKey).mockResolvedValueOnce({ idKey: 'p1' } as never);
    const { result } = renderHookWithQuery(() => usePerson('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonByIdKey).toHaveBeenCalledWith('p1');
  });
});

describe('usePersonFamily / usePersonGroups', () => {
  it('both are idle without idKey', () => {
    const fam = renderHookWithQuery(() => usePersonFamily()).result;
    const grp = renderHookWithQuery(() => usePersonGroups()).result;
    expect(fam.current.fetchStatus).toBe('idle');
    expect(grp.current.fetchStatus).toBe('idle');
  });

  it('both fire when idKey provided', async () => {
    vi.mocked(peopleApi.getPersonFamily).mockResolvedValueOnce({ familyMembers: [] } as never);
    vi.mocked(peopleApi.getPersonGroups).mockResolvedValueOnce([] as never);
    const fam = renderHookWithQuery(() => usePersonFamily('p1'));
    const grp = renderHookWithQuery(() => usePersonGroups('p1'));
    await waitFor(() => expect(fam.result.current.isSuccess).toBe(true));
    await waitFor(() => expect(grp.result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonFamily).toHaveBeenCalledWith('p1');
    expect(peopleApi.getPersonGroups).toHaveBeenCalledWith('p1');
  });
});

describe('usePersonAttendance', () => {
  it('defaults days to 90', async () => {
    vi.mocked(peopleApi.getPersonAttendance).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePersonAttendance('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonAttendance).toHaveBeenCalledWith('p1', 90);
  });

  it('respects custom days', async () => {
    vi.mocked(peopleApi.getPersonAttendance).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePersonAttendance('p1', 30));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonAttendance).toHaveBeenCalledWith('p1', 30);
  });

  it('is idle without personIdKey', () => {
    const { result } = renderHookWithQuery(() => usePersonAttendance(undefined));
    expect(result.current.fetchStatus).toBe('idle');
  });
});

describe('usePersonGivingSummary / usePersonNotes', () => {
  it('giving summary fires on idKey', async () => {
    vi.mocked(peopleApi.getPersonGivingSummary).mockResolvedValueOnce({ ytdTotal: 0 } as never);
    const { result } = renderHookWithQuery(() => usePersonGivingSummary('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonGivingSummary).toHaveBeenCalledWith('p1');
  });

  it('person notes idle without idKey', () => {
    const { result } = renderHookWithQuery(() => usePersonNotes());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('person notes fetches on idKey', async () => {
    vi.mocked(peopleApi.getPersonNotes).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => usePersonNotes('p1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(peopleApi.getPersonNotes).toHaveBeenCalledWith('p1');
  });
});

describe('mutations: useCreatePerson / useUpdatePerson / useDeletePerson / useUploadPersonPhoto', () => {
  it('create invalidates people list', async () => {
    vi.mocked(peopleApi.createPerson).mockResolvedValueOnce({ idKey: 'p1' } as never);
    const { result, client } = renderHookWithQuery(() => useCreatePerson());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ firstName: 'A' } as never);
    });
    expect(peopleApi.createPerson).toHaveBeenCalledWith({ firstName: 'A' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people'] });
  });

  it('update invalidates specific + list', async () => {
    vi.mocked(peopleApi.updatePerson).mockResolvedValueOnce({ idKey: 'p1' } as never);
    const { result, client } = renderHookWithQuery(() => useUpdatePerson());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'p1', request: { firstName: 'X' } as never });
    });
    expect(peopleApi.updatePerson).toHaveBeenCalledWith('p1', { firstName: 'X' });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people', 'p1'] });
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people'] });
  });

  it('delete invalidates people list', async () => {
    vi.mocked(peopleApi.deletePerson).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeletePerson());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('p1');
    });
    expect(peopleApi.deletePerson).toHaveBeenCalledWith('p1');
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people'] });
  });

  it('upload photo invalidates', async () => {
    vi.mocked(peopleApi.uploadPersonPhoto).mockResolvedValueOnce({ idKey: 'p1' } as never);
    const { result, client } = renderHookWithQuery(() => useUploadPersonPhoto());
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    const file = new File(['x'], 'p.png', { type: 'image/png' });
    await act(async () => {
      await result.current.mutateAsync({ idKey: 'p1', file });
    });
    expect(peopleApi.uploadPersonPhoto).toHaveBeenCalledWith('p1', file);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['people', 'p1'] });
  });
});

describe('person note mutations', () => {
  it('create / update / delete each invalidate the note list', async () => {
    vi.mocked(peopleApi.createPersonNote).mockResolvedValueOnce({ idKey: 'n1' } as never);
    vi.mocked(peopleApi.updatePersonNote).mockResolvedValueOnce({ idKey: 'n1' } as never);
    vi.mocked(peopleApi.deletePersonNote).mockResolvedValueOnce(undefined as never);

    const create = renderHookWithQuery(() => useCreatePersonNote());
    const update = renderHookWithQuery(() => useUpdatePersonNote());
    const del = renderHookWithQuery(() => useDeletePersonNote());

    const invC = vi.spyOn(create.client, 'invalidateQueries');
    const invU = vi.spyOn(update.client, 'invalidateQueries');
    const invD = vi.spyOn(del.client, 'invalidateQueries');

    await act(async () => {
      await create.result.current.mutateAsync({
        personIdKey: 'p1',
        request: { body: 'hi' } as never,
      });
    });
    expect(peopleApi.createPersonNote).toHaveBeenCalledWith('p1', { body: 'hi' });
    expect(invC).toHaveBeenCalledWith({ queryKey: ['people', 'p1', 'notes'] });

    await act(async () => {
      await update.result.current.mutateAsync({
        personIdKey: 'p1',
        noteIdKey: 'n1',
        request: { body: 'new' } as never,
      });
    });
    expect(peopleApi.updatePersonNote).toHaveBeenCalledWith('p1', 'n1', { body: 'new' });
    expect(invU).toHaveBeenCalledWith({ queryKey: ['people', 'p1', 'notes'] });

    await act(async () => {
      await del.result.current.mutateAsync({ personIdKey: 'p1', noteIdKey: 'n1' });
    });
    expect(peopleApi.deletePersonNote).toHaveBeenCalledWith('p1', 'n1');
    expect(invD).toHaveBeenCalledWith({ queryKey: ['people', 'p1', 'notes'] });
  });
});
