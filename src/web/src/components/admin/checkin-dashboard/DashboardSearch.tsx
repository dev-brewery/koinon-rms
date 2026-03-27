/**
 * DashboardSearch
 * Search bar + room filter for the check-in operations dashboard
 */

interface DashboardSearchProps {
  searchQuery: string;
  onSearchChange: (value: string) => void;
  selectedRoomIdKey: string;
  onRoomChange: (idKey: string) => void;
  roomOptions: Array<{ idKey: string; name: string }>;
}

export function DashboardSearch({
  searchQuery,
  onSearchChange,
  selectedRoomIdKey,
  onRoomChange,
  roomOptions,
}: DashboardSearchProps) {
  return (
    <div
      className="bg-white rounded-lg border border-gray-200 shadow-sm px-4 py-3 flex flex-col sm:flex-row gap-3"
      data-testid="dashboard-search"
    >
      {/* Child name search */}
      <div className="flex-1 relative">
        <svg
          className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M21 21l-4.35-4.35M17 11A6 6 0 105 11a6 6 0 0012 0z"
          />
        </svg>
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search for a child across all rooms..."
          className="w-full pl-9 pr-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-indigo-500 focus:border-indigo-500"
          data-testid="child-search-input"
        />
        {searchQuery && (
          <button
            type="button"
            onClick={() => onSearchChange('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
            aria-label="Clear search"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      {/* Room filter */}
      <div className="sm:w-52">
        <select
          value={selectedRoomIdKey}
          onChange={(e) => onRoomChange(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:ring-indigo-500 focus:border-indigo-500"
          data-testid="room-filter-select"
        >
          <option value="">All Rooms</option>
          {roomOptions.map((room) => (
            <option key={room.idKey} value={room.idKey}>
              {room.name}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
