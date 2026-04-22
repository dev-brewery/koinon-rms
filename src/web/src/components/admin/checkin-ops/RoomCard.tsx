import type { CheckinOperationsRoomDto } from '@/services/api/checkinOperations';
import { CapacityPill } from './CapacityPill';

interface RoomCardProps {
  room: CheckinOperationsRoomDto;
  onToggle: (locationIdKey: string) => void;
  isToggling: boolean;
}

export function RoomCard({ room, onToggle, isToggling }: RoomCardProps) {
  return (
    <div
      data-testid="checkin-ops-room-card"
      data-location-idkey={room.locationIdKey}
      data-is-open={room.isOpen ? 'true' : 'false'}
      className="flex flex-col gap-3 rounded-lg border border-gray-200 bg-white p-4 shadow-sm"
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <h3
            className="text-base font-semibold text-gray-900"
            data-testid="checkin-ops-room-name"
          >
            {room.locationName}
          </h3>
          <p className="text-xs text-gray-500">
            {room.isOpen ? 'Open for check-ins' : 'Closed'}
          </p>
        </div>
        <CapacityPill
          color={room.capacityPillColor}
          checkedInCount={room.checkedInCount}
          capacity={room.capacity}
          percentFull={room.percentFull}
          isOpen={room.isOpen}
        />
      </div>

      <div className="flex items-center justify-between">
        <p className="text-2xl font-bold text-gray-900">
          {room.checkedInCount}
          {room.capacity ? (
            <span className="ml-1 text-sm font-normal text-gray-500">
              / {room.capacity}
            </span>
          ) : null}
        </p>

        <button
          type="button"
          data-testid="checkin-ops-room-toggle"
          data-location-idkey={room.locationIdKey}
          onClick={() => onToggle(room.locationIdKey)}
          disabled={isToggling}
          className={`rounded-md border px-3 py-1.5 text-sm font-medium transition-colors ${
            room.isOpen
              ? 'border-red-300 bg-white text-red-700 hover:bg-red-50'
              : 'border-green-300 bg-white text-green-700 hover:bg-green-50'
          } disabled:opacity-50`}
        >
          {room.isOpen ? 'Close room' : 'Open room'}
        </button>
      </div>
    </div>
  );
}
