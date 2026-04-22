import type { CheckinOperationsRoomDto } from '@/services/api/checkinOperations';
import { RoomCard } from './RoomCard';

interface RoomListProps {
  rooms: CheckinOperationsRoomDto[];
  onToggle: (locationIdKey: string) => void;
  togglingLocationIdKey?: string;
}

export function RoomList({ rooms, onToggle, togglingLocationIdKey }: RoomListProps) {
  if (rooms.length === 0) {
    return (
      <div
        data-testid="checkin-ops-room-list"
        className="rounded-lg border border-dashed border-gray-300 bg-white p-8 text-center text-sm text-gray-500"
      >
        No check-in rooms configured. Add capacity to a location to see it here.
      </div>
    );
  }

  return (
    <div
      data-testid="checkin-ops-room-list"
      className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3"
    >
      {rooms.map((room) => (
        <RoomCard
          key={room.locationIdKey}
          room={room}
          onToggle={onToggle}
          isToggling={togglingLocationIdKey === room.locationIdKey}
        />
      ))}
    </div>
  );
}
