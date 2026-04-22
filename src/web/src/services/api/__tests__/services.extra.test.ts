/**
 * Additional API service coverage (wave 3, #498).
 *
 * Targets modules that the existing services.test.ts / services.more.test.ts
 * only smoke-imported: followups, pager, personMerge, pickup, authorizedPickup,
 * publicGroups, communications, communicationTemplates, membershipRequests,
 * myGroups, files, auditLogApi, settings, devices (deeper), import (deeper),
 * giving (deeper), checkinOperations, groups (member endpoints).
 *
 * Same pattern as the existing files: mock ./client once, import each service
 * lazily, assert URL / HTTP method / body / envelope unwrapping.
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
const mockApiClient = vi.mocked(client.apiClient);

beforeEach(() => {
  mockGet.mockReset();
  mockPost.mockReset();
  mockPut.mockReset();
  mockDel.mockReset();
  mockApiClient.mockReset();
});

// ---------------------------------------------------------------------------
describe('followupsApi', () => {
  it('getPendingFollowUps without assignee => /followups/pending', async () => {
    const api = await import('../followups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getPendingFollowUps();
    expect(mockGet).toHaveBeenCalledWith('/followups/pending');
  });

  it('getPendingFollowUps with assignee appends query', async () => {
    const api = await import('../followups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getPendingFollowUps('u1');
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toBe('/followups/pending?assignedToIdKey=u1');
  });

  it('getFollowUp unwraps envelope', async () => {
    const api = await import('../followups');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'f1' } });
    expect(await api.getFollowUp('f1')).toEqual({ idKey: 'f1' });
    expect(mockGet).toHaveBeenCalledWith('/followups/f1');
  });

  it('updateFollowUpStatus puts to /followups/:idKey/status', async () => {
    const api = await import('../followups');
    mockPut.mockResolvedValueOnce({ data: { idKey: 'f1' } });
    await api.updateFollowUpStatus('f1', { status: 'Done' } as never);
    expect(mockPut).toHaveBeenCalledWith('/followups/f1/status', { status: 'Done' });
  });

  it('assignFollowUp puts with no envelope', async () => {
    const api = await import('../followups');
    mockPut.mockResolvedValueOnce(undefined);
    await api.assignFollowUp('f1', { personIdKey: 'p1' } as never);
    expect(mockPut).toHaveBeenCalledWith('/followups/f1/assign', { personIdKey: 'p1' });
  });
});

// ---------------------------------------------------------------------------
describe('pagerApi', () => {
  it('sendPage posts to /pager/send', async () => {
    const api = await import('../pager');
    mockPost.mockResolvedValueOnce({ data: { messageIdKey: 'm1' } });
    await api.sendPage({ pagerNumber: 7, message: 'Come' } as never);
    expect(mockPost).toHaveBeenCalledWith('/pager/send', { pagerNumber: 7, message: 'Come' });
  });

  it('searchPagers without args omits query', async () => {
    const api = await import('../pager');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.searchPagers();
    expect(mockGet).toHaveBeenCalledWith('/pager/search');
  });

  it('searchPagers with searchTerm + date builds query', async () => {
    const api = await import('../pager');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.searchPagers('Smith', '2024-01-01');
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/pager/search?');
    expect(url).toContain('searchTerm=Smith');
    expect(url).toContain('date=2024-01-01');
  });

  it('getPagerHistory uses pager number in path and optional date query', async () => {
    const api = await import('../pager');
    mockGet.mockResolvedValueOnce({ data: { pages: [] } });
    await api.getPagerHistory(7);
    expect(mockGet).toHaveBeenCalledWith('/pager/7/history');

    mockGet.mockResolvedValueOnce({ data: { pages: [] } });
    await api.getPagerHistory(7, '2024-01-01');
    const url = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url).toContain('/pager/7/history?date=2024-01-01');
  });

  it('getNextPagerNumber with/without campus', async () => {
    const api = await import('../pager');
    mockGet.mockResolvedValueOnce({ data: 42 });
    expect(await api.getNextPagerNumber()).toBe(42);
    expect(mockGet).toHaveBeenCalledWith('/pager/next-number');

    mockGet.mockResolvedValueOnce({ data: 43 });
    await api.getNextPagerNumber('c1');
    expect(mockGet).toHaveBeenLastCalledWith('/pager/next-number?campusId=c1');
  });
});

// ---------------------------------------------------------------------------
describe('personMergeApi', () => {
  it('getDuplicates default pagination', async () => {
    const api = await import('../personMerge');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await api.getDuplicates();
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/people/duplicates?');
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=25');
  });

  it('getDuplicatesForPerson unwraps envelope', async () => {
    const api = await import('../personMerge');
    mockGet.mockResolvedValueOnce({ data: [{ idKey: 'p2' }] });
    const out = await api.getDuplicatesForPerson('p1');
    expect(mockGet).toHaveBeenCalledWith('/people/p1/duplicates');
    expect(out).toEqual([{ idKey: 'p2' }]);
  });

  it('comparePeople builds query with both idKeys', async () => {
    const api = await import('../personMerge');
    mockGet.mockResolvedValueOnce({ data: { person1: {}, person2: {} } });
    await api.comparePeople('p1', 'p2');
    expect(mockGet).toHaveBeenCalledWith(
      '/people/compare?person1IdKey=p1&person2IdKey=p2'
    );
  });

  it('mergePeople posts to /people/merge', async () => {
    const api = await import('../personMerge');
    mockPost.mockResolvedValueOnce({ data: { mergedIdKey: 'p1' } });
    const req = { targetIdKey: 'p1', sourceIdKey: 'p2' } as never;
    await api.mergePeople(req);
    expect(mockPost).toHaveBeenCalledWith('/people/merge', req);
  });

  it('getMergeHistory default pagination', async () => {
    const api = await import('../personMerge');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await api.getMergeHistory();
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/people/merge-history?');
    expect(url).toContain('page=1');
  });

  it('ignoreDuplicate posts to /people/duplicates/ignore', async () => {
    const api = await import('../personMerge');
    mockPost.mockResolvedValueOnce(undefined);
    await api.ignoreDuplicate({ person1IdKey: 'p1', person2IdKey: 'p2' } as never);
    expect(mockPost).toHaveBeenCalledWith('/people/duplicates/ignore', {
      person1IdKey: 'p1',
      person2IdKey: 'p2',
    });
  });

  it('unignoreDuplicate deletes with both idKeys in path', async () => {
    const api = await import('../personMerge');
    mockDel.mockResolvedValueOnce(undefined);
    await api.unignoreDuplicate('p1', 'p2');
    expect(mockDel).toHaveBeenCalledWith('/people/duplicates/ignore/p1/p2');
  });
});

// ---------------------------------------------------------------------------
describe('pickupApi', () => {
  it('verifyPickup posts to /checkin/verify-pickup', async () => {
    const api = await import('../pickup');
    mockPost.mockResolvedValueOnce({ data: { isAuthorized: true } });
    const req = { childIdKey: 'c1', pickupPersonName: 'Bob' } as never;
    await api.verifyPickup(req);
    expect(mockPost).toHaveBeenCalledWith('/checkin/verify-pickup', req);
  });

  it('recordPickup returns body directly (no unwrap)', async () => {
    const api = await import('../pickup');
    mockPost.mockResolvedValueOnce({ idKey: 'log1' });
    const out = await api.recordPickup({ childIdKey: 'c1' } as never);
    expect(out).toEqual({ idKey: 'log1' });
    expect(mockPost).toHaveBeenCalledWith('/checkin/record-pickup', { childIdKey: 'c1' });
  });

  it('getPickupHistory without dates / with dates', async () => {
    const api = await import('../pickup');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getPickupHistory('c1');
    expect(mockGet).toHaveBeenCalledWith('/checkin/people/c1/pickup-history');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getPickupHistory('c1', '2024-01-01', '2024-02-01');
    const url = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url).toContain('fromDate=2024-01-01');
    expect(url).toContain('toDate=2024-02-01');
  });
});

// ---------------------------------------------------------------------------
describe('authorizedPickupApi', () => {
  it('getAuthorizedPickups unwraps envelope', async () => {
    const api = await import('../authorizedPickup');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getAuthorizedPickups('c1');
    expect(mockGet).toHaveBeenCalledWith('/people/c1/authorized-pickups');
  });

  it('createAuthorizedPickup posts to child-scoped URL', async () => {
    const api = await import('../authorizedPickup');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'ap1' } });
    await api.createAuthorizedPickup('c1', { personIdKey: 'p1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/people/c1/authorized-pickups', {
      personIdKey: 'p1',
    });
  });

  it('updateAuthorizedPickup puts by pickup idKey (child agnostic)', async () => {
    const api = await import('../authorizedPickup');
    mockPut.mockResolvedValueOnce({ data: { idKey: 'ap1' } });
    await api.updateAuthorizedPickup('ap1', { relationship: 'Parent' } as never);
    expect(mockPut).toHaveBeenCalledWith('/authorized-pickups/ap1', {
      relationship: 'Parent',
    });
  });

  it('deleteAuthorizedPickup deletes by pickup idKey', async () => {
    const api = await import('../authorizedPickup');
    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteAuthorizedPickup('ap1');
    expect(mockDel).toHaveBeenCalledWith('/authorized-pickups/ap1');
  });

  it('autoPopulateFamilyMembers posts with empty body and unwraps data', async () => {
    const api = await import('../authorizedPickup');
    mockPost.mockResolvedValueOnce({
      data: { message: 'ok', count: 2, pickups: [] },
    });
    const out = await api.autoPopulateFamilyMembers('c1');
    expect(mockPost).toHaveBeenCalledWith(
      '/people/c1/authorized-pickups/auto-populate',
      {}
    );
    expect(out.count).toBe(2);
  });
});

// ---------------------------------------------------------------------------
describe('publicGroupsApi', () => {
  it('searchPublicGroups with no params hits /groups/public (skipAuth)', async () => {
    const api = await import('../publicGroups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.searchPublicGroups();
    expect(mockGet).toHaveBeenCalledWith('/groups/public', { skipAuth: true });
  });

  it('searchPublicGroups encodes the full param set', async () => {
    const api = await import('../publicGroups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.searchPublicGroups({
      searchTerm: 'bible',
      groupTypeIdKey: 'gt1',
      campusIdKey: 'c1',
      dayOfWeek: 0,
      timeOfDay: 2,
      hasOpenings: true,
      pageNumber: 2,
      pageSize: 20,
    });
    const url = mockGet.mock.calls[0][0] as string;
    for (const token of [
      '/groups/public?',
      'searchTerm=bible',
      'groupTypeIdKey=gt1',
      'campusIdKey=c1',
      'dayOfWeek=0',
      'timeOfDay=2',
      'hasOpenings=true',
      'pageNumber=2',
      'pageSize=20',
    ]) {
      expect(url).toContain(token);
    }
  });
});

// ---------------------------------------------------------------------------
describe('communicationsApi', () => {
  it('createCommunication, sendCommunication, scheduleCommunication, cancelSchedule', async () => {
    const api = await import('../communications');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await api.createCommunication({ subject: 'S' } as never);
    expect(mockPost).toHaveBeenCalledWith('/communications', { subject: 'S' });

    mockPost.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await api.sendCommunication('c1');
    expect(mockPost).toHaveBeenLastCalledWith('/communications/c1/send');

    mockPost.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await api.scheduleCommunication('c1', '2024-01-01T12:00:00Z');
    expect(mockPost).toHaveBeenLastCalledWith('/communications/c1/schedule', {
      scheduledDateTime: '2024-01-01T12:00:00Z',
    });

    mockPost.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await api.cancelSchedule('c1');
    expect(mockPost).toHaveBeenLastCalledWith('/communications/c1/cancel-schedule');
  });

  it('getCommunication / getCommunications (with / without params)', async () => {
    const api = await import('../communications');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await api.getCommunication('c1');
    expect(mockGet).toHaveBeenCalledWith('/communications/c1');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getCommunications();
    expect(mockGet).toHaveBeenLastCalledWith('/communications');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getCommunications({ page: 2, pageSize: 50, status: 'Draft' });
    const url = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=50');
    expect(url).toContain('status=Draft');
  });

  it('getMergeFields unwraps', async () => {
    const api = await import('../communications');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getMergeFields();
    expect(mockGet).toHaveBeenCalledWith('/communications/merge-fields');
  });

  it('previewCommunication posts', async () => {
    const api = await import('../communications');
    mockPost.mockResolvedValueOnce({ data: { body: 'x' } });
    await api.previewCommunication({ templateIdKey: 't1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/communications/preview', {
      templateIdKey: 't1',
    });
  });

  it('getCommunicationPreferences / update / bulkUpdate', async () => {
    const api = await import('../communications');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getCommunicationPreferences('p1');
    expect(mockGet).toHaveBeenCalledWith('/people/p1/communication-preferences');

    mockPut.mockResolvedValueOnce({ data: { type: 'Email' } });
    await api.updateCommunicationPreference('p1', 'Email', { optIn: false } as never);
    expect(mockPut).toHaveBeenLastCalledWith(
      '/people/p1/communication-preferences/Email',
      { optIn: false }
    );

    mockPut.mockResolvedValueOnce({ data: [] });
    await api.bulkUpdateCommunicationPreferences('p1', {
      preferences: [],
    } as never);
    expect(mockPut).toHaveBeenLastCalledWith('/people/p1/communication-preferences', {
      preferences: [],
    });
  });
});

// ---------------------------------------------------------------------------
describe('communicationTemplatesApi', () => {
  it('list / get / create / update / delete', async () => {
    const api = await import('../communicationTemplates');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getCommunicationTemplates();
    expect(mockGet).toHaveBeenCalledWith('/communication-templates');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getCommunicationTemplates({ page: 1, pageSize: 20, type: 'Email', isActive: true });
    const url = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=20');
    expect(url).toContain('type=Email');
    expect(url).toContain('isActive=true');

    mockGet.mockResolvedValueOnce({ data: { idKey: 't1' } });
    await api.getCommunicationTemplate('t1');
    expect(mockGet).toHaveBeenLastCalledWith('/communication-templates/t1');

    mockPost.mockResolvedValueOnce({ data: { idKey: 't1' } });
    await api.createCommunicationTemplate({ name: 'N' } as never);
    expect(mockPost).toHaveBeenCalledWith('/communication-templates', { name: 'N' });

    mockPut.mockResolvedValueOnce({ data: { idKey: 't1' } });
    await api.updateCommunicationTemplate('t1', { name: 'X' } as never);
    expect(mockPut).toHaveBeenCalledWith('/communication-templates/t1', { name: 'X' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteCommunicationTemplate('t1');
    expect(mockDel).toHaveBeenCalledWith('/communication-templates/t1');
  });
});

// ---------------------------------------------------------------------------
describe('membershipRequestsApi', () => {
  it('submit / list / process', async () => {
    const api = await import('../membershipRequests');
    mockPost.mockResolvedValueOnce({ idKey: 'r1' });
    await api.submitMembershipRequest('g1', { note: 'x' } as never);
    expect(mockPost).toHaveBeenCalledWith('/groups/g1/membership-requests', { note: 'x' });

    mockGet.mockResolvedValueOnce([{ idKey: 'r1' }]);
    const pending = await api.getPendingRequests('g1');
    expect(mockGet).toHaveBeenCalledWith('/groups/g1/membership-requests');
    expect(pending).toEqual([{ idKey: 'r1' }]);

    mockPut.mockResolvedValueOnce({ idKey: 'r1' });
    await api.processRequest('g1', 'r1', { approve: true } as never);
    expect(mockPut).toHaveBeenCalledWith('/groups/g1/membership-requests/r1', { approve: true });
  });
});

// ---------------------------------------------------------------------------
describe('myGroupsApi', () => {
  it('getMyGroups / getMyGroupMembers / update / remove / recordAttendance', async () => {
    const api = await import('../myGroups');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getMyGroups();
    expect(mockGet).toHaveBeenCalledWith('/my-groups');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getMyGroupMembers('g1');
    expect(mockGet).toHaveBeenLastCalledWith('/my-groups/g1/members');

    mockPut.mockResolvedValueOnce({ data: { idKey: 'm1' } });
    await api.updateGroupMember('g1', 'm1', { note: 'x' } as never);
    expect(mockPut).toHaveBeenCalledWith('/my-groups/g1/members/m1', { note: 'x' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.removeGroupMember('g1', 'm1');
    expect(mockDel).toHaveBeenCalledWith('/my-groups/g1/members/m1');

    mockPost.mockResolvedValueOnce(undefined);
    await api.recordAttendance('g1', { attendance: [] } as never);
    expect(mockPost).toHaveBeenCalledWith('/my-groups/g1/attendance', { attendance: [] });
  });
});

// ---------------------------------------------------------------------------
describe('settingsApi', () => {
  it('preferences get/update, sessions get/revoke', async () => {
    const api = await import('../settings');
    mockGet.mockResolvedValueOnce({ data: { theme: 'dark' } });
    await api.getPreferences();
    expect(mockGet).toHaveBeenCalledWith('/my-settings/preferences');

    mockPut.mockResolvedValueOnce({ data: { theme: 'light' } });
    await api.updatePreferences({ theme: 'light' } as never);
    expect(mockPut).toHaveBeenCalledWith('/my-settings/preferences', { theme: 'light' });

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getSessions();
    expect(mockGet).toHaveBeenLastCalledWith('/my-settings/sessions');

    mockDel.mockResolvedValueOnce(undefined);
    await api.revokeSession('s1');
    expect(mockDel).toHaveBeenCalledWith('/my-settings/sessions/s1');
  });

  it('security: changePassword / 2FA status / setup / verify / disable', async () => {
    const api = await import('../settings');
    mockPost.mockResolvedValueOnce(undefined);
    await api.changePassword({ currentPassword: 'a', newPassword: 'b' } as never);
    expect(mockPost).toHaveBeenCalledWith('/my-settings/change-password', {
      currentPassword: 'a',
      newPassword: 'b',
    });

    mockGet.mockResolvedValueOnce({ data: { isEnabled: false } });
    await api.getTwoFactorStatus();
    expect(mockGet).toHaveBeenLastCalledWith('/my-settings/two-factor');

    mockPost.mockResolvedValueOnce({ data: { qrCode: 'q' } });
    await api.setupTwoFactor();
    expect(mockPost).toHaveBeenLastCalledWith('/my-settings/two-factor/setup');

    mockPost.mockResolvedValueOnce(undefined);
    await api.verifyTwoFactor('123456');
    expect(mockPost).toHaveBeenLastCalledWith('/my-settings/two-factor/verify', {
      code: '123456',
    });

    mockDel.mockResolvedValueOnce(undefined);
    await api.disableTwoFactor('123456');
    // disable sends body via stringified JSON
    const [url, options] = mockDel.mock.calls.at(-1) as [string, { body: string }];
    expect(url).toBe('/my-settings/two-factor');
    expect(options.body).toBe(JSON.stringify({ code: '123456' }));
  });
});

// ---------------------------------------------------------------------------
describe('auditLogApi', () => {
  it('searchAuditLogs empty params => /audit-logs', async () => {
    const api = await import('../auditLogApi');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await api.searchAuditLogs();
    expect(mockGet).toHaveBeenCalledWith('/audit-logs');
  });

  it('searchAuditLogs forwards the full filter', async () => {
    const api = await import('../auditLogApi');
    const { AuditAction } = await import('../types');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await api.searchAuditLogs({
      startDate: '2024-01-01',
      endDate: '2024-02-01',
      entityType: 'Person',
      actionType: AuditAction.Update,
      personIdKey: 'p1',
      entityIdKey: 'e1',
      page: 2,
      pageSize: 25,
    });
    const url = mockGet.mock.calls[0][0] as string;
    for (const token of [
      '/audit-logs?',
      'startDate=2024-01-01',
      'endDate=2024-02-01',
      'entityType=Person',
      'actionType=Update',
      'personIdKey=p1',
      'entityIdKey=e1',
      'page=2',
      'pageSize=25',
    ]) {
      expect(url).toContain(token);
    }
  });

  it('getEntityAuditHistory has no envelope', async () => {
    const api = await import('../auditLogApi');
    mockGet.mockResolvedValueOnce([{ idKey: 'a1' }]);
    const out = await api.getEntityAuditHistory('Person', 'p1');
    expect(mockGet).toHaveBeenCalledWith('/audit-logs/entity/Person/p1');
    expect(out).toEqual([{ idKey: 'a1' }]);
  });

  it('exportAuditLogs uses fetch directly and returns a blob', async () => {
    const api = await import('../auditLogApi');
    // Stub global fetch
    const blob = new Blob(['csv'], { type: 'text/csv' });
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
      blob: () => Promise.resolve(blob),
    } as unknown as Response);
    vi.mocked(client.getAccessToken).mockReturnValue('tok');

    try {
      const out = await api.exportAuditLogs({
        startDate: '2024-01-01',
        format: 'csv',
      } as never);
      expect(out).toBe(blob);
      const [url, opts] = fetchSpy.mock.calls[0];
      expect(String(url)).toContain('/audit-logs/export?');
      expect(String(url)).toContain('startDate=2024-01-01');
      expect(String(url)).toContain('format=csv');
      const headers = (opts as RequestInit).headers as Record<string, string>;
      expect(headers['Authorization']).toBe('Bearer tok');
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it('exportAuditLogs throws on non-ok response', async () => {
    const api = await import('../auditLogApi');
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: false,
      statusText: 'Nope',
    } as unknown as Response);
    try {
      await expect(api.exportAuditLogs({ format: 'csv' } as never)).rejects.toThrow(
        /Failed to export audit logs/
      );
    } finally {
      fetchSpy.mockRestore();
    }
  });
});

// ---------------------------------------------------------------------------
describe('filesApi', () => {
  it('uploadFile sends FormData through apiClient', async () => {
    const api = await import('../files');
    mockApiClient.mockResolvedValueOnce({ data: { idKey: 'f1' } } as never);
    const file = new File(['x'], 'x.txt', { type: 'text/plain' });
    await api.uploadFile(file, { description: 'd', binaryFileTypeIdKey: 'bft1' });
    const [endpoint, options] = mockApiClient.mock.calls[0];
    expect(endpoint).toBe('/files');
    const opts = options as RequestInit;
    expect(opts.method).toBe('POST');
    expect(opts.body).toBeInstanceOf(FormData);
  });

  it('getFileMetadata unwraps envelope', async () => {
    const api = await import('../files');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'f1' } });
    expect(await api.getFileMetadata('f1')).toEqual({ idKey: 'f1' });
    expect(mockGet).toHaveBeenCalledWith('/files/f1/metadata');
  });

  it('downloadFile uses fetch directly', async () => {
    const api = await import('../files');
    const blob = new Blob(['x']);
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
      blob: () => Promise.resolve(blob),
    } as unknown as Response);
    vi.mocked(client.getAccessToken).mockReturnValue('tok');

    try {
      const out = await api.downloadFile('f1');
      expect(out).toBe(blob);
      const [url, opts] = fetchSpy.mock.calls[0];
      expect(String(url)).toContain('/files/f1');
      const headers = (opts as RequestInit).headers as Record<string, string>;
      expect(headers['Authorization']).toBe('Bearer tok');
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it('downloadFile throws on non-ok', async () => {
    const api = await import('../files');
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: false,
      statusText: 'Nope',
    } as unknown as Response);
    try {
      await expect(api.downloadFile('f1')).rejects.toThrow(/Failed to download file/);
    } finally {
      fetchSpy.mockRestore();
    }
  });

  it('deleteFile deletes by idKey', async () => {
    const api = await import('../files');
    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteFile('f1');
    expect(mockDel).toHaveBeenCalledWith('/files/f1');
  });
});

// ---------------------------------------------------------------------------
describe('devicesApi deeper coverage', () => {
  it('getDevices forwards filters + unwraps envelope', async () => {
    const api = await import('../devices');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getDevices({
      q: 'iPad',
      campusId: 'c1',
      includeInactive: true,
      page: 2,
      pageSize: 25,
    });
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/devices?');
    expect(url).toContain('q=iPad');
    expect(url).toContain('campusIdKey=c1');
    expect(url).toContain('includeInactive=true');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
  });

  it('getDeviceByIdKey / create / update / delete', async () => {
    const api = await import('../devices');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'd1' } });
    await api.getDeviceByIdKey('d1');
    expect(mockGet).toHaveBeenCalledWith('/devices/d1');

    mockPost.mockResolvedValueOnce({ data: { idKey: 'd1' } });
    await api.createDevice({ name: 'iPad 1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/devices', { name: 'iPad 1' });

    mockPut.mockResolvedValueOnce({ data: { idKey: 'd1' } });
    await api.updateDevice('d1', { name: 'iPad 2' } as never);
    expect(mockPut).toHaveBeenCalledWith('/devices/d1', { name: 'iPad 2' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteDevice('d1');
    expect(mockDel).toHaveBeenCalledWith('/devices/d1');
  });

  it('generateKioskToken posts empty body', async () => {
    const api = await import('../devices');
    mockPost.mockResolvedValueOnce({ data: { token: 't', deviceIdKey: 'd1', deviceName: 'A' } });
    const out = await api.generateKioskToken('d1');
    expect(mockPost).toHaveBeenCalledWith('/devices/d1/token', {});
    expect(out.token).toBe('t');
  });
});

// ---------------------------------------------------------------------------
describe('importApi deeper coverage', () => {
  it('uploadCsvPreview calls apiClient with FormData', async () => {
    const api = await import('../import');
    mockApiClient.mockResolvedValueOnce({ data: { headers: [] } } as never);
    const file = new File(['a,b'], 'x.csv', { type: 'text/csv' });
    await api.uploadCsvPreview(file);
    const [endpoint, options] = mockApiClient.mock.calls[0];
    expect(endpoint).toBe('/import/upload');
    expect((options as RequestInit).method).toBe('POST');
    expect((options as RequestInit).body).toBeInstanceOf(FormData);
  });

  it('getImportTemplates with/without type filter', async () => {
    const api = await import('../import');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getImportTemplates();
    expect(mockGet).toHaveBeenCalledWith('/import/templates');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getImportTemplates('People');
    expect(mockGet).toHaveBeenLastCalledWith('/import/templates?type=People');
  });

  it('getImportTemplate / createImportTemplate / deleteImportTemplate', async () => {
    const api = await import('../import');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'it1' } });
    await api.getImportTemplate('it1');
    expect(mockGet).toHaveBeenLastCalledWith('/import/templates/it1');

    mockPost.mockResolvedValueOnce({ data: { idKey: 'it1' } });
    await api.createImportTemplate({ name: 'N' } as never);
    expect(mockPost).toHaveBeenCalledWith('/import/templates', { name: 'N' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.deleteImportTemplate('it1');
    expect(mockDel).toHaveBeenCalledWith('/import/templates/it1');
  });

  it('validateImport sends FormData with mappings JSON', async () => {
    const api = await import('../import');
    mockApiClient.mockResolvedValueOnce({ data: { jobIdKey: 'j1' } } as never);
    const file = new File(['a,b'], 'x.csv', { type: 'text/csv' });
    await api.validateImport(file, 'People', { a: 'firstName' });
    const [endpoint, options] = mockApiClient.mock.calls[0];
    expect(endpoint).toBe('/import/validate');
    expect((options as RequestInit).body).toBeInstanceOf(FormData);
  });

  it('executeImport includes templateIdKey when provided', async () => {
    const api = await import('../import');
    mockApiClient.mockResolvedValueOnce({ data: { idKey: 'j1' } } as never);
    const file = new File(['a,b'], 'x.csv', { type: 'text/csv' });
    await api.executeImport(file, 'People', {}, 'tpl1');
    const [, options] = mockApiClient.mock.calls[0];
    const body = (options as RequestInit).body as FormData;
    expect(body.get('templateIdKey')).toBe('tpl1');
    expect(body.get('importType')).toBe('People');
  });

  it('getImportJobs / getImportJobStatus', async () => {
    const api = await import('../import');
    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getImportJobs();
    expect(mockGet).toHaveBeenLastCalledWith('/import/jobs');

    mockGet.mockResolvedValueOnce({ data: [] });
    await api.getImportJobs('People');
    expect(mockGet).toHaveBeenLastCalledWith('/import/jobs?type=People');

    mockGet.mockResolvedValueOnce({ data: { idKey: 'j1' } });
    await api.getImportJobStatus('j1');
    expect(mockGet).toHaveBeenLastCalledWith('/import/jobs/j1');
  });

  it('downloadImportErrors uses fetch directly', async () => {
    const api = await import('../import');
    const blob = new Blob(['csv']);
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
      blob: () => Promise.resolve(blob),
    } as unknown as Response);
    vi.mocked(client.getAccessToken).mockReturnValue('tok');
    try {
      const out = await api.downloadImportErrors('j1');
      expect(out).toBe(blob);
      const [url] = fetchSpy.mock.calls[0];
      expect(String(url)).toContain('/import/jobs/j1/errors');
    } finally {
      fetchSpy.mockRestore();
    }
  });
});

// ---------------------------------------------------------------------------
describe('givingApi deeper coverage', () => {
  it('getFund, getBatch, getBatchSummary unwrap envelopes', async () => {
    const giving = await import('../giving');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'f1' } });
    expect(await giving.getFund('f1')).toEqual({ idKey: 'f1' });

    mockGet.mockResolvedValueOnce({ data: { idKey: 'b1' } });
    expect(await giving.getBatch('b1')).toEqual({ idKey: 'b1' });

    mockGet.mockResolvedValueOnce({ data: { totalAmount: 10 } });
    expect(await giving.getBatchSummary('b1')).toEqual({ totalAmount: 10 });
  });

  it('deactivateFund deletes at /giving/admin/funds/:idKey', async () => {
    const giving = await import('../giving');
    mockDel.mockResolvedValueOnce(undefined);
    await giving.deactivateFund('f1');
    expect(mockDel).toHaveBeenCalledWith('/giving/admin/funds/f1');
  });

  it('getBatches forwards all filters and pagination', async () => {
    const giving = await import('../giving');
    mockGet.mockResolvedValueOnce({ data: [], meta: { page: 1 } });
    await giving.getBatches({
      status: 'Open',
      campusIdKey: 'c1',
      startDate: '2024-01-01',
      endDate: '2024-02-01',
      page: 2,
      pageSize: 25,
    });
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/giving/batches?');
    expect(url).toContain('status=Open');
    expect(url).toContain('campusIdKey=c1');
    expect(url).toContain('startDate=2024-01-01');
    expect(url).toContain('page=2');
  });

  it('openBatch / closeBatch post empty object body', async () => {
    const giving = await import('../giving');
    mockPost.mockResolvedValueOnce({ message: 'ok' });
    await giving.openBatch('b1');
    expect(mockPost).toHaveBeenCalledWith('/giving/batches/b1/open', {});

    mockPost.mockResolvedValueOnce({ message: 'ok' });
    await giving.closeBatch('b1');
    expect(mockPost).toHaveBeenLastCalledWith('/giving/batches/b1/close', {});
  });

  it('createBatch and addContribution return body directly (no unwrap)', async () => {
    const giving = await import('../giving');
    mockPost.mockResolvedValueOnce({ idKey: 'b1' });
    const out = await giving.createBatch({ name: 'Jan' } as never);
    expect(out).toEqual({ idKey: 'b1' });

    mockPost.mockResolvedValueOnce({ idKey: 'c1' });
    const c = await giving.addContribution('b1', { amount: 10 } as never);
    expect(c).toEqual({ idKey: 'c1' });
    expect(mockPost).toHaveBeenLastCalledWith('/giving/batches/b1/contributions', {
      amount: 10,
    });
  });

  it('contribution get/update/delete hit /giving/contributions/:idKey', async () => {
    const giving = await import('../giving');
    mockGet.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await giving.getContribution('c1');
    expect(mockGet).toHaveBeenLastCalledWith('/giving/contributions/c1');

    mockPut.mockResolvedValueOnce({ data: { idKey: 'c1' } });
    await giving.updateContribution('c1', { amount: 15 } as never);
    expect(mockPut).toHaveBeenCalledWith('/giving/contributions/c1', { amount: 15 });

    mockDel.mockResolvedValueOnce(undefined);
    await giving.deleteContribution('c1');
    expect(mockDel).toHaveBeenCalledWith('/giving/contributions/c1');
  });

  it('statements: getStatements default pagination; getStatement unwraps', async () => {
    const giving = await import('../giving');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await giving.getStatements();
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/giving/statements?');
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=25');

    mockGet.mockResolvedValueOnce({ data: { idKey: 'st1' } });
    expect(await giving.getStatement('st1')).toEqual({ idKey: 'st1' });
  });

  it('previewStatement posts to preview', async () => {
    const giving = await import('../giving');
    mockPost.mockResolvedValueOnce({ data: { peopleCount: 5 } });
    await giving.previewStatement({ startDate: 'a', endDate: 'b' } as never);
    expect(mockPost).toHaveBeenCalledWith('/giving/statements/preview', {
      startDate: 'a',
      endDate: 'b',
    });
  });

  it('generateStatement returns body directly', async () => {
    const giving = await import('../giving');
    mockPost.mockResolvedValueOnce({ idKey: 'st1' });
    const out = await giving.generateStatement({ startDate: 'a', endDate: 'b' } as never);
    expect(out).toEqual({ idKey: 'st1' });
  });

  it('getEligiblePeople includes minimumAmount only when defined', async () => {
    const giving = await import('../giving');
    mockGet.mockResolvedValueOnce({ data: [] });
    await giving.getEligiblePeople('2024-01-01', '2024-02-01');
    const url1 = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url1).toContain('startDate=2024-01-01');
    expect(url1).toContain('endDate=2024-02-01');
    expect(url1).not.toContain('minimumAmount');

    mockGet.mockResolvedValueOnce({ data: [] });
    await giving.getEligiblePeople('2024-01-01', '2024-02-01', 25);
    const url2 = mockGet.mock.calls.at(-1)?.[0] as string;
    expect(url2).toContain('minimumAmount=25');
  });
});

// ---------------------------------------------------------------------------
describe('checkinOperationsApi (#482)', () => {
  it('getCheckinOperationsDashboard unwraps envelope', async () => {
    const api = await import('../checkinOperations');
    mockGet.mockResolvedValueOnce({
      data: { rooms: [], attendees: [], summary: {}, generatedAt: 'now' },
    });
    await api.getCheckinOperationsDashboard();
    expect(mockGet).toHaveBeenCalledWith('/checkin-operations/dashboard');
  });

  it('toggleCheckinOperationsRoom encodes locationIdKey', async () => {
    const api = await import('../checkinOperations');
    mockPost.mockResolvedValueOnce({
      data: { locationIdKey: 'abc/def', isOpen: true },
    });
    await api.toggleCheckinOperationsRoom('abc/def');
    // encodeURIComponent turns `/` into `%2F`
    expect(mockPost).toHaveBeenCalledWith(
      '/checkin-operations/rooms/abc%2Fdef/toggle',
      undefined
    );
  });
});

// ---------------------------------------------------------------------------
describe('groupsApi member endpoints', () => {
  it('getGroupMembers builds status/role/pagination query', async () => {
    const api = await import('../groups');
    mockGet.mockResolvedValueOnce({ data: [], meta: {} });
    await api.getGroupMembers('g1', {
      status: 'Active',
      roleId: 'r1',
      page: 2,
      pageSize: 25,
    });
    const url = mockGet.mock.calls[0][0] as string;
    expect(url).toContain('/groups/g1/members?');
    expect(url).toContain('status=Active');
    expect(url).toContain('roleId=r1');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
  });

  it('addGroupMember posts + removeGroupMember deletes', async () => {
    const api = await import('../groups');
    mockPost.mockResolvedValueOnce({ data: { idKey: 'm1' } });
    await api.addGroupMember('g1', { personIdKey: 'p1' } as never);
    expect(mockPost).toHaveBeenCalledWith('/groups/g1/members', { personIdKey: 'p1' });

    mockDel.mockResolvedValueOnce(undefined);
    await api.removeGroupMember('g1', 'm1');
    expect(mockDel).toHaveBeenCalledWith('/groups/g1/members/m1');
  });
});
