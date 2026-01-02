/**
 * BatchStatusBadge Component
 * Displays the status of a financial batch with appropriate color coding
 */

import { cn } from '@/lib/utils';

export interface BatchStatusBadgeProps {
  status: string; // 'Open' | 'Closed' | 'Posted'
  className?: string;
}

export function BatchStatusBadge({ status, className }: BatchStatusBadgeProps) {
  // Determine color classes based on status
  const getStatusClasses = () => {
    switch (status) {
      case 'Open':
        return 'bg-green-100 text-green-800';
      case 'Closed':
        return 'bg-gray-100 text-gray-800';
      case 'Posted':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  return (
    <span
      className={cn(
        'inline-flex items-center px-3 py-1 text-xs font-medium rounded-full',
        getStatusClasses(),
        className
      )}
    >
      {status}
    </span>
  );
}
