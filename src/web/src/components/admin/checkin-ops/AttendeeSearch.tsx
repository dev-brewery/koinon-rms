import { useMemo, useState } from 'react';
import type { CheckinOperationsAttendeeDto } from '@/services/api/checkinOperations';

interface AttendeeSearchProps {
  attendees: CheckinOperationsAttendeeDto[];
  onCheckout: (attendanceIdKey: string) => void;
  checkingOutIdKey?: string;
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

function AttendeeRow({
  attendee,
  onCheckout,
  isCheckingOut,
}: {
  attendee: CheckinOperationsAttendeeDto;
  onCheckout: (attendanceIdKey: string) => void;
  isCheckingOut: boolean;
}) {
  const [isConfirming, setIsConfirming] = useState(false);

  return (
    <li
      data-testid="checkin-ops-attendee-row"
      data-person-idkey={attendee.personIdKey}
      data-is-present={attendee.isPresent ? 'true' : 'false'}
      className="flex items-center justify-between gap-4 py-2 text-sm"
    >
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-1.5">
          <p className="font-medium text-gray-900">{attendee.fullName}</p>
          {attendee.isFirstTime ? (
            <span
              data-testid="checkin-ops-first-visit"
              className="inline-block rounded-full bg-indigo-100 px-2 py-0.5 text-[11px] font-medium text-indigo-700"
            >
              First visit
            </span>
          ) : null}
          {attendee.securityCode ? (
            <span
              data-testid="checkin-ops-security-code"
              className="rounded bg-gray-100 px-1.5 py-0.5 font-mono text-[11px] text-gray-700"
            >
              {attendee.securityCode}
            </span>
          ) : null}
        </div>
        <p className="text-xs text-gray-500">{attendee.locationName}</p>
        {attendee.allergies ? (
          <p
            data-testid="checkin-ops-allergies"
            title={attendee.allergies}
            className={`mt-0.5 text-[11px] ${
              attendee.hasCriticalAllergies ? 'font-semibold text-red-700' : 'text-amber-700'
            }`}
          >
            ⚠ {attendee.allergies}
          </p>
        ) : null}
      </div>

      <div className="flex flex-col items-end gap-1 text-right text-xs text-gray-500">
        <p>In: {formatTime(attendee.checkInTime)}</p>
        {attendee.isPresent ? (
          isConfirming ? (
            <div className="flex items-center gap-1">
              <button
                type="button"
                data-testid="checkin-ops-checkout-confirm"
                onClick={() => onCheckout(attendee.attendanceIdKey)}
                disabled={isCheckingOut}
                className="rounded-md bg-red-600 px-2 py-0.5 text-[11px] font-medium text-white hover:bg-red-700 disabled:opacity-50"
              >
                {isCheckingOut ? 'Checking out…' : 'Confirm'}
              </button>
              <button
                type="button"
                onClick={() => setIsConfirming(false)}
                className="rounded-md border border-gray-300 bg-white px-2 py-0.5 text-[11px] text-gray-600 hover:bg-gray-50"
              >
                Cancel
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <span className="inline-block rounded-full bg-green-100 px-2 py-0.5 text-green-700">
                Present
              </span>
              <button
                type="button"
                data-testid="checkin-ops-checkout"
                onClick={() => setIsConfirming(true)}
                className="rounded-md border border-gray-300 bg-white px-2 py-0.5 text-[11px] text-gray-700 hover:bg-gray-50"
              >
                Check out
              </button>
            </div>
          )
        ) : (
          <span className="inline-block rounded-full bg-gray-100 px-2 py-0.5 text-gray-600">
            Checked out
          </span>
        )}
      </div>
    </li>
  );
}

export function AttendeeSearch({ attendees, onCheckout, checkingOutIdKey }: AttendeeSearchProps) {
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
            <AttendeeRow
              key={attendee.attendanceIdKey}
              attendee={attendee}
              onCheckout={onCheckout}
              isCheckingOut={checkingOutIdKey === attendee.attendanceIdKey}
            />
          ))}
        </ul>
      )}
    </div>
  );
}
