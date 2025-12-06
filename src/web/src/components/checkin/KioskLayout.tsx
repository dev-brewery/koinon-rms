import React from 'react';

export interface KioskLayoutProps {
  children: React.ReactNode;
  title?: string;
  onReset?: () => void;
}

/**
 * Full-screen kiosk layout for check-in
 */
export function KioskLayout({ children, title, onReset }: KioskLayoutProps) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-blue-100 flex flex-col">
      {/* Header */}
      <header className="bg-white shadow-md py-4 px-6">
        <div className="max-w-7xl mx-auto flex justify-between items-center">
          <div className="flex items-center gap-4">
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
          {onReset && (
            <button
              onClick={onReset}
              className="px-6 py-3 text-gray-600 hover:text-gray-900 font-medium transition-colors"
            >
              Start Over
            </button>
          )}
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
