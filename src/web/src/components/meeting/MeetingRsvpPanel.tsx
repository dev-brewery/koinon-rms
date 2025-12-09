import { Card } from '@/components/ui/Card';
import { RsvpStatusBadge } from './RsvpStatusBadge';

interface Rsvp {
  idKey: string;
  personIdKey: string;
  personName: string;
  status: 'NoResponse' | 'Attending' | 'NotAttending' | 'Maybe';
  note?: string;
  respondedDateTime?: string;
}

interface MeetingRsvpSummary {
  meetingDate: string;
  attending: number;
  notAttending: number;
  maybe: number;
  noResponse: number;
  totalInvited: number;
  rsvps: Rsvp[];
}

interface MeetingRsvpPanelProps {
  summary: MeetingRsvpSummary;
}

export function MeetingRsvpPanel({ summary }: MeetingRsvpPanelProps) {
  return (
    <Card className="p-6">
      <h2 className="text-xl font-semibold mb-2">Meeting RSVP Summary</h2>
      <p className="text-gray-600 mb-4">Meeting on {summary.meetingDate}</p>
      
      <div className="grid grid-cols-4 gap-4 mb-6">
        <div className="text-center">
          <div className="text-2xl font-bold text-green-600">{summary.attending}</div>
          <div className="text-sm text-gray-500">Attending</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-red-600">{summary.notAttending}</div>
          <div className="text-sm text-gray-500">Not Attending</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-yellow-600">{summary.maybe}</div>
          <div className="text-sm text-gray-500">Maybe</div>
        </div>
        <div className="text-center">
          <div className="text-2xl font-bold text-gray-600">{summary.noResponse}</div>
          <div className="text-sm text-gray-500">No Response</div>
        </div>
      </div>

      <div className="space-y-2">
        <h4 className="font-semibold">Responses</h4>
        {summary.rsvps.length === 0 ? (
          <p className="text-sm text-gray-500">No RSVPs yet</p>
        ) : (
          <div className="space-y-2">
            {summary.rsvps.map((rsvp) => (
              <div key={rsvp.idKey} className="flex items-center justify-between border-b pb-2">
                <div>
                  <div className="font-medium">{rsvp.personName}</div>
                  {rsvp.note && <div className="text-sm text-gray-500">{rsvp.note}</div>}
                </div>
                <RsvpStatusBadge status={rsvp.status} />
              </div>
            ))}
          </div>
        )}
      </div>
    </Card>
  );
}
