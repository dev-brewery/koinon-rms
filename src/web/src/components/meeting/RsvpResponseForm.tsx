import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

interface RsvpResponseFormProps {
  groupName: string;
  meetingDate: string;
  currentStatus?: string;
  currentNote?: string;
  onSubmit: (status: string, note: string) => Promise<void>;
  isLoading?: boolean;
}

export function RsvpResponseForm({
  groupName,
  meetingDate,
  currentStatus = 'NoResponse',
  currentNote = '',
  onSubmit,
  isLoading = false,
}: RsvpResponseFormProps) {
  const [status, setStatus] = useState(currentStatus);
  const [note, setNote] = useState(currentNote);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(status, note);
  };

  return (
    <Card className="p-6">
      <h2 className="text-xl font-semibold mb-2">RSVP for {groupName}</h2>
      <p className="text-gray-600 mb-4">Meeting on {meetingDate}</p>
      
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-3">
          <label className="block text-sm font-medium text-gray-700">Will you attend?</label>
          <div className="space-y-2">
            {[
              { value: 'Attending', label: 'Yes, I will be there' },
              { value: 'NotAttending', label: 'No, I cannot make it' },
              { value: 'Maybe', label: 'Maybe' },
            ].map((option) => (
              <label key={option.value} className="flex items-center space-x-2">
                <input
                  type="radio"
                  name="status"
                  value={option.value}
                  checked={status === option.value}
                  onChange={(e) => setStatus(e.target.value)}
                  className="h-4 w-4 text-blue-600 border-gray-300"
                />
                <span className="text-sm text-gray-700">{option.label}</span>
              </label>
            ))}
          </div>
        </div>

        <div className="space-y-2">
          <label htmlFor="note" className="block text-sm font-medium text-gray-700">
            Note (optional)
          </label>
          <textarea
            id="note"
            placeholder="Add a note for the group leader..."
            value={note}
            onChange={(e) => setNote(e.target.value)}
            maxLength={500}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
            rows={3}
          />
        </div>

        <Button type="submit" variant="primary" disabled={isLoading}>
          {isLoading ? 'Updating...' : 'Update RSVP'}
        </Button>
      </form>
    </Card>
  );
}
