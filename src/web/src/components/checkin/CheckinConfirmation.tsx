import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import type { AttendanceResultDto } from '@/services/api/types';

export interface CheckinConfirmationProps {
  attendances: AttendanceResultDto[];
  onDone: () => void;
  onPrintLabels?: () => void;
  printStatus?: 'idle' | 'printing' | 'success' | 'error';
  printError?: string | null;
  printerAvailable?: boolean;
}

/**
 * Show results after check-in with security codes
 */
export function CheckinConfirmation({
  attendances,
  onDone,
  onPrintLabels,
  printStatus = 'idle',
  printError = null,
  printerAvailable = false,
}: CheckinConfirmationProps) {
  return (
    <div className="max-w-4xl mx-auto">
      <div className="text-center mb-8">
        <div className="inline-flex items-center justify-center w-20 h-20 bg-green-100 rounded-full mb-4">
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
        <h2 className="text-4xl font-bold text-gray-900 mb-2">
          Check-In Complete!
        </h2>
        <p className="text-xl text-gray-600">
          {attendances.length === 1
            ? '1 person checked in'
            : `${attendances.length} people checked in`}
        </p>
      </div>

      {/* Attendance Cards */}
      <div className="space-y-4 mb-8">
        {attendances.map((attendance) => (
          <Card key={attendance.attendanceIdKey}>
            <div className="flex items-center justify-between">
              <div className="flex-1">
                <h3 className="text-2xl font-bold text-gray-900 mb-1">
                  {attendance.personName}
                </h3>
                <p className="text-gray-600 mb-2">
                  {attendance.groupName} • {attendance.locationName}
                </p>
                {attendance.isFirstTime && (
                  <span className="inline-block bg-yellow-100 text-yellow-800 px-3 py-1 rounded-full text-sm font-medium">
                    First Time Guest
                  </span>
                )}
              </div>
              <div className="text-center">
                <p className="text-sm text-gray-600 mb-1">Security Code</p>
                <p className="text-4xl font-bold font-mono text-blue-600">
                  {attendance.securityCode}
                </p>
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Print Status */}
      {printStatus === 'printing' && (
        <div className="mb-4">
          <Card className="bg-blue-50 border-blue-200">
            <div className="flex items-center gap-3">
              <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600"></div>
              <p className="text-blue-800 text-sm">Printing labels...</p>
            </div>
          </Card>
        </div>
      )}

      {printStatus === 'success' && (
        <div className="mb-4">
          <Card className="bg-green-50 border-green-200">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              <p className="text-green-800 text-sm font-medium">Labels printed successfully!</p>
            </div>
          </Card>
        </div>
      )}

      {printStatus === 'error' && printError && (
        <div className="mb-4">
          <Card className="bg-red-50 border-red-200">
            <div className="flex items-center gap-2">
              <svg className="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
              <div>
                <p className="text-red-800 text-sm font-medium">Print failed</p>
                <p className="text-red-700 text-xs">{printError}</p>
              </div>
            </div>
          </Card>
        </div>
      )}

      {/* Action Buttons */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {onPrintLabels && (
          <Button
            onClick={onPrintLabels}
            variant="secondary"
            size="lg"
            className="text-xl"
            disabled={printStatus === 'printing'}
            loading={printStatus === 'printing'}
          >
            {printStatus === 'success' ? 'Reprint Labels' : 'Print Labels'}
          </Button>
        )}
        <Button
          onClick={onDone}
          size="lg"
          className={`text-xl ${!onPrintLabels ? 'md:col-span-2' : ''}`}
        >
          Done
        </Button>
      </div>

      {/* Instructions */}
      <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h4 className="font-semibold text-blue-900 mb-2">Important:</h4>
        <ul className="text-blue-800 space-y-1">
          <li>• Keep your security code to pick up your child</li>
          <li>• Present your code at the check-out desk</li>
          {printerAvailable && printStatus === 'success' && (
            <li>• Collect your printed labels from the printer</li>
          )}
          {!printerAvailable && (
            <li>• Labels can be printed later from the welcome desk</li>
          )}
        </ul>
      </div>
    </div>
  );
}
