import { useState } from 'react';
import { Button, Card } from '@/components/ui';
import { SecurityCodeVerify } from './SecurityCodeVerify';
import type { AttendanceResultDto } from '@/services/api/types';

export interface CheckoutFlowProps {
  currentAttendance: AttendanceResultDto[];
  onCheckout: (attendanceIdKey: string) => Promise<void>;
}

type CheckoutStep = 'list' | 'verify' | 'success';

interface SelectedAttendance {
  attendanceIdKey: string;
  personName: string;
  securityCode: string;
  locationName: string;
  scheduleName: string;
}

/**
 * Checkout flow for recording when children leave
 * Requires security code verification before checkout
 */
export function CheckoutFlow({ currentAttendance, onCheckout }: CheckoutFlowProps) {
  const [step, setStep] = useState<CheckoutStep>('list');
  const [selectedAttendance, setSelectedAttendance] = useState<SelectedAttendance | null>(null);
  const [checkedOutName, setCheckedOutName] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [checkingOut, setCheckingOut] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Filter attendance by search query
  const filteredAttendance = currentAttendance.filter((attendance) => {
    if (!searchQuery) return true;

    const query = searchQuery.toLowerCase();
    return (
      attendance.personName.toLowerCase().includes(query) ||
      attendance.securityCode.includes(query)
    );
  });

  const handleSelectAttendance = (attendance: AttendanceResultDto) => {
    setSelectedAttendance({
      attendanceIdKey: attendance.attendanceIdKey,
      personName: attendance.personName,
      securityCode: attendance.securityCode,
      locationName: attendance.locationName,
      scheduleName: attendance.scheduleName,
    });
    setStep('verify');
    setError(null);
  };

  const handleVerified = async () => {
    if (!selectedAttendance) return;

    setCheckingOut(true);
    setError(null);

    try {
      await onCheckout(selectedAttendance.attendanceIdKey);
      // Store name before clearing selectedAttendance
      setCheckedOutName(selectedAttendance.personName);
      setStep('success');
    } catch {
      setError('Failed to record checkout. Please try again.');
      setStep('list');
    } finally {
      setCheckingOut(false);
      setSelectedAttendance(null);
    }
  };

  const handleCancel = () => {
    setStep('list');
    setSelectedAttendance(null);
    setError(null);
  };

  const handleContinue = () => {
    setStep('list');
    setSearchQuery('');
    setError(null);
    setCheckedOutName(null);
  };

  // List view - select person to checkout
  if (step === 'list') {
    return (
      <div className="space-y-6">
        <div>
          <h3 className="text-2xl font-bold text-gray-900 mb-2">Checkout</h3>
          <p className="text-gray-600">
            Select a person to check out. Security code verification required.
          </p>
        </div>

        {/* Error Message */}
        {error && (
          <Card className="bg-red-50 border border-red-200 p-4">
            <p className="text-red-800 text-center">{error}</p>
          </Card>
        )}

        {/* Search */}
        <div>
          <input
            type="text"
            placeholder="Search by name or security code..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full px-4 py-3 text-lg border-2 border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          />
        </div>

        {/* Attendance List */}
        <Card className="p-6">
          {filteredAttendance.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-600 text-lg">
                {searchQuery
                  ? 'No matching check-ins found'
                  : 'No one is currently checked in'}
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {filteredAttendance.map((attendance) => (
                <div
                  key={attendance.attendanceIdKey}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors"
                >
                  <div className="flex-1">
                    <p className="font-semibold text-lg text-gray-900">
                      {attendance.personName}
                    </p>
                    <p className="text-sm text-gray-600">
                      {attendance.locationName} - {attendance.scheduleName}
                    </p>
                    <p className="text-sm text-gray-500 mt-1">
                      Security Code: <span className="font-mono font-semibold">{attendance.securityCode.slice(0, 2)}••</span>
                      {' • '}
                      Checked in at{' '}
                      {new Date(attendance.checkInTime).toLocaleTimeString([], {
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </p>
                    {attendance.isFirstTime && (
                      <span className="inline-block mt-2 px-2 py-1 text-xs font-semibold bg-green-100 text-green-800 rounded">
                        First Time
                      </span>
                    )}
                  </div>

                  <Button
                    onClick={() => handleSelectAttendance(attendance)}
                    variant="primary"
                    size="lg"
                    className="ml-4"
                  >
                    Check Out
                  </Button>
                </div>
              ))}
            </div>
          )}
        </Card>

        {/* Info Box */}
        <Card className="p-4 bg-blue-50 border border-blue-200">
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 w-6 h-6 bg-blue-600 rounded-full flex items-center justify-center text-white text-sm font-bold">
              i
            </div>
            <div className="flex-1">
              <p className="text-sm text-blue-900 font-medium">Checkout Process</p>
              <p className="text-sm text-blue-800 mt-1">
                Select a person to check out, then verify the security code from their label.
                All checkouts are timestamped and logged for audit purposes.
              </p>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  // Verify step - enter security code
  if (step === 'verify' && selectedAttendance) {
    return (
      <SecurityCodeVerify
        expectedCode={selectedAttendance.securityCode}
        personName={selectedAttendance.personName}
        onVerified={handleVerified}
        onCancel={handleCancel}
        loading={checkingOut}
      />
    );
  }

  // Success step
  if (step === 'success') {
    return (
      <Card className="max-w-2xl mx-auto p-8">
        <div className="text-center">
          {/* Success Icon */}
          <div className="mb-6 flex justify-center">
            <div className="w-20 h-20 bg-green-100 rounded-full flex items-center justify-center">
              <svg
                className="w-12 h-12 text-green-600"
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
          </div>

          <h2 className="text-3xl font-bold text-gray-900 mb-2">Checkout Complete!</h2>
          <p className="text-lg text-gray-600 mb-8">
            {checkedOutName} has been successfully checked out.
          </p>

          <Button onClick={handleContinue} size="lg" className="w-full">
            Continue
          </Button>
        </div>
      </Card>
    );
  }

  return null;
}
