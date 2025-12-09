import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { RsvpStatusBadge } from './RsvpStatusBadge';

interface MyRsvp {
  groupIdKey: string;
  groupName: string;
  meetingDate: string;
  status: 'NoResponse' | 'Attending' | 'NotAttending' | 'Maybe';
  note?: string;
}

interface UpcomingMeetingsCardProps {
  rsvps: MyRsvp[];
  onRsvpClick: (groupIdKey: string, meetingDate: string) => void;
  isLoading?: boolean;
}

export function UpcomingMeetingsCard({ rsvps, onRsvpClick, isLoading = false }: UpcomingMeetingsCardProps) {
  if (isLoading) {
    return (
      <Card className="p-6">
        <h2 className="text-xl font-semibold mb-2">Upcoming Meetings</h2>
        <p className="text-gray-600">Loading...</p>
      </Card>
    );
  }

  if (rsvps.length === 0) {
    return (
      <Card className="p-6">
        <h2 className="text-xl font-semibold mb-2">Upcoming Meetings</h2>
        <p className="text-gray-600">No upcoming meetings requiring RSVP</p>
      </Card>
    );
  }

  return (
    <Card className="p-6">
      <h2 className="text-xl font-semibold mb-2">Upcoming Meetings</h2>
      <p className="text-gray-600 mb-4">Meetings requiring your RSVP</p>
      
      <div className="space-y-3">
        {rsvps.map((rsvp) => (
          <div key={`${rsvp.groupIdKey}-${rsvp.meetingDate}`} className="flex items-center justify-between border-b pb-3 last:border-0">
            <div className="flex-1">
              <div className="font-medium">{rsvp.groupName}</div>
              <div className="text-sm text-gray-500">{rsvp.meetingDate}</div>
            </div>
            <div className="flex items-center gap-2">
              <RsvpStatusBadge status={rsvp.status} />
              <Button
                size="sm"
                variant="outline"
                onClick={() => onRsvpClick(rsvp.groupIdKey, rsvp.meetingDate)}
              >
                {rsvp.status === 'NoResponse' ? 'Respond' : 'Update'}
              </Button>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
