/**
 * Admin-tier API service coverage (wave 3):
 *   - securityRoles: role CRUD, claims CRUD, role-member CRUD, claims listing
 *   - people.ts: remaining givingSummary + notes mutations
 *   - checkin.ts: opportunities, registerFamily, checkoutFromRoster
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';

vi.mock('../client', () => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
  patch: vi.fn(),
  apiClient: vi.fn(),
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

// ---------------------------------------------------------------------------
describe('securityRolesApi', () => {
  it('list / get / claims list', async () => {
    const api = await import('../securityRoles');
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'r1' }] });
    expect(await api.getSecurityRoles()).toEqual([{ idKey: 'r1' }]);
    expect(mockGet).toHaveBeenCalledWith('/admin/security-roles');

    mockGet.mockResolvedValueOnce({ data: { idKey: 'r1' } });
    await api.getSecurityRole('r1');
    expect(mockGet).toHaveBeenLastCalledWith('/admin/security-roles/r1');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getSecurityClaims();
    expect(mockGet).toHaveBeenLastCalledWith('/admin/security-claims');
  });

  it('create / update / delete role', async () => {
    const api = await import('../securityRoles');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'r1' } });
    await api.createSecurityRole({ name: 'Admin' });
    expect(mockPost).toHaveBeenCalledWith('/admin/security-roles', { name: 'Admin' });

    mockPut.mockResolvedValueOnce({ data: { idKey: 'r1' } });
    await api.updateSecurityRole('r1', { name: 'Admin2', isActive: true });
    expect(mockPut).toHaveBeenCalledWith('/admin/security-roles/r1', {
      name: 'Admin2',
      isActive: true,
    });

    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteSecurityRole('r1');
    expect(mockDel).toHaveBeenCalledWith('/admin/security-roles/r1');
  });

  it('assign/remove role claim', async () => {
    const api = await import('../securityRoles');
    mockPost.mockResolvedValueOnce(undefined);
    await api.assignRoleClaim('r1', { claimIdKey: 'c1', allowOrDeny: 'A' });
    expect(mockPost).toHaveBeenCalledWith('/admin/security-roles/r1/claims', {
      claimIdKey: 'c1',
      allowOrDeny: 'A',
    });

    mockDel.mockResolvedValueOnce(undefined);
    await api.removeRoleClaim('r1', 'c1');
    expect(mockDel).toHaveBeenCalledWith('/admin/security-roles/r1/claims/c1');
  });

  it('add/remove role member', async () => {
    const api = await import('../securityRoles');
    mockPost.mockResolvedValueOnce(undefined);
    await api.addRoleMember('r1', { personIdKey: 'p1' });
    expect(mockPost).toHaveBeenCalledWith('/admin/security-roles/r1/members', {
      personIdKey: 'p1',
    });

    mockDel.mockResolvedValueOnce(undefined);
    await api.removeRoleMember('r1', 'p1');
    expect(mockDel).toHaveBeenCalledWith('/admin/security-roles/r1/members/p1');
  });
});

// ---------------------------------------------------------------------------
describe('peopleApi deeper coverage', () => {
  it('createPerson posts to /people', async () => {
    const api = await import('../people');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'p1' } });
    await api.createPerson({ firstName: 'Jo' } as never);
    expect(mockPost).toHaveBeenCalledWith('/people', { firstName: 'Jo' });
  });

  it('updatePerson puts to /people/:idKey', async () => {
    const api = await import('../people');
    mockPut.mockResolvedValueOnce({ data: { idKey: 'p1' } });
    await api.updatePerson('p1', { firstName: 'X' } as never);
    expect(mockPut).toHaveBeenCalledWith('/people/p1', { firstName: 'X' });
  });

  it('getPersonGroups returns the paged result as-is (no envelope unwrap)', async () => {
    const api = await import('../people');
    const paged = { data: [{ idKey: 'g1' }], meta: { page: 1 } };
    mockGet.mockResolvedValueOnce(paged);
    const out = await api.getPersonGroups('p1');
    expect(mockGet).toHaveBeenCalledWith('/people/p1/groups');
    expect(out).toBe(paged);
  });

  it('getPersonGivingSummary unwraps envelope', async () => {
    const api = await import('../people');
    mockGet.mockResolvedValueOnce({ data: { ytdTotal: 100 } });
    const out = await api.getPersonGivingSummary('p1');
    expect(mockGet).toHaveBeenCalledWith('/people/p1/giving-summary');
    expect(out).toEqual({ ytdTotal: 100 });
  });

  it('note CRUD: create / update / delete', async () => {
    const api = await import('../people');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'n1' } });
    await api.createPersonNote('p1', { body: 'hi' } as never);
    expect(mockPost).toHaveBeenCalledWith('/people/p1/notes', { body: 'hi' });

    mockPut.mockResolvedValueOnce({ data: { idKey: 'n1' } });
    await api.updatePersonNote('p1', 'n1', { body: 'bye' } as never);
    expect(mockPut).toHaveBeenCalledWith('/people/p1/notes/n1', { body: 'bye' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.deletePersonNote('p1', 'n1');
    expect(mockDel).toHaveBeenCalledWith('/people/p1/notes/n1');
  });
});

// ---------------------------------------------------------------------------
describe('checkinApi deeper coverage', () => {
  it('getCheckinOpportunities maps backend response to frontend DTOs', async () => {
    const api = await import('../checkin');
    mockGet.mockResolvedValueOnce({
      data: {
        family: {
          familyIdKey: 'F1',
          familyName: 'Smith',
          members: [
            {
              personIdKey: 'P1',
              firstName: 'A',
              lastName: 'B',
              fullName: 'A B',
              age: 5,
            },
          ],
        },
        opportunities: [
          {
            person: {
              personIdKey: 'P1',
              firstName: 'A',
              lastName: 'B',
              fullName: 'A B',
            },
            currentAttendance: null,
            availableOptions: [],
          },
        ],
      },
    });
    const out = await api.getCheckinOpportunities('F1', { scheduleId: 's1' });
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toBe('/checkin/opportunities/F1?scheduleId=s1');
    expect(out.family.idKey).toBe('F1');
    expect(out.family.name).toBe('Smith');
    expect(out.opportunities[0].person.idKey).toBe('P1');
  });

  it('getCheckinOpportunities without params omits query', async () => {
    const api = await import('../checkin');
    mockGet.mockResolvedValueOnce({
      data: {
        family: { familyIdKey: 'F1', familyName: 'S', members: [] },
        opportunities: [],
      },
    });
    await api.getCheckinOpportunities('F1');
    expect(mockGet).toHaveBeenCalledWith('/checkin/opportunities/F1');
  });

  it('registerFamily posts and maps response', async () => {
    const api = await import('../checkin');
    mockPost.mockResolvedValueOnce({
      data: {
        familyIdKey: 'F1',
        familyName: 'Smith',
        members: [
          {
            personIdKey: 'P1',
            firstName: 'A',
            lastName: 'B',
            fullName: 'A B',
          },
        ],
      },
    });
    const out = await api.registerFamily({ familyName: 'Smith' } as never);
    expect(mockPost).toHaveBeenCalledWith('/checkin/register-family', {
      familyName: 'Smith',
    });
    expect(out.idKey).toBe('F1');
    expect(out.members[0].idKey).toBe('P1');
  });

  it('checkoutFromRoster posts with undefined body', async () => {
    const api = await import('../checkin');
    mockPost.mockResolvedValueOnce(undefined);
    await api.checkoutFromRoster('a1');
    expect(mockPost).toHaveBeenCalledWith('/checkin/checkout/a1', undefined);
  });

  it('searchFamiliesForCheckin falls back to cached results when offline', async () => {
    const { offlineFamilyCache } = await import('@/services/offline/OfflineFamilyCache');
    const mockClient = await import('../client');
    const api = await import('../checkin');
    // Force offline mode for this test.
    const origOnline = Object.getOwnPropertyDescriptor(navigator, 'onLine');
    Object.defineProperty(navigator, 'onLine', {
      configurable: true,
      get: () => false,
    });
    vi.mocked(mockClient.get).mockRejectedValueOnce(new Error('Network Error'));
    vi.mocked(offlineFamilyCache.getCachedResults).mockResolvedValueOnce([
      { idKey: 'F1', name: 'Cached', members: [] },
    ] as never);
    try {
      const out = await api.searchFamiliesForCheckin({
        searchValue: '555',
      } as never);
      expect(out).toEqual([{ idKey: 'F1', name: 'Cached', members: [] }]);
    } finally {
      if (origOnline) Object.defineProperty(navigator, 'onLine', origOnline);
    }
  });
});
