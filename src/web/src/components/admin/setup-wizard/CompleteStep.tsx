/**
 * Complete Step
 * Step 6: Celebration + quick links
 */

import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/Button';

interface CompleteStepProps {
  campusName: string | null;
  locationNames: string[];
  scheduleName: string | null;
}

interface QuickLinkProps {
  to: string;
  icon: React.ReactNode;
  label: string;
  description: string;
}

function QuickLink({ to, icon, label, description }: QuickLinkProps) {
  const navigate = useNavigate();
  return (
    <button
      type="button"
      onClick={() => navigate(to)}
      className="flex items-start gap-3 p-4 bg-white border border-gray-200 rounded-lg hover:shadow-md transition-shadow text-left w-full"
    >
      <div className="flex-shrink-0 mt-0.5">{icon}</div>
      <div>
        <p className="text-sm font-semibold text-gray-900">{label}</p>
        <p className="text-xs text-gray-500 mt-0.5">{description}</p>
      </div>
    </button>
  );
}

export function CompleteStep({ campusName, locationNames, scheduleName }: CompleteStepProps) {
  const navigate = useNavigate();

  const summaryItems: string[] = [];
  if (campusName) summaryItems.push(`Campus: ${campusName}`);
  if (locationNames.length > 0) summaryItems.push(`Rooms: ${locationNames.join(', ')}`);
  if (scheduleName) summaryItems.push(`Schedule: ${scheduleName}`);

  return (
    <div className="space-y-8 text-center">
      {/* Celebration */}
      <div>
        <div className="mx-auto w-20 h-20 bg-green-100 rounded-full flex items-center justify-center mb-4">
          <svg className="w-10 h-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold text-gray-900">You're all set!</h2>
        <p className="mt-2 text-gray-600">
          Koinon RMS is configured and ready to use. Here's what was set up during this wizard.
        </p>
      </div>

      {/* Summary */}
      {summaryItems.length > 0 && (
        <div className="text-left bg-gray-50 border border-gray-200 rounded-lg p-4">
          <h3 className="text-sm font-semibold text-gray-700 mb-2">Setup summary:</h3>
          <ul className="space-y-1">
            {summaryItems.map(item => (
              <li key={item} className="flex items-center gap-2 text-sm text-gray-800">
                <svg className="w-4 h-4 text-green-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
                {item}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Quick links */}
      <div className="text-left">
        <h3 className="text-sm font-semibold text-gray-700 mb-3">What would you like to do next?</h3>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <QuickLink
            to="/admin"
            icon={
              <svg className="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            }
            label="Dashboard"
            description="View overview and recent activity"
          />
          <QuickLink
            to="/admin/settings/locations"
            icon={
              <svg className="w-5 h-5 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            }
            label="Configure Check-in"
            description="Assign groups to rooms"
          />
          <QuickLink
            to="/admin/people/new"
            icon={
              <svg className="w-5 h-5 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
              </svg>
            }
            label="Add People"
            description="Start adding church members"
          />
        </div>
      </div>

      <Button
        onClick={() => navigate('/admin')}
        size="lg"
        className="w-full sm:w-auto"
      >
        Go to Dashboard
      </Button>
    </div>
  );
}
