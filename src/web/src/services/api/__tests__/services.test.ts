/**
 * API service wrappers - shared test file
 *
 * These services are thin wrappers around ./client (get/post/put/del). They
 * are the seam between UI hooks and raw HTTP, so it's valuable to lock in:
 *   - exact URL construction (especially query-string encoding)
 *   - that `{ data: X }` envelopes are unwrapped
 *   - request body passthrough
 *
 * We mock ./client once so every service under test hits the same spies.
 * This keeps the file dense with high-signal assertions.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('../client', () => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
  patch: vi.fn(),
  setTokens: vi.fn(),
  clearTokens: vi.fn(),
  getAccessToken: vi.fn(),
  getRefreshToken: vi.fn(),
  ApiClientError: class MockApiClientError extends Error {
    statusCode: number;
    constructor(statusCode: number, error: { message?: string }) {
      super(error?.message || 'err');
      this.statusCode = statusCode;
    }
  },
}));

// Skip the OfflineFamilyCache side effect in checkin.ts
vi.mock('@/services/offline/OfflineFamilyCache', () => ({
  offlineFamilyCache: {
    cacheResults: vi.fn().mockResolvedValue(undefined),
    getCachedResults: vi.fn().mockResolvedValue(null),
  },
}));

import * as client from '../client';

const mockGet = vi.mocked(client.get);
const mockPost = vi.mocked(client.post);
const mockPut = vi.mocked(client.put);
const mockDel = vi.mocked(client.del);

beforeEach(() => {
  mockGet.mockReset();
  mockPost.mockReset();
  mockPut.mockReset();
  mockDel.mockReset();
});

// ===========================================================================
// families.ts
// ===========================================================================
describe('familiesApi', () => {
  it('searchFamilies builds query string from params', async () => {
    const { searchFamilies } = await import('../families');
    mockGet.mockResolvedValueOnce({ data: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });

    await searchFamilies({
      q: 'smith',
      campusId: 'c1',
      includeInactive: true,
      page: 2,
      pageSize: 25,
    });

    const url = mockGet.mock.calls[0][0];
    expect(url).toContain('/families?');
    expect(url).toContain('query=smith');
    expect(url).toContain('campusId=c1');
    expect(url).toContain('includeInactive=true');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
  });

  it('searchFamilies omits empty params', async () => {
    const { searchFamilies } = await import('../families');
    mockGet.mockResolvedValueOnce({ data: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
    await searchFamilies();
    expect(mockGet.mock.calls[0][0]).toBe('/families');
  });

  it('getFamilyByIdKey unwraps envelope', async () => {
    const { getFamilyByIdKey } = await import('../families');
    const fam = { idKey: 'f1' };
    mockGet.mockResolvedValueOnce({ data: fam });
    expect(await getFamilyByIdKey('f1')).toBe(fam);
    expect(mockGet).toHaveBeenCalledWith('/families/f1');
  });

  it('createFamily posts to /families', async () => {
    const { createFamily } = await import('../families');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'new' } });
    const body = { name: 'Smith' } as never;
    const res = await createFamily(body);
    expect(mockPost).toHaveBeenCalledWith('/families', body);
    expect(res.idKey).toBe('new');
  });

  it('updateFamily puts to /families/:idKey', async () => {
    const { updateFamily } = await import('../families');
    mockPut.mockResolvedValueOnce({ data: { idKey: 'f1' } });
    await updateFamily('f1', { name: 'x' } as never);
    expect(mockPut).toHaveBeenCalledWith('/families/f1', { name: 'x' });
  });

  it('addFamilyMember posts to members subresource', async () => {
    const { addFamilyMember } = await import('../families');
    mockPost.mockResolvedValueOnce({ data: { personIdKey: 'p1' } });
    await addFamilyMember('f1', { personIdKey: 'p1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/families/f1/members', { personIdKey: 'p1' });
  });

  it('removeFamilyMember without params omits query string', async () => {
    const { removeFamilyMember } = await import('../families');
    mockDel.mockResolvedValueOnce(undefined);
    await removeFamilyMember('f1', 'p1');
    expect(mockDel).toHaveBeenCalledWith('/families/f1/members/p1');
  });

  it('removeFamilyMember appends removeFromAllGroups=true', async () => {
    const { removeFamilyMember } = await import('../families');
    mockDel.mockResolvedValueOnce(undefined);
    await removeFamilyMember('f1', 'p1', { removeFromAllGroups: true });
    expect(mockDel).toHaveBeenCalledWith(
      '/families/f1/members/p1?removeFromAllGroups=true'
    );
  });

  it('updateFamilyAddress puts to address subresource', async () => {
    const { updateFamilyAddress } = await import('../families');
    mockPut.mockResolvedValueOnce({ data: { street: 'x' } });
    await updateFamilyAddress('f1', { street: 'x' } as never);
    expect(mockPut).toHaveBeenCalledWith('/families/f1/address', { street: 'x' });
  });
});

// ===========================================================================
// people.ts
// ===========================================================================
describe('peopleApi', () => {
  it('searchPeople forwards all documented filters', async () => {
    const { searchPeople } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
    await searchPeople({
      q: 'jo',
      firstName: 'Jo',
      lastName: 'Doe',
      email: 'a@b.com',
      phone: '555',
      recordStatusId: 'rs',
      connectionStatusId: 'cs',
      campusId: 'c',
      includeInactive: true,
      page: 3,
      pageSize: 50,
      sortBy: 'lastName',
      sortDir: 'asc',
    });
    const url = mockGet.mock.calls[0][0];
    for (const token of [
      'query=jo',
      'firstName=Jo',
      'lastName=Doe',
      'email=a%40b.com',
      'phone=555',
      'recordStatusId=rs',
      'connectionStatusId=cs',
      'campusId=c',
      'includeInactive=true',
      'page=3',
      'pageSize=50',
      'sortBy=lastName',
      'sortDir=asc',
    ]) {
      expect(url).toContain(token);
    }
  });

  it('getPersonByIdKey and getPersonFamily unwrap envelopes', async () => {
    const { getPersonByIdKey, getPersonFamily } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'p1' } });
    expect(await getPersonByIdKey('p1')).toEqual({ idKey: 'p1' });

    mockGet.mockResolvedValueOnce({ data: { familyMembers: [] } });
    expect(await getPersonFamily('p1')).toEqual({ familyMembers: [] });
  });

  it('deletePerson DELETEs /people/:idKey', async () => {
    const { deletePerson } = await import('../people');
    mockDel.mockResolvedValueOnce(undefined);
    await deletePerson('p1');
    expect(mockDel).toHaveBeenCalledWith('/people/p1');
  });

  it('getPersonAttendance defaults days to 90', async () => {
    const { getPersonAttendance } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: [] });
    await getPersonAttendance('p1');
    expect(mockGet).toHaveBeenCalledWith('/people/p1/attendance?days=90');
  });

  it('getPersonAttendance respects custom days', async () => {
    const { getPersonAttendance } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: [] });
    await getPersonAttendance('p1', 30);
    expect(mockGet).toHaveBeenCalledWith('/people/p1/attendance?days=30');
  });

  it('getPersonNotes handles flat array response', async () => {
    const { getPersonNotes } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'n1' }] });
    const notes = await getPersonNotes('p1');
    expect(notes).toEqual([{ idKey: 'n1' }]);
  });

  it('getPersonNotes unwraps paged response format', async () => {
    const { getPersonNotes } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: { data: [{ idKey: 'n2' }], meta: {} } });
    const notes = await getPersonNotes('p1');
    expect(notes).toEqual([{ idKey: 'n2' }]);
  });

  it('getPersonNotes returns [] for unknown shape', async () => {
    const { getPersonNotes } = await import('../people');
    mockGet.mockResolvedValueOnce({ data: null });
    expect(await getPersonNotes('p1')).toEqual([]);
  });

  it('uploadPersonPhoto sends FormData', async () => {
    const { uploadPersonPhoto } = await import('../people');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'p1' } });
    const file = new File(['hi'], 'pic.png', { type: 'image/png' });
    await uploadPersonPhoto('p1', file);
    const [url, body] = mockPost.mock.calls[0];
    expect(url).toBe('/people/p1/photo');
    expect(body).toBeInstanceOf(FormData);
  });
});

// ===========================================================================
// groups.ts
// ===========================================================================
describe('groupsApi', () => {
  it('searchGroups builds query strings', async () => {
    const { searchGroups } = await import('../groups');
    mockGet.mockResolvedValueOnce({ data: [], meta: { page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
    await searchGroups({ q: 'ab', groupTypeId: 'gt1', page: 1, pageSize: 10 });
    const url = mockGet.mock.calls[0][0];
    expect(url).toContain('query=ab');
    expect(url).toContain('groupTypeId=gt1');
  });

  it('deleteGroup DELETEs /groups/:idKey', async () => {
    const { deleteGroup } = await import('../groups');
    mockDel.mockResolvedValueOnce(undefined);
    await deleteGroup('g1');
    expect(mockDel).toHaveBeenCalledWith('/groups/g1');
  });

  it('addGroupSchedule and removeGroupSchedule hit /schedules subresource', async () => {
    const { addGroupSchedule, removeGroupSchedule } = await import('../groups');
    mockPost.mockResolvedValueOnce({ data: { idKey: 's1' } });
    await addGroupSchedule('g1', { scheduleIdKey: 's1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/groups/g1/schedules', { scheduleIdKey: 's1' });

    mockDel.mockResolvedValueOnce(undefined);
    await removeGroupSchedule('g1', 's1');
    expect(mockDel).toHaveBeenCalledWith('/groups/g1/schedules/s1');
  });

  it('getChildGroups and attendance endpoints hit correct paths', async () => {
    const { getChildGroups, getGroupAttendanceHistory, getGroupAttendanceDetail } = await import('../groups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await getChildGroups('g1');
    expect(mockGet).toHaveBeenCalledWith('/groups/g1/children');

    mockGet.mockResolvedValueOnce({ data: [] });
    await getGroupAttendanceHistory('g1');
    expect(mockGet).toHaveBeenCalledWith('/groups/g1/attendance');

    mockGet.mockResolvedValueOnce({ data: [{ personIdKey: 'p1' }] });
    const detail = await getGroupAttendanceDetail('g1', 'o1');
    expect(mockGet).toHaveBeenCalledWith('/groups/g1/attendance/o1');
    expect(detail).toEqual([{ personIdKey: 'p1' }]);
  });
});

// ===========================================================================
// dashboard.ts
// ===========================================================================
describe('dashboardApi', () => {
  it('getDashboardStats unwraps data envelope', async () => {
    const { getDashboardStats } = await import('../dashboard');
    mockGet.mockResolvedValueOnce({ data: { activeCheckins: 10 } });
    expect(await getDashboardStats()).toEqual({ activeCheckins: 10 });
    expect(mockGet).toHaveBeenCalledWith('/dashboard/stats');
  });
});

// ===========================================================================
// auth.ts
// ===========================================================================
describe('authApi', () => {
  it('login validates response and stores tokens', async () => {
    const { login } = await import('../auth');
    mockPost.mockResolvedValueOnce({
      data: {
        accessToken: 'a',
        refreshToken: 'r',
        expiresAt: '2030-01-01',
        user: {
          idKey: 'u',
          firstName: 'A',
          lastName: 'B',
          email: null,
          photoUrl: null,
          roles: [],
        },
      },
    });
    const out = await login({ username: 'u', password: 'p' } as never);
    expect(out.accessToken).toBe('a');
    expect(client.setTokens).toHaveBeenCalledWith('a', 'r');
  });

  it('refresh posts new refreshToken and returns validated payload', async () => {
    const { refresh } = await import('../auth');
    mockPost.mockResolvedValueOnce({
      data: { accessToken: 'a2', refreshToken: 'r2', expiresAt: '2030' },
    });
    const out = await refresh('r1');
    expect(out.accessToken).toBe('a2');
    expect(mockPost).toHaveBeenCalledWith('/auth/refresh', { refreshToken: 'r1' }, { skipAuth: true });
  });

  it('logout clears tokens even if API fails', async () => {
    const { logout } = await import('../auth');
    mockPost.mockRejectedValueOnce(new Error('network'));
    await expect(logout('r1')).rejects.toThrow('network');
    expect(client.clearTokens).toHaveBeenCalled();
  });

  it('clearSession calls clearTokens', async () => {
    const { clearSession } = await import('../auth');
    clearSession();
    expect(client.clearTokens).toHaveBeenCalled();
  });
});

// ===========================================================================
// campuses, locations, schedules, search, groupTypes, reference
// Small CRUD modules; verify URL shapes.
// ===========================================================================
describe('small CRUD services', () => {
  it('campusesApi list/get/create/update/delete', async () => {
    const campuses = await import('../campuses');
    mockGet.mockResolvedValueOnce({ data: [] });
    await campuses.getCampuses();
    expect(mockGet).toHaveBeenCalledWith('/campuses');

    mockGet.mockResolvedValueOnce({ data: [] });
    await campuses.getCampuses(true);
    expect(mockGet).toHaveBeenLastCalledWith('/campuses?includeInactive=true');

    mockGet.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await campuses.getCampus('c1');
    expect(mockGet).toHaveBeenCalledWith('/campuses/c1');

    mockPost.mockResolvedValueOnce({ data: { idKey: 'new' } });
    await campuses.createCampus({ name: 'Main' } as never);
    expect(mockPost).toHaveBeenCalledWith('/campuses', { name: 'Main' });

    mockPut.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await campuses.updateCampus('c1', { name: 'New' } as never);
    expect(mockPut).toHaveBeenCalledWith('/campuses/c1', { name: 'New' });

    mockDel.mockResolvedValueOnce(undefined);
    await campuses.deleteCampus('c1');
    expect(mockDel).toHaveBeenCalledWith('/campuses/c1');
  });

  it('locationsApi list/get/tree/create/update/delete unwraps {data} envelope (#693)', async () => {
    const locations = await import('../locations');

    // Backend returns Ok(new { data = result }) for all locations endpoints that
    // have bodies (including Create's CreatedAtAction). The FE service must
    // unwrap so callers see the DTO directly, NOT { data: DTO }.

    // GET /locations
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'l1' }, { idKey: 'l2' }] });
    const list = await locations.getLocations();
    expect(mockGet).toHaveBeenCalledWith('/locations');
    expect(Array.isArray(list)).toBe(true);
    expect(list).toEqual([{ idKey: 'l1' }, { idKey: 'l2' }]);

    // GET /locations with query params
    mockGet.mockResolvedValueOnce({ data: [] });
    await locations.getLocations({ campusIdKey: 'c1', includeInactive: true });
    const listUrl = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(listUrl).toContain('campusIdKey=c1');
    expect(listUrl).toContain('includeInactive=true');

    // GET /locations/tree
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'root' }] });
    const tree = await locations.getLocationTree();
    expect(mockGet).toHaveBeenLastCalledWith('/locations/tree');
    expect(tree).toEqual([{ idKey: 'root' }]);

    // GET /locations/:idKey — assert unwrapped, not { data: ... }
    mockGet.mockResolvedValueOnce({ data: { idKey: 'l1', name: 'Sanctuary' } });
    const one = await locations.getLocation('l1');
    expect(mockGet).toHaveBeenLastCalledWith('/locations/l1');
    expect(one).toEqual({ idKey: 'l1', name: 'Sanctuary' });

    // POST /locations — CreatedAtAction(..., new { data = result.Value })
    mockPost.mockResolvedValueOnce({ data: { idKey: 'new', name: 'Room A' } });
    const created = await locations.createLocation({ name: 'Room A' } as never);
    expect(mockPost).toHaveBeenCalledWith('/locations', { name: 'Room A' });
    expect(created).toEqual({ idKey: 'new', name: 'Room A' });

    // PUT /locations/:idKey
    mockPut.mockResolvedValueOnce({ data: { idKey: 'l1', name: 'X' } });
    const updated = await locations.updateLocation('l1', { name: 'X' } as never);
    expect(mockPut).toHaveBeenCalledWith('/locations/l1', { name: 'X' });
    expect(updated).toEqual({ idKey: 'l1', name: 'X' });

    mockDel.mockResolvedValueOnce(undefined);
    await locations.deleteLocation('l1');
    expect(mockDel).toHaveBeenCalledWith('/locations/l1');
  });

  it('searchApi.globalSearch hits /search with required q param', async () => {
    const search = await import('../search');
    mockGet.mockResolvedValueOnce({ people: [], families: [], groups: [] });
    await search.globalSearch({ query: 'foo', category: 'people', pageNumber: 2, pageSize: 10 } as never);
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/search?');
    expect(url).toContain('q=foo');
    expect(url).toContain('category=people');
    expect(url).toContain('pageNumber=2');
    expect(url).toContain('pageSize=10');
  });
});
