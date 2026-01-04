/**
 * SessionsSettingsSection
 * View and manage active sessions
 */

import { useState } from 'react';
import { useSessions, useRevokeSession } from '@/hooks/useSettings';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Loading } from '@/components/ui/Loading';
import { ErrorState } from '@/components/ui/ErrorState';
import { EmptyState } from '@/components/ui/EmptyState';
import { ConfirmDialog } from '@/components/ui/ConfirmDialog';
import { formatDateTime } from '@/lib/utils';

export function SessionsSettingsSection() {
  const { data: sessions, isLoading, error } = useSessions();
  const revokeSession = useRevokeSession();

  const [sessionToRevoke, setSessionToRevoke] = useState<string | null>(null);

  const handleRevokeSession = async (idKey: string) => {
    try {
      await revokeSession.mutateAsync(idKey);
      setSessionToRevoke(null);
    } catch (error) {
      // Error handled by mutation
    }
  };

  if (isLoading) {
    return <Loading />;
  }

  if (error) {
    return <ErrorState title="Error" message="Failed to load active sessions" />;
  }

  if (!sessions || sessions.length === 0) {
    return (
      <Card>
        <EmptyState
          title="No active sessions"
          description="You don't have any active sessions."
        />
      </Card>
    );
  }

  return (
    <>
      <div className="space-y-4">
        <div className="mb-4">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">Active Sessions</h3>
          <p className="text-sm text-gray-600">
            Manage devices and browsers that are signed into your account
          </p>
        </div>

        {sessions.map((session) => (
          <Card key={session.idKey}>
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1">
                {/* Device Icon and Info */}
                <div className="flex items-start gap-3">
                  <div className="flex-shrink-0 mt-1">
                    <svg className="w-6 h-6 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                      />
                    </svg>
                  </div>
                  
                  <div className="flex-1 min-w-0">
                    {/* Device Info */}
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="text-base font-medium text-gray-900">
                        {session.deviceInfo}
                      </h4>
                      {session.isCurrentSession && (
                        <span className="inline-flex items-center px-2 py-1 text-xs font-medium text-green-700 bg-green-100 rounded-full">
                          Current session
                        </span>
                      )}
                    </div>

                    {/* Location and IP */}
                    <div className="space-y-1">
                      <p className="text-sm text-gray-600">
                        <span className="font-medium">IP Address:</span> {session.ipAddress}
                      </p>
                      {session.location && (
                        <p className="text-sm text-gray-600">
                          <span className="font-medium">Location:</span> {session.location}
                        </p>
                      )}
                      <p className="text-sm text-gray-500">
                        Last active: {formatDateTime(session.lastActivityAt)}
                      </p>
                      <p className="text-sm text-gray-500">
                        Signed in: {formatDateTime(session.createdDateTime)}
                      </p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Revoke Button */}
              {!session.isCurrentSession && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setSessionToRevoke(session.idKey)}
                  disabled={revokeSession.isPending}
                >
                  Revoke
                </Button>
              )}
            </div>
          </Card>
        ))}
      </div>

      {/* Confirm Revoke Dialog */}
      {sessionToRevoke && (
        <ConfirmDialog
          isOpen={!!sessionToRevoke}
          onClose={() => setSessionToRevoke(null)}
          onConfirm={() => handleRevokeSession(sessionToRevoke)}
          title="Revoke Session"
          description="Are you sure you want to revoke this session? The device will be signed out immediately."
          confirmLabel="Revoke"
          variant="danger"
          isLoading={revokeSession.isPending}
        />
      )}

      {revokeSession.isError && (
        <div className="mt-4">
          <p className="text-sm text-red-600">
            Failed to revoke session. Please try again.
          </p>
        </div>
      )}
    </>
  );
}
