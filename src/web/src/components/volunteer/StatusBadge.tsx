/**
 * Status Badge Component
 * Displays volunteer schedule assignment status with color coding
 */

import { VolunteerScheduleStatus } from '@/types/volunteer';
import { cn } from '@/lib/utils';

interface StatusBadgeProps {
  status: VolunteerScheduleStatus;
  className?: string;
}

const statusConfig = {
  [VolunteerScheduleStatus.Scheduled]: {
    label: 'Scheduled',
    className: 'bg-gray-100 text-gray-800 border-gray-300',
  },
  [VolunteerScheduleStatus.Confirmed]: {
    label: 'Confirmed',
    className: 'bg-green-100 text-green-800 border-green-300',
  },
  [VolunteerScheduleStatus.Declined]: {
    label: 'Declined',
    className: 'bg-red-100 text-red-800 border-red-300',
  },
  [VolunteerScheduleStatus.NoResponse]: {
    label: 'No Response',
    className: 'bg-yellow-100 text-yellow-800 border-yellow-300',
  },
};

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const config = statusConfig[status];

  return (
    <span
      className={cn(
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border',
        config.className,
        className
      )}
    >
      {config.label}
    </span>
  );
}
