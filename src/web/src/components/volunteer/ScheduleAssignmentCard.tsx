/**
 * Schedule Assignment Card Component
 * Displays a single volunteer schedule assignment with actions
 */

import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { StatusBadge } from './StatusBadge';
import type { ScheduleAssignmentDto } from '@/types/volunteer';
import { VolunteerScheduleStatus } from '@/types/volunteer';

interface ScheduleAssignmentCardProps {
  assignment: ScheduleAssignmentDto;
  onConfirm?: (assignmentIdKey: string) => void;
  onDecline?: (assignmentIdKey: string) => void;
  isUpdating?: boolean;
  showActions?: boolean;
}

export function ScheduleAssignmentCard({
  assignment,
  onConfirm,
  onDecline,
  isUpdating = false,
  showActions = true,
}: ScheduleAssignmentCardProps) {
  const canRespond =
    showActions &&
    (assignment.status === VolunteerScheduleStatus.Scheduled ||
      assignment.status === VolunteerScheduleStatus.NoResponse);

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <Card className="p-4">
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <h3 className="text-lg font-medium text-gray-900">
              {assignment.memberName}
            </h3>
            <StatusBadge status={assignment.status} />
          </div>

          <div className="space-y-1 text-sm text-gray-600">
            <p>
              <span className="font-medium">Schedule:</span> {assignment.scheduleName}
            </p>
            <p>
              <span className="font-medium">Date:</span> {formatDate(assignment.assignedDate)}
            </p>
            {assignment.note && (
              <p>
                <span className="font-medium">Note:</span> {assignment.note}
              </p>
            )}
            {assignment.declineReason && (
              <p className="text-red-600">
                <span className="font-medium">Decline Reason:</span> {assignment.declineReason}
              </p>
            )}
            {assignment.respondedDateTime && (
              <p className="text-xs text-gray-500">
                Responded: {new Date(assignment.respondedDateTime).toLocaleString()}
              </p>
            )}
          </div>
        </div>

        {canRespond && (
          <div className="flex gap-2 ml-4">
            <Button
              size="sm"
              variant="primary"
              onClick={() => onConfirm?.(assignment.idKey)}
              disabled={isUpdating}
              loading={isUpdating}
            >
              Confirm
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => onDecline?.(assignment.idKey)}
              disabled={isUpdating}
            >
              Decline
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
}
