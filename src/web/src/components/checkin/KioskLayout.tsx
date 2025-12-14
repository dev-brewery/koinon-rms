import React, { useState, useCallback } from 'react';

export interface KioskLayoutProps {
  children: React.ReactNode;
  title?: string;
  onReset?: () => void;
  onSupervisorTrigger?: () => void;
}

/**
 * Full-screen kiosk layout for check-in
 * Supports triple-tap on header to trigger supervisor mode
 */
export function KioskLayout({ children, title, onReset, onSupervisorTrigger }: KioskLayoutProps) {
  const [tapCount, setTapCount] = useState(0);
  const [tapTimeout, setTapTimeout] = useState<NodeJS.Timeout | null>(null);

  const handleHeaderTap = useCallback(() => {
    if (!onSupervisorTrigger) return;

    // Clear existing timeout
    if (tapTimeout) {
      clearTimeout(tapTimeout);
    }

    const newTapCount = tapCount + 1;
    setTapCount(newTapCount);

    if (newTapCount >= 3) {
      // Triple-tap detected
      setTapCount(0);
      onSupervisorTrigger();
    } else {
      // Reset tap count after 1 second
      const timeout = setTimeout(() => {
        setTapCount(0);
      }, 1000);
      setTapTimeout(timeout);
    }
  }, [tapCount, tapTimeout, onSupervisorTrigger]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-blue-100 flex flex-col">
      {/* Header */}
      <header className="bg-white shadow-md py-4 px-6">
        <div className="max-w-7xl mx-auto flex justify-between items-center">
          <div
            className="flex items-center gap-4 select-none"
            onClick={handleHeaderTap}
            style={{ cursor: onSupervisorTrigger ? 'pointer' : 'default' }}
          >
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <svg
                className="w-8 h-8 text-white"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div>
              <h1 className="text-2xl font-bold text-gray-900">Check-In</h1>
              {title && <p className="text-sm text-gray-600">{title}</p>}
            </div>
          </div>
          <div className="flex items-center gap-3">
            {onSupervisorTrigger && (
              <button
                onClick={onSupervisorTrigger}
                className="px-4 py-3 min-h-[48px] text-gray-500 hover:text-gray-700 font-medium transition-colors flex items-center gap-2"
                aria-label="Supervisor Mode"
              >
                <svg
                  className="w-5 h-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"
                  />
                </svg>
                <span className="hidden sm:inline">Supervisor</span>
              </button>
            )}
            {onReset && (
              <button
                onClick={onReset}
                className="px-6 py-3 min-h-[48px] text-gray-600 hover:text-gray-900 font-medium transition-colors"
              >
                Start Over
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Content */}
      <main className="flex-1 overflow-y-auto">
        <div className="max-w-7xl mx-auto p-6">
          {children}
        </div>
      </main>

      {/* Footer */}
      <footer className="bg-white shadow-md py-3 px-6 text-center">
        <p className="text-sm text-gray-600">
          Need help? Contact the welcome desk.
        </p>
      </footer>
    </div>
  );
}
