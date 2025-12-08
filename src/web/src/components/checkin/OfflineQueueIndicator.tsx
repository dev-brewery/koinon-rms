/**
 * Offline Queue Indicator
 * Shows queue status and sync progress for offline check-ins
 */

import { Card } from '@/components/ui';
import type { OfflineCheckinState } from '@/hooks/useOfflineCheckin';

export interface OfflineQueueIndicatorProps {
  state: OfflineCheckinState;
  onSync?: () => void;
}

export function OfflineQueueIndicator({ state, onSync }: OfflineQueueIndicatorProps) {
  const { mode, queuedCount, syncStatus, lastSyncResults } = state;

  // Don't show if online and no queue
  if (mode === 'online' && queuedCount === 0 && syncStatus === 'idle') {
    return null;
  }

  return (
    <div className="fixed top-4 right-4 z-50 max-w-md">
      {/* Offline Mode Indicator */}
      {mode === 'offline' && (
        <Card className="bg-orange-50 border-2 border-orange-300 shadow-lg mb-2">
          <div className="flex items-center gap-3 p-4">
            <div className="flex-shrink-0">
              <svg
                className="w-6 h-6 text-orange-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M15.536 8.464a5 5 0 010 7.072m0 0l-2.829-2.829m-4.243 2.829a4.978 4.978 0 01-1.414-2.83m-1.414 5.658a9 9 0 01-2.167-9.238m7.824 2.167a1 1 0 111.414 1.414m-1.414-1.414L3 3m8.293 8.293l1.414 1.414"
                />
              </svg>
            </div>
            <div className="flex-1">
              <p className="font-semibold text-orange-900">Offline Mode</p>
              <p className="text-sm text-orange-700">
                Check-ins will be synced when connection is restored
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Queue Status */}
      {queuedCount > 0 && (
        <Card className="bg-blue-50 border-2 border-blue-300 shadow-lg mb-2">
          <div className="flex items-center gap-3 p-4">
            <div className="flex-shrink-0">
              <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center">
                <span className="text-white font-bold text-lg">{queuedCount}</span>
              </div>
            </div>
            <div className="flex-1">
              <p className="font-semibold text-blue-900">
                {queuedCount} {queuedCount === 1 ? 'check-in' : 'check-ins'} queued
              </p>
              <p className="text-sm text-blue-700">
                {mode === 'offline'
                  ? 'Will sync when online'
                  : 'Ready to sync'}
              </p>
            </div>
            {mode === 'online' && onSync && (
              <button
                onClick={onSync}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors text-sm"
              >
                Sync Now
              </button>
            )}
          </div>
        </Card>
      )}

      {/* Sync Status */}
      {syncStatus === 'syncing' && (
        <Card className="bg-purple-50 border-2 border-purple-300 shadow-lg">
          <div className="flex items-center gap-3 p-4">
            <div className="flex-shrink-0">
              <div className="w-6 h-6 border-4 border-purple-600 border-t-transparent rounded-full animate-spin" />
            </div>
            <div>
              <p className="font-semibold text-purple-900">Syncing check-ins...</p>
              <p className="text-sm text-purple-700">Please wait</p>
            </div>
          </div>
        </Card>
      )}

      {/* Sync Success */}
      {syncStatus === 'success' && lastSyncResults && lastSyncResults.length > 0 && (
        <Card className="bg-green-50 border-2 border-green-300 shadow-lg">
          <div className="flex items-center gap-3 p-4">
            <div className="flex-shrink-0">
              <svg
                className="w-6 h-6 text-green-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <div>
              <p className="font-semibold text-green-900">Sync complete!</p>
              <p className="text-sm text-green-700">
                {lastSyncResults.filter(r => r.success).length} check-ins synced
                {lastSyncResults.some(r => r.isDuplicate) &&
                  ' (some were already checked in)'}
              </p>
            </div>
          </div>
        </Card>
      )}

      {/* Sync Error */}
      {syncStatus === 'error' && (
        <Card className="bg-red-50 border-2 border-red-300 shadow-lg">
          <div className="flex items-center gap-3 p-4">
            <div className="flex-shrink-0">
              <svg
                className="w-6 h-6 text-red-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div className="flex-1">
              <p className="font-semibold text-red-900">Sync failed</p>
              <p className="text-sm text-red-700">
                {lastSyncResults?.some(r => !r.success)
                  ? 'Some check-ins could not be synced. Will retry automatically.'
                  : 'Unable to sync. Will retry when connection improves.'}
              </p>
            </div>
            {onSync && (
              <button
                onClick={onSync}
                className="px-4 py-2 bg-red-600 text-white rounded-lg font-medium hover:bg-red-700 transition-colors text-sm"
              >
                Retry
              </button>
            )}
          </div>
        </Card>
      )}
    </div>
  );
}
