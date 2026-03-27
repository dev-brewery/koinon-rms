/**
 * RoomCard
 * A single room card in the check-in dashboard grid.
 * Shows capacity status, child list, and room open/close toggle.
 */

import { useState } from 'react';
import type { RoomRosterDto, CheckinLocationDto } from '@/services/api/types';
import { ChildRow } from './ChildRow';
import { Button } from '@/components/ui';

const VISIBLE_CHILD_LIMIT = 5;

// Map 0–100% to the nearest 10% Tailwind width class.
// All 11 strings are statically present so Tailwind includes them in the build.
const WIDTH_CLASSES: Record<number, string> = {
  0: 'w-0',
  10: 'w-[10%]',
  20: 'w-[20%]',
  30: 'w-[30%]',
  40: 'w-[40%]',
  50: 'w-1/2',
  60: 'w-[60%]',
  70: 'w-[70%]',
  80: 'w-4/5',
  90: 'w-[90%]',
  100: 'w-full',
};

function capacityWidthClass(pct: number): string {
  const step = Math.min(100, Math.round(pct / 10) * 10);
  return WIDTH_CLASSES[step] ?? 'w-0';
}

interface RoomCardProps {
  roster: RoomRosterDto | undefined;
  location: CheckinLocationDto;
  searchQuery: string;
  onToggleRoom: (idKey: string, isActive: boolean) => void;
  isToggling: boolean;
}

function capacityBadgeClasses(isAtCapacity: boolean, isNearCapacity: boolean): string {
  if (isAtCapacity) return 'bg-red-100 text-red-800';
  if (isNearCapacity) return 'bg-yellow-100 text-yellow-800';
  return 'bg-green-100 text-green-800';
}

function capacityBarClasses(isAtCapacity: boolean, isNearCapacity: boolean): string {
  if (isAtCapacity) return 'bg-red-500';
  if (isNearCapacity) return 'bg-yellow-500';
  return 'bg-green-500';
}

export function RoomCard({
  roster,
  location,
  searchQuery,
  onToggleRoom,
  isToggling,
}: RoomCardProps) {
  const [showAll, setShowAll] = useState(false);

  const children = roster?.children ?? [];

  const filteredChildren = searchQuery
    ? children.filter((c) =>
        c.fullName.toLowerCase().includes(searchQuery.toLowerCase())
      )
    : children;

  const visibleChildren =
    showAll || searchQuery
      ? filteredChildren
      : filteredChildren.slice(0, VISIBLE_CHILD_LIMIT);

  const hiddenCount = searchQuery
    ? 0
    : Math.max(0, filteredChildren.length - VISIBLE_CHILD_LIMIT);

  const count = roster?.totalCount ?? 0;
  const capacity = roster?.capacity;
  const isAtCapacity = roster?.isAtCapacity ?? false;
  const isNearCapacity = roster?.isNearCapacity ?? false;

  const capacityPct = capacity ? Math.min((count / capacity) * 100, 100) : 0;

  const isOpen = location.isOpen && location.isActive;

  return (
    <div
      className={`bg-white rounded-lg shadow border p-4 flex flex-col gap-3 ${
        !isOpen ? 'opacity-60' : ''
      }`}
      data-testid="room-card"
      data-location-id={location.idKey}
    >
      {/* Room header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-900 truncate">{location.name}</h3>

          {/* Headcount + capacity badge */}
          <div className="flex items-center gap-2 mt-1">
            <span
              className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold ${capacityBadgeClasses(
                isAtCapacity,
                isNearCapacity
              )}`}
            >
              {capacity ? `${count}/${capacity}` : `${count} checked in`}
            </span>

            {!isOpen && (
              <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
                Closed
              </span>
            )}
          </div>
        </div>

        {/* Room toggle button */}
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onToggleRoom(location.idKey, !location.isActive)}
          disabled={isToggling}
          aria-label={isOpen ? 'Close room' : 'Open room'}
        >
          {isOpen ? 'Close Room' : 'Open Room'}
        </Button>
      </div>

      {/* Capacity bar — width driven by a stepped Tailwind class, no inline styles */}
      {capacity !== undefined && capacity > 0 && (
        <div className="w-full h-1.5 bg-gray-200 rounded-full overflow-hidden">
          <div
            className={`h-1.5 rounded-full transition-all duration-300 ${capacityBarClasses(
              isAtCapacity,
              isNearCapacity
            )} ${capacityWidthClass(capacityPct)}`}
            role="meter"
            aria-valuenow={count}
            aria-valuemin={0}
            aria-valuemax={capacity}
            aria-label={`${count} of ${capacity} capacity`}
          />
        </div>
      )}

      {/* Divider */}
      <hr className="border-gray-100" />

      {/* Children list */}
      <div className="flex flex-col divide-y divide-gray-100 min-h-0">
        {filteredChildren.length === 0 ? (
          <p className="text-sm text-gray-400 py-2">
            {searchQuery ? 'No match in this room' : 'No children checked in'}
          </p>
        ) : (
          <>
            {visibleChildren.map((child) => (
              <ChildRow key={child.attendanceIdKey} child={child} />
            ))}

            {hiddenCount > 0 && !showAll && (
              <button
                type="button"
                className="mt-2 text-xs text-indigo-600 hover:text-indigo-800 text-left py-1"
                onClick={() => setShowAll(true)}
              >
                Show {hiddenCount} more...
              </button>
            )}

            {showAll && filteredChildren.length > VISIBLE_CHILD_LIMIT && (
              <button
                type="button"
                className="mt-2 text-xs text-indigo-600 hover:text-indigo-800 text-left py-1"
                onClick={() => setShowAll(false)}
              >
                Show less
              </button>
            )}
          </>
        )}
      </div>
    </div>
  );
}
