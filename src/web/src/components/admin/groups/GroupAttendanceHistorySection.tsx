/**
 * Group Attendance History Section
 * Displays past attendance occurrences with expandable attendee details
 */

import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGroupAttendanceHistory, useGroupAttendanceDetail } from '@/hooks/useGroups';
import type { GroupAttendanceOccurrenceDto } from '@/services/api/types';

interface GroupAttendanceHistorySectionProps {
  groupIdKey: string;
}

function formatOccurrenceDate(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00');
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

function TrendIndicator({
  current,
  previous,
}: {
  current: GroupAttendanceOccurrenceDto;
  previous: GroupAttendanceOccurrenceDto | undefined;
}) {
  if (!previous || current.didNotOccur || previous.didNotOccur) return null;

  const diff = current.attendeeCount - previous.attendeeCount;
  if (diff === 0) return null;

  if (diff > 0) {
    return (
      <span
        className="text-green-600 font-semibold ml-2"
        title={`Up from ${previous.attendeeCount}`}
      >
        ↑
      </span>
    );
  }

  return (
    <span
      className="text-red-600 font-semibold ml-2"
      title={`Down from ${previous.attendeeCount}`}
    >
      ↓
    </span>
  );
}

function OccurrenceRow({
  occurrence,
  previousOccurrence,
  groupIdKey,
}: {
  occurrence: GroupAttendanceOccurrenceDto;
  previousOccurrence: GroupAttendanceOccurrenceDto | undefined;
  groupIdKey: string;
}) {
  const [expanded, setExpanded] = useState(false);
  const { data: attendees } = useGroupAttendanceDetail(
    expanded ? groupIdKey : undefined,
    expanded ? occurrence.idKey : undefined
  );

  const presentAttendees = (attendees || []).filter((a) => a.didAttend);

  return (
    <div className="border border-gray-200 rounded-lg">
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full text-left px-4 py-3 flex items-center justify-between hover:bg-gray-50"
        aria-label={formatOccurrenceDate(occurrence.occurrenceDate)}
      >
        <div className="flex items-center gap-3">
          <span className="font-medium text-gray-900">
            {formatOccurrenceDate(occurrence.occurrenceDate)}
          </span>
          {occurrence.locationName && (
            <span className="text-sm text-gray-500">{occurrence.locationName}</span>
          )}
          {occurrence.scheduleName && (
            <span className="text-sm text-gray-400">({occurrence.scheduleName})</span>
          )}
        </div>
        <div className="flex items-center">
          {occurrence.didNotOccur ? (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">
              Did not occur
            </span>
          ) : (
            <span className="text-sm text-gray-600">
              {occurrence.attendeeCount} attended
              <TrendIndicator current={occurrence} previous={previousOccurrence} />
            </span>
          )}
          <svg
            className={`w-4 h-4 ml-2 text-gray-400 transition-transform ${expanded ? 'rotate-180' : ''}`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </button>

      {expanded && presentAttendees.length > 0 && (
        <div className="border-t border-gray-200 px-4 py-3 space-y-2">
          {presentAttendees.map((attendee) => (
            <div key={attendee.personIdKey} className="flex items-center gap-3">
              {attendee.photoUrl ? (
                <img
                  src={attendee.photoUrl}
                  alt={attendee.fullName}
                  className="w-8 h-8 rounded-full object-cover"
                />
              ) : (
                <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-xs font-medium text-gray-600">
                  {attendee.fullName.charAt(0)}
                </div>
              )}
              <Link
                to={`/admin/people/${attendee.personIdKey}`}
                className="text-sm text-blue-600 hover:text-blue-800 hover:underline"
              >
                {attendee.fullName}
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function GroupAttendanceHistorySection({ groupIdKey }: GroupAttendanceHistorySectionProps) {
  const { data: historyData, isLoading } = useGroupAttendanceHistory(groupIdKey);

  const occurrences = historyData?.data || [];

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h2 className="text-lg font-semibold text-gray-900 mb-4">Attendance History</h2>

      {isLoading && (
        <div className="text-center py-8 text-gray-500">Loading attendance history...</div>
      )}

      {!isLoading && occurrences.length === 0 && (
        <div className="text-center py-8 text-gray-500">No attendance records yet</div>
      )}

      {!isLoading && occurrences.length > 0 && (
        <div className="space-y-2">
          {occurrences.map((occ, index) => (
            <OccurrenceRow
              key={occ.idKey}
              occurrence={occ}
              previousOccurrence={occurrences[index + 1]}
              groupIdKey={groupIdKey}
            />
          ))}
        </div>
      )}
    </div>
  );
}
