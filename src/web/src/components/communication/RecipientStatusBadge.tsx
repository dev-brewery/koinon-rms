/**
 * RecipientStatusBadge
 * Badge component for displaying individual recipient delivery status
 */

interface RecipientStatusBadgeProps {
  status: string;
}

export function RecipientStatusBadge({ status }: RecipientStatusBadgeProps) {
  const colors: Record<string, string> = {
    Pending: 'bg-blue-100 text-blue-800',
    Delivered: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Opened: 'bg-purple-100 text-purple-800',
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
        colors[status] || 'bg-gray-100 text-gray-800'
      }`}
    >
      {status}
    </span>
  );
}
