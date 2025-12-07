import { useState } from 'react';
import { Button, Card } from '@/components/ui';
import { CheckoutFlow } from './CheckoutFlow';
import type { SupervisorInfoDto, AttendanceResultDto, LabelDto } from '@/services/api/types';

export interface SupervisorModeProps {
  supervisor: SupervisorInfoDto;
  currentAttendance: AttendanceResultDto[];
  onReprint: (attendanceIdKey: string) => Promise<LabelDto[]>;
  onCheckout: (attendanceIdKey: string) => Promise<void>;
  onExit: () => void;
  printLabel?: (labels: LabelDto[]) => Promise<void>;
}

type TabType = 'reprint' | 'checkout';

/**
 * Supervisor mode panel with administrative functions
 */
export function SupervisorMode({
  supervisor,
  currentAttendance,
  onReprint,
  onCheckout,
  onExit,
  printLabel,
}: SupervisorModeProps) {
  const [activeTab, setActiveTab] = useState<TabType>('reprint');
  const [reprintingId, setReprintingId] = useState<string | null>(null);
  const [reprintError, setReprintError] = useState<string | null>(null);

  const handleReprint = async (attendanceIdKey: string) => {
    setReprintingId(attendanceIdKey);
    setReprintError(null);

    try {
      const labels = await onReprint(attendanceIdKey);

      // If print function provided, print the labels
      if (printLabel && labels.length > 0) {
        await printLabel(labels);
      }
    } catch {
      setReprintError('Failed to reprint label. Please try again.');
    } finally {
      setReprintingId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Supervisor Mode</h2>
          <p className="text-gray-600">Logged in as {supervisor.fullName}</p>
        </div>
        <Button onClick={onExit} variant="secondary" size="lg">
          Exit Supervisor Mode
        </Button>
      </div>

      {/* Tab Navigation */}
      <div className="flex gap-2 border-b border-gray-200">
        <button
          onClick={() => setActiveTab('reprint')}
          className={`px-6 py-3 text-lg font-semibold border-b-2 transition-colors ${
            activeTab === 'reprint'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-600 hover:text-gray-900'
          }`}
        >
          Reprint Labels
        </button>
        <button
          onClick={() => setActiveTab('checkout')}
          className={`px-6 py-3 text-lg font-semibold border-b-2 transition-colors ${
            activeTab === 'checkout'
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-600 hover:text-gray-900'
          }`}
        >
          Checkout
        </button>
      </div>

      {/* Tab Content */}
      {activeTab === 'reprint' && (
        <>
          {/* Reprint Error */}
          {reprintError && (
            <Card className="bg-red-50 border border-red-200">
              <p className="text-red-800 text-center">{reprintError}</p>
            </Card>
          )}

          {/* Current Attendance List */}
          <Card className="p-6">
            <h3 className="text-xl font-semibold mb-4 text-gray-900">
              Current Check-Ins
            </h3>

            {currentAttendance.length === 0 ? (
              <p className="text-gray-600 text-center py-8">
                No one is currently checked in
              </p>
            ) : (
              <div className="space-y-3">
                {currentAttendance.map((attendance) => (
                  <div
                    key={attendance.attendanceIdKey}
                    className="flex items-center justify-between p-4 bg-gray-50 rounded-lg"
                  >
                    <div className="flex-1">
                      <p className="font-semibold text-gray-900">
                        {attendance.personName}
                      </p>
                      <p className="text-sm text-gray-600">
                        {attendance.locationName} - {attendance.scheduleName}
                      </p>
                      <p className="text-sm text-gray-500">
                        Code: {attendance.securityCode} • Checked in at{' '}
                        {new Date(attendance.checkInTime).toLocaleTimeString([], {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </p>
                      {attendance.isFirstTime && (
                        <span className="inline-block mt-1 px-2 py-1 text-xs font-semibold bg-green-100 text-green-800 rounded">
                          First Time
                        </span>
                      )}
                    </div>

                    <Button
                      onClick={() => handleReprint(attendance.attendanceIdKey)}
                      disabled={reprintingId === attendance.attendanceIdKey}
                      loading={reprintingId === attendance.attendanceIdKey}
                      variant="secondary"
                      className="ml-4"
                    >
                      Reprint Label
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      )}

      {activeTab === 'checkout' && (
        <CheckoutFlow
          currentAttendance={currentAttendance}
          onCheckout={onCheckout}
        />
      )}

      {/* Additional Info */}
      <Card className="p-4 bg-yellow-50 border border-yellow-200">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0 w-6 h-6 bg-yellow-600 rounded-full flex items-center justify-center text-white text-sm font-bold">
            !
          </div>
          <div className="flex-1">
            <p className="text-sm text-yellow-900 font-medium">
              Supervisor Session Active
            </p>
            <p className="text-sm text-yellow-800 mt-1">
              This session will automatically expire after 2 minutes of inactivity.
              All supervisor actions are logged for audit purposes.
            </p>
          </div>
        </div>
      </Card>
    </div>
  );
}
