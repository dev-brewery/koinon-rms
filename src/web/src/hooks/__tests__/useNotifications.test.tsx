/**
 * useNotifications and related hooks.
 */

import { describe, it, expect, beforeEach, vi } from 'vitest';
import { waitFor, act } from '@testing-library/react';

vi.mock('@/services/api/notifications', () => ({
  getNotifications: vi.fn(),
  getNotification: vi.fn(),
  getUnreadCount: vi.fn(),
  getPreferences: vi.fn(),
  markAsRead: vi.fn(),
  markAllAsRead: vi.fn(),
  deleteNotification: vi.fn(),
  updatePreference: vi.fn(),
}));

import * as api from '@/services/api/notifications';
import {
  useDeleteNotification,
  useMarkAllAsRead,
  useMarkAsRead,
  useNotification,
  useNotificationPreferences,
  useNotifications,
  useUnreadCount,
  useUpdatePreference,
} from '../useNotifications';
import { renderHookWithQuery } from '@/test-utils/queryTestHarness';

beforeEach(() => vi.clearAllMocks());

describe('useNotifications', () => {
  it('forwards unreadOnly + limit to api', async () => {
    vi.mocked(api.getNotifications).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useNotifications(true, 5));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getNotifications).toHaveBeenCalledWith(true, 5);
  });
});

describe('useNotification', () => {
  it('idle without idKey', () => {
    const { result } = renderHookWithQuery(() => useNotification());
    expect(result.current.fetchStatus).toBe('idle');
  });

  it('fires with idKey', async () => {
    vi.mocked(api.getNotification).mockResolvedValueOnce({ idKey: 'n1' } as never);
    const { result } = renderHookWithQuery(() => useNotification('n1'));
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getNotification).toHaveBeenCalledWith('n1');
  });
});

describe('useUnreadCount / useNotificationPreferences', () => {
  it('unread count hits api', async () => {
    vi.mocked(api.getUnreadCount).mockResolvedValueOnce(3 as never);
    const { result } = renderHookWithQuery(() => useUnreadCount());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toBe(3);
  });

  it('preferences hits api', async () => {
    vi.mocked(api.getPreferences).mockResolvedValueOnce([] as never);
    const { result } = renderHookWithQuery(() => useNotificationPreferences());
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(api.getPreferences).toHaveBeenCalled();
  });
});

describe('mutations invalidate the right keys', () => {
  it('markAsRead invalidates notifications + unread-count + detail', async () => {
    vi.mocked(api.markAsRead).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useMarkAsRead());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('n1');
    });
    expect(api.markAsRead).toHaveBeenCalledWith('n1');
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications', 'unread-count'] });
  });

  it('markAllAsRead invalidates', async () => {
    vi.mocked(api.markAllAsRead).mockResolvedValueOnce(2 as never);
    const { result, client } = renderHookWithQuery(() => useMarkAllAsRead());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync();
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications'] });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications', 'unread-count'] });
  });

  it('deleteNotification invalidates', async () => {
    vi.mocked(api.deleteNotification).mockResolvedValueOnce(undefined as never);
    const { result, client } = renderHookWithQuery(() => useDeleteNotification());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync('n1');
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications'] });
  });

  it('updatePreference invalidates preferences', async () => {
    vi.mocked(api.updatePreference).mockResolvedValueOnce({ id: 1 } as never);
    const { result, client } = renderHookWithQuery(() => useUpdatePreference());
    const inv = vi.spyOn(client, 'invalidateQueries');
    await act(async () => {
      await result.current.mutateAsync({ type: 'email', enabled: true } as never);
    });
    expect(inv).toHaveBeenCalledWith({ queryKey: ['notifications', 'preferences'] });
  });
});
