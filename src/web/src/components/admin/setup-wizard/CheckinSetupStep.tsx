/**
 * Check-in Setup Step
 * Step 5: Summary of what was created + guidance on next steps
 */

import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/Button';

interface CheckinSetupStepProps {
  campusName: string | null;
  locationNames: string[];
  scheduleName: string | null;
  onNext: () => void;
  onBack: () => void;
}

function SummaryRow({ label, value, empty }: { label: string; value: string | null; empty?: string }) {
  if (!value) {
    return (
      <div className="flex items-start gap-3 py-3 border-b border-gray-100 last:border-0">
        <span className="text-sm font-medium text-gray-500 w-28 flex-shrink-0">{label}</span>
        <span className="text-sm text-gray-400 italic">{empty ?? 'Skipped'}</span>
      </div>
    );
  }
  return (
    <div className="flex items-start gap-3 py-3 border-b border-gray-100 last:border-0">
      <span className="text-sm font-medium text-gray-500 w-28 flex-shrink-0">{label}</span>
      <span className="text-sm text-gray-900">{value}</span>
    </div>
  );
}

export function CheckinSetupStep({
  campusName,
  locationNames,
  scheduleName,
  onNext,
  onBack,
}: CheckinSetupStepProps) {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
            <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900">Check-in Setup Summary</h2>
        </div>
        <p className="text-gray-600">
          Here's what was created. One more thing needed before check-in is fully operational:
          groups must be assigned to rooms.
        </p>
      </div>

      {/* Setup summary */}
      <div className="bg-white border border-gray-200 rounded-lg p-4">
        <h3 className="text-sm font-semibold text-gray-700 mb-2">What was set up:</h3>
        <div>
          <SummaryRow label="Campus" value={campusName} />
          <SummaryRow
            label="Rooms"
            value={locationNames.length > 0 ? locationNames.join(', ') : null}
            empty="No rooms added"
          />
          <SummaryRow label="Schedule" value={scheduleName} empty="No schedule added" />
        </div>
      </div>

      {/* Next step guidance */}
      <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
        <div className="flex gap-3">
          <svg className="w-5 h-5 text-amber-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <div>
            <h3 className="text-sm font-semibold text-amber-900">One more step to enable check-in</h3>
            <p className="text-sm text-amber-800 mt-1">
              Groups (e.g. "Nursery", "K–2nd Grade") need to be assigned to rooms so children
              can be routed to the right location at check-in. You can configure this from the
              Check-in configuration page.
            </p>
            <Link
              to="/admin/settings/locations"
              className="inline-block mt-2 text-sm font-medium text-amber-700 underline hover:text-amber-900"
            >
              Go to Check-in Configuration
            </Link>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex items-center justify-between pt-2">
        <Button type="button" variant="outline" onClick={onBack}>
          Back
        </Button>
        <Button type="button" onClick={onNext}>
          Finish Setup
        </Button>
      </div>
    </div>
  );
}
