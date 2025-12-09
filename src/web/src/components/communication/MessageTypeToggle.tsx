/**
 * MessageTypeToggle Component
 * Toggle between Email and SMS communication types
 */

interface MessageTypeToggleProps {
  value: 'Email' | 'Sms';
  onChange: (type: 'Email' | 'Sms') => void;
}

export function MessageTypeToggle({ value, onChange }: MessageTypeToggleProps) {
  return (
    <div className="inline-flex rounded-lg border border-gray-300 bg-white p-1">
      <button
        type="button"
        onClick={() => onChange('Email')}
        className={`flex items-center gap-2 px-4 py-2 rounded-md font-medium transition-colors ${
          value === 'Email'
            ? 'bg-primary-600 text-white'
            : 'text-gray-700 hover:bg-gray-100'
        }`}
      >
        <svg
          className="w-5 h-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
          />
        </svg>
        Email
      </button>
      <button
        type="button"
        onClick={() => onChange('Sms')}
        className={`flex items-center gap-2 px-4 py-2 rounded-md font-medium transition-colors ${
          value === 'Sms'
            ? 'bg-primary-600 text-white'
            : 'text-gray-700 hover:bg-gray-100'
        }`}
      >
        <svg
          className="w-5 h-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 18h.01M8 21h8a2 2 0 002-2V5a2 2 0 00-2-2H8a2 2 0 00-2 2v14a2 2 0 002 2z"
          />
        </svg>
        SMS
      </button>
    </div>
  );
}
