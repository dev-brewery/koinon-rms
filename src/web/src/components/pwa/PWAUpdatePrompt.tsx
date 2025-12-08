import { useEffect, useState } from 'react';
import { Button } from '@/components/ui';

interface PWAUpdatePromptProps {
  onUpdate: () => void;
  offlineReady: boolean;
}

export function PWAUpdatePrompt({ onUpdate, offlineReady }: PWAUpdatePromptProps) {
  const [show, setShow] = useState(false);

  useEffect(() => {
    if (!offlineReady) {
      return;
    }
    setShow(true);
    const timer = setTimeout(() => setShow(false), 5000);
    return () => clearTimeout(timer);
  }, [offlineReady]);

  if (!show) return null;

  return (
    <div className="fixed bottom-4 right-4 left-4 md:left-auto md:w-96 bg-white rounded-lg shadow-xl border-2 border-blue-600 p-4 z-50 animate-slide-up">
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          <svg
            className="w-6 h-6 text-blue-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
            />
          </svg>
        </div>
        <div className="flex-1">
          <h3 className="text-sm font-semibold text-gray-900 mb-1">
            New Version Available
          </h3>
          <p className="text-sm text-gray-600 mb-3">
            A new version of the check-in app is ready. Update now for the latest features and improvements.
          </p>
          <div className="flex gap-2">
            <Button
              onClick={onUpdate}
              size="sm"
              className="flex-1"
            >
              Update Now
            </Button>
            <Button
              onClick={() => setShow(false)}
              variant="outline"
              size="sm"
              className="flex-1"
            >
              Later
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
