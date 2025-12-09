import { UpcomingMeetingsCard } from '@/components/meeting/UpcomingMeetingsCard';
import { useMyRsvps } from '@/features/meeting/hooks';
import { useNavigate } from 'react-router-dom';

export function MyRsvpsPage() {
  const navigate = useNavigate();
  const { data: rsvps = [], isLoading, isError, error } = useMyRsvps();

  const handleRsvpClick = (groupIdKey: string, meetingDate: string) => {
    navigate(`/groups/${groupIdKey}/meeting/${meetingDate}/rsvp`);
  };

  if (isError) {
    return (
      <div className="container mx-auto py-6">
        <h1 className="text-3xl font-bold mb-6">My Meeting RSVPs</h1>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
          <p className="font-medium">Failed to load RSVPs</p>
          <p className="text-sm">{error?.message || 'Please try again later.'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-6">
      <h1 className="text-3xl font-bold mb-6">My Meeting RSVPs</h1>
      <UpcomingMeetingsCard
        rsvps={rsvps}
        onRsvpClick={handleRsvpClick}
        isLoading={isLoading}
      />
    </div>
  );
}
