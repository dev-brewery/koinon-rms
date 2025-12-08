/**
 * ChildCard Component
 * Displays a single child's information on the roster
 */

import { useState } from 'react';
import type { RosterChildDto } from '@/services/api/types';
import { useCheckOutFromRoster } from '@/hooks/useRoomRoster';
import { Card, Button } from '@/components/ui';

interface ChildCardProps {
  child: RosterChildDto;
}

export function ChildCard({ child }: ChildCardProps) {
  const [showDetails, setShowDetails] = useState(false);
  const [isConfirming, setIsConfirming] = useState(false);
  const checkOutMutation = useCheckOutFromRoster();

  const handleCheckOut = () => {
    if (!isConfirming) {
      setIsConfirming(true);
      return;
    }
    setIsConfirming(false);
    checkOutMutation.mutate(child.attendanceIdKey);
  };

  const handleCancelCheckOut = () => {
    setIsConfirming(false);
  };

  const checkInTime = new Date(child.checkInTime);
  const timeString = checkInTime.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });

  return (
    <Card
      className={`p-4 ${
        child.hasCriticalAllergies ? 'border-l-4 border-l-red-500' : ''
      }`}
    >
      <div className="flex items-start gap-4">
        {/* Photo placeholder */}
        <div className="flex-shrink-0">
          {child.photoUrl ? (
            <img
              src={child.photoUrl}
              alt={child.fullName}
              className="w-16 h-16 rounded-full object-cover"
            />
          ) : (
            <div className="w-16 h-16 rounded-full bg-gray-200 flex items-center justify-center">
              <svg
                className="w-8 h-8 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </div>
          )}
        </div>

        {/* Child info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">
                {child.nickName || child.firstName} {child.lastName}
                {child.isFirstTime && (
                  <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                    First Time
                  </span>
                )}
              </h3>
              <div className="flex items-center gap-4 text-sm text-gray-600 mt-1">
                {child.age && <span>Age {child.age}</span>}
                {child.grade && <span>{child.grade}</span>}
                {child.securityCode && (
                  <span className="font-mono font-semibold">{child.securityCode}</span>
                )}
                <span className="text-gray-500">Checked in at {timeString}</span>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              <button
                onClick={() => setShowDetails(!showDetails)}
                className="text-gray-400 hover:text-gray-600"
                title="Toggle details"
              >
                <svg
                  className={`w-5 h-5 transform transition-transform ${
                    showDetails ? 'rotate-180' : ''
                  }`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M19 9l-7 7-7-7"
                  />
                </svg>
              </button>

              {isConfirming ? (
                <>
                  <Button
                    onClick={handleCheckOut}
                    variant="primary"
                    size="sm"
                    disabled={checkOutMutation.isPending}
                  >
                    {checkOutMutation.isPending ? 'Checking out...' : 'Confirm'}
                  </Button>
                  <Button
                    onClick={handleCancelCheckOut}
                    variant="secondary"
                    size="sm"
                    disabled={checkOutMutation.isPending}
                  >
                    Cancel
                  </Button>
                </>
              ) : (
                <Button
                  onClick={handleCheckOut}
                  variant="secondary"
                  size="sm"
                  disabled={checkOutMutation.isPending}
                >
                  Check Out
                </Button>
              )}
            </div>
          </div>

          {/* Allergies - always visible if present */}
          {child.allergies && (
            <div
              className={`mt-2 p-2 rounded ${
                child.hasCriticalAllergies
                  ? 'bg-red-50 border border-red-200'
                  : 'bg-yellow-50 border border-yellow-200'
              }`}
            >
              <div className="flex items-start gap-2">
                <svg
                  className={`w-5 h-5 flex-shrink-0 ${
                    child.hasCriticalAllergies ? 'text-red-600' : 'text-yellow-600'
                  }`}
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
                <div className="flex-1">
                  <p
                    className={`text-sm font-medium ${
                      child.hasCriticalAllergies ? 'text-red-900' : 'text-yellow-900'
                    }`}
                  >
                    {child.hasCriticalAllergies ? 'CRITICAL ALLERGIES' : 'Allergies'}
                  </p>
                  <p
                    className={`text-sm ${
                      child.hasCriticalAllergies ? 'text-red-800' : 'text-yellow-800'
                    }`}
                  >
                    {child.allergies}
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Expandable details */}
          {showDetails && (
            <div className="mt-4 pt-4 border-t border-gray-200 space-y-3">
              {/* Special needs */}
              {child.specialNeeds && (
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase">
                    Special Needs
                  </p>
                  <p className="text-sm text-gray-700 mt-1">{child.specialNeeds}</p>
                </div>
              )}

              {/* Parent info */}
              {child.parentName && (
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase">
                    Parent Contact
                  </p>
                  <div className="text-sm text-gray-700 mt-1">
                    <p>{child.parentName}</p>
                    {child.parentMobilePhone && (
                      <a
                        href={`tel:${child.parentMobilePhone}`}
                        className="text-indigo-600 hover:text-indigo-800"
                      >
                        {child.parentMobilePhone}
                      </a>
                    )}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
