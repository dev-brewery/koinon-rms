/**
 * ChildRow
 * A single child entry inside a RoomCard — compact row layout
 */

import { useState } from 'react';
import type { RosterChildDto } from '@/services/api/types';
import { useCheckOutFromRoster } from '@/hooks/useRoomRoster';
import { Button } from '@/components/ui';
import { useToast } from '@/contexts/ToastContext';

interface ChildRowProps {
  child: RosterChildDto;
}

export function ChildRow({ child }: ChildRowProps) {
  const [isConfirming, setIsConfirming] = useState(false);
  const { error: toastError } = useToast();
  const checkOutMutation = useCheckOutFromRoster();

  const displayName = child.nickName
    ? `${child.nickName} ${child.lastName}`
    : `${child.firstName} ${child.lastName}`;

  const handleCheckOut = () => {
    if (!isConfirming) {
      setIsConfirming(true);
      return;
    }
    setIsConfirming(false);
    checkOutMutation.mutate(child.attendanceIdKey, {
      onError: () => {
        toastError(
          'Check-out failed',
          `Failed to check out ${displayName}. Please try again.`
        );
      },
    });
  };

  const handleCancel = () => {
    setIsConfirming(false);
  };

  const timeString = new Date(child.checkInTime).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });

  return (
    <div
      className={`flex items-center justify-between gap-2 py-1.5 text-sm ${
        child.hasCriticalAllergies ? 'border-l-2 border-l-red-500 pl-2 -ml-2' : ''
      }`}
      data-testid="child-row"
    >
      {/* Name + metadata */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className="font-medium text-gray-900 truncate">{displayName}</span>

          {child.isFirstTime && (
            <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-700 whitespace-nowrap">
              First Visit
            </span>
          )}

          {child.hasCriticalAllergies && (
            <span
              className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-red-100 text-red-700 whitespace-nowrap"
              title={child.allergies}
            >
              Allergy
            </span>
          )}
        </div>

        <div className="flex items-center gap-2 text-xs text-gray-500 mt-0.5">
          {child.age !== undefined && <span>Age {child.age}</span>}
          {child.securityCode && (
            <span className="font-mono font-semibold text-gray-700">{child.securityCode}</span>
          )}
          <span>{timeString}</span>
        </div>
      </div>

      {/* Checkout actions */}
      <div className="flex items-center gap-1 flex-shrink-0">
        {isConfirming ? (
          <>
            <Button
              onClick={handleCheckOut}
              variant="primary"
              size="sm"
              disabled={checkOutMutation.isPending}
            >
              {checkOutMutation.isPending ? '...' : 'Confirm'}
            </Button>
            <Button
              onClick={handleCancel}
              variant="secondary"
              size="sm"
              disabled={checkOutMutation.isPending}
            >
              No
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
  );
}
