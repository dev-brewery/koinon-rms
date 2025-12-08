import { useMemo } from 'react';
import { CapacityStatus } from '@/services/api/types';

export interface RoomCapacityIndicatorProps {
  currentCount: number;
  softCapacity?: number;
  hardCapacity?: number;
  status: CapacityStatus;
  percentageFull?: number;
  locationName: string;
  showDetails?: boolean;
  compact?: boolean;
}

export function RoomCapacityIndicator({
  currentCount,
  softCapacity,
  hardCapacity,
  status,
  percentageFull,
  locationName,
  showDetails = true,
  compact = false,
}: RoomCapacityIndicatorProps) {
  const { bgColor, textColor, borderColor, icon, statusText } = useMemo(() => {
    switch (status) {
      case CapacityStatus.Full:
        return {
          bgColor: 'bg-red-100',
          textColor: 'text-red-800',
          borderColor: 'border-red-300',
          icon: '🔴',
          statusText: 'FULL',
        };
      case CapacityStatus.Warning:
        return {
          bgColor: 'bg-yellow-100',
          textColor: 'text-yellow-800',
          borderColor: 'border-yellow-300',
          icon: '⚠️',
          statusText: 'WARNING',
        };
      default:
        return {
          bgColor: 'bg-green-100',
          textColor: 'text-green-800',
          borderColor: 'border-green-300',
          icon: '✅',
          statusText: 'AVAILABLE',
        };
    }
  }, [status]);

  const capacityText = useMemo(() => {
    const capacity = hardCapacity ?? softCapacity;
    if (!capacity) {
      return currentCount + ' checked in';
    }
    return currentCount + ' / ' + capacity;
  }, [currentCount, softCapacity, hardCapacity]);

  if (compact) {
    return (
      <div className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-sm font-semibold ${bgColor} ${textColor}`}>
        <span className="text-xs">{icon}</span>
        {showDetails && <span>{capacityText}</span>}
      </div>
    );
  }

  return (
    <div className={`p-4 rounded-lg border-2 ${bgColor} ${borderColor}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-2xl">{icon}</span>
            <h4 className={`text-lg font-bold ${textColor}`}>{statusText}</h4>
          </div>
          {showDetails && (
            <>
              <p className="text-sm text-gray-700 mb-1">{locationName}</p>
              <div className="flex items-center gap-3">
                <p className={`text-2xl font-bold ${textColor}`}>{capacityText}</p>
                {percentageFull !== undefined && (
                  <p className="text-sm text-gray-600">({percentageFull}%)</p>
                )}
              </div>
            </>
          )}
        </div>

        {softCapacity && showDetails && (
          <div className="w-24">
            <div className="h-2 bg-gray-200 rounded-full overflow-hidden">
              <div
                className={
                  status === CapacityStatus.Full
                    ? 'h-full transition-all duration-300 bg-red-600'
                    : status === CapacityStatus.Warning
                    ? 'h-full transition-all duration-300 bg-yellow-500'
                    : 'h-full transition-all duration-300 bg-green-500'
                }
                style={{ width: Math.min((currentCount / softCapacity) * 100, 100) + '%' }}
              />
            </div>
            <p className="text-xs text-gray-500 mt-1 text-center">
              {Math.round((currentCount / softCapacity) * 100)}%
            </p>
          </div>
        )}
      </div>

      {status === CapacityStatus.Full && (
        <div className="mt-3 pt-3 border-t border-red-200">
          <p className="text-sm text-red-700">
            <strong>Room Full:</strong> Please select an overflow room or contact a supervisor.
          </p>
        </div>
      )}

      {status === CapacityStatus.Warning && (
        <div className="mt-3 pt-3 border-t border-yellow-200">
          <p className="text-sm text-yellow-700">
            <strong>Near Capacity:</strong> Room is approaching capacity limit.
          </p>
        </div>
      )}
    </div>
  );
}

export interface RoomCapacityBadgeProps {
  status: CapacityStatus;
  currentCount: number;
  capacity?: number;
}

export function RoomCapacityBadge({ status, currentCount, capacity }: RoomCapacityBadgeProps) {
  const { bgColor, textColor, icon } = useMemo(() => {
    switch (status) {
      case CapacityStatus.Full:
        return { bgColor: 'bg-red-100', textColor: 'text-red-800', icon: '🔴' };
      case CapacityStatus.Warning:
        return { bgColor: 'bg-yellow-100', textColor: 'text-yellow-800', icon: '⚠️' };
      default:
        return { bgColor: 'bg-green-100', textColor: 'text-green-800', icon: '✅' };
    }
  }, [status]);

  return (
    <span className={`inline-flex items-center gap-1 px-2 py-1 rounded text-xs font-semibold ${bgColor} ${textColor}`}>
      <span>{icon}</span>
      <span>
        {currentCount}
        {capacity && '/' + capacity}
      </span>
    </span>
  );
}
