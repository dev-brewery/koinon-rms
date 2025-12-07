import { useEffect, useRef } from 'react';

export interface IdleWarningModalProps {
  isOpen: boolean;
  secondsRemaining: number;
  onStayActive: () => void;
}

/**
 * Full-screen modal that appears when kiosk idle timeout warning is triggered
 *
 * Shows countdown and allows user to continue or lets timeout expire.
 * Any touch/click automatically dismisses and resets timer.
 */
export function IdleWarningModal({
  isOpen,
  secondsRemaining,
  onStayActive,
}: IdleWarningModalProps) {
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Focus the Continue button when modal opens
  useEffect(() => {
    if (isOpen && buttonRef.current) {
      buttonRef.current.focus();
    }
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm"
      onClick={onStayActive}
      onTouchStart={onStayActive}
      role="alertdialog"
      aria-labelledby="idle-warning-title"
      aria-describedby="idle-warning-description"
    >
      <div
        className="bg-white rounded-2xl shadow-2xl p-12 max-w-2xl mx-4 text-center"
        onClick={(e) => e.stopPropagation()} // Prevent double-trigger
      >
        {/* Icon */}
        <div className="mb-8 flex justify-center">
          <div className="w-24 h-24 bg-yellow-100 rounded-full flex items-center justify-center">
            <svg
              className="w-16 h-16 text-yellow-600"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
          </div>
        </div>

        {/* Title */}
        <h2
          id="idle-warning-title"
          className="text-4xl font-bold text-gray-900 mb-4"
        >
          Still There?
        </h2>

        {/* Description */}
        <p
          id="idle-warning-description"
          className="text-xl text-gray-600 mb-8"
        >
          Your session will reset in:
        </p>

        {/* Countdown */}
        <div className="mb-10">
          <div className="text-8xl font-bold text-blue-600 mb-2 tabular-nums">
            {secondsRemaining}
          </div>
          <div className="text-2xl text-gray-500">
            {secondsRemaining === 1 ? 'second' : 'seconds'}
          </div>
        </div>

        {/* Continue Button */}
        <button
          ref={buttonRef}
          onClick={onStayActive}
          className="px-12 py-6 bg-blue-600 text-white text-2xl font-semibold rounded-xl hover:bg-blue-700 transition-colors shadow-lg min-h-[80px] min-w-[300px]"
        >
          Continue Check-In
        </button>

        {/* Helper text */}
        <p className="text-gray-500 mt-8 text-lg">
          Or tap anywhere to continue
        </p>
      </div>
    </div>
  );
}
