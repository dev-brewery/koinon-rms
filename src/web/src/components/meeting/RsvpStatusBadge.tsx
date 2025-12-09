interface RsvpStatusBadgeProps {
  status: 'NoResponse' | 'Attending' | 'NotAttending' | 'Maybe';
}

export function RsvpStatusBadge({ status }: RsvpStatusBadgeProps) {
  const config = {
    NoResponse: { className: 'bg-gray-200 text-gray-700', label: 'No Response' },
    Attending: { className: 'bg-green-100 text-green-800', label: 'Attending' },
    NotAttending: { className: 'bg-red-100 text-red-800', label: 'Not Attending' },
    Maybe: { className: 'bg-yellow-100 text-yellow-800', label: 'Maybe' },
  }[status];

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.className}`}>
      {config.label}
    </span>
  );
}
