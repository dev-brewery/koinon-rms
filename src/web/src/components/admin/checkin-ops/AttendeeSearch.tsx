import { useMemo, useState } from 'react';
import type { CheckinOperationsAttendeeDto } from '@/services/api/checkinOperations';

interface AttendeeSearchProps {
  attendees: CheckinOperationsAttendeeDto[];
}

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString([], {
      hour: 'numeric',
      minute: '2-digit',
    });
  } catch {
    return iso;
  }
}

export function AttendeeSearch({ attendees }: AttendeeSearchProps) {
  const [query, setQuery] = useState('');

  const filtered = useMemo(() => {
    const trimmed = query.trim().toLowerCase();
    if (!trimmed) return attendees;
    return attendees.filter(
      (a) =>
        a.fullName.toLowerCase().includes(trimmed) ||
        a.locationName.toLowerCase().includes(trimmed)
    );
  }, [attendees, query]);

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
      <div className="flex items-center justify-between gap-4">
        <h2 className="text-sm font-semibold text-gray-900">Checked-in attendees</h2>
        <input
          type="search"
          data-testid="checkin-ops-search"
          placeholder="Search by name or room..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="w-full max-w-sm rounded-md border border-gray-300 px-3 py-1.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
        />
      </div>

      {filtered.length === 0 ? (
        <p
          data-testid="checkin-ops-attendee-empty"
          className="py-6 text-center text-sm text-gray-500"
        >
          {attendees.length === 0
            ? 'No check-ins yet today.'
            : 'No attendees match your search.'}
        </p>
      ) : (
        <ul className="divide-y divide-gray-100">
          {filtered.map((attendee) => (
            <li
              key={attendee.attendanceIdKey}
              data-testid="checkin-ops-attendee-row"
              data-person-idkey={attendee.personIdKey}
              data-is-present={attendee.isPresent ? 'true' : 'false'}
              className="flex items-center justify-between gap-4 py-2 text-sm"
            >
              <div>
                <p className="font-medium text-gray-900">{attendee.fullName}</p>
                <p className="text-xs text-gray-500">{attendee.locationName}</p>
              </div>
              <div className="text-right text-xs text-gray-500">
                <p>In: {formatTime(attendee.checkInTime)}</p>
                {attendee.isPresent ? (
                  <span className="inline-block rounded-full bg-green-100 px-2 py-0.5 text-green-700">
                    Present
                  </span>
                ) : (
                  <span className="inline-block rounded-full bg-gray-100 px-2 py-0.5 text-gray-600">
                    Checked out
                  </span>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
