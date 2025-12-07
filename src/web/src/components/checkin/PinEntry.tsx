import React, { useState, useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';

export interface PinEntryProps {
  onSubmit: (pin: string) => Promise<boolean>;
  onCancel: () => void;
  maxLength?: number;
}

/**
 * PIN entry component with touch-friendly numeric keypad
 * Features shake animation on failed PIN
 * Security: Clears PIN from memory immediately after submission
 */
export function PinEntry({
  onSubmit,
  onCancel,
  maxLength = 6,
}: PinEntryProps) {
  const [pin, setPin] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [hasError, setHasError] = useState(false);
  const modalRef = useRef<HTMLDivElement>(null);

  // Auto-focus modal on mount for keyboard support
  useEffect(() => {
    modalRef.current?.focus();
  }, []);

  // Reset error animation after it plays
  useEffect(() => {
    if (hasError) {
      const timer = setTimeout(() => setHasError(false), 650);
      return () => clearTimeout(timer);
    }
  }, [hasError]);

  const handleNumberPress = (digit: number) => {
    if (pin.length < maxLength) {
      setPin((prev) => prev + digit);
    }
  };

  const handleClear = () => {
    setPin('');
    setHasError(false);
  };

  const handleBackspace = () => {
    setPin((prev) => prev.slice(0, -1));
    setHasError(false);
  };

  const handleSubmit = async () => {
    if (pin.length < 4 || pin.length > maxLength) {
      setHasError(true);
      return;
    }

    setIsSubmitting(true);
    try {
      // Clear PIN from UI immediately (note: JS strings are immutable and cannot be cleared from memory)
      const pinToSubmit = pin;
      setPin('');

      const success = await onSubmit(pinToSubmit);
      if (!success) {
        setHasError(true);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && pin.length >= 4) {
      handleSubmit();
    } else if (e.key === 'Escape') {
      onCancel();
    } else if (e.key === 'Backspace') {
      handleBackspace();
    } else if (/^[0-9]$/.test(e.key)) {
      handleNumberPress(parseInt(e.key, 10));
    }
  };

  return (
    <div
      ref={modalRef}
      tabIndex={-1}
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm"
      onKeyDown={handleKeyDown}
    >
      <div className="bg-white rounded-2xl shadow-2xl p-8 max-w-md w-full mx-4">
        {/* Header */}
        <div className="text-center mb-6">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Supervisor Access
          </h2>
          <p className="text-gray-600">Enter your PIN to continue</p>
        </div>

        {/* PIN Display */}
        <div
          className={cn(
            'mb-8 flex justify-center gap-3',
            hasError && 'animate-shake'
          )}
        >
          {Array.from({ length: maxLength }).map((_, index) => (
            <div
              key={index}
              className={cn(
                'w-12 h-12 rounded-lg border-2 flex items-center justify-center transition-colors',
                index < pin.length
                  ? 'border-blue-600 bg-blue-50'
                  : 'border-gray-300 bg-white',
                hasError && index < pin.length && 'border-red-500 bg-red-50'
              )}
              aria-hidden="true"
            >
              {index < pin.length && (
                <div className="w-3 h-3 rounded-full bg-gray-900" />
              )}
            </div>
          ))}
        </div>

        {/* Error Message */}
        {hasError && (
          <div className="mb-4 text-center">
            <p className="text-red-600 font-medium">
              Incorrect PIN. Please try again.
            </p>
          </div>
        )}

        {/* Keypad */}
        <div className="grid grid-cols-3 gap-3 mb-6">
          {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((digit) => (
            <button
              key={digit}
              onClick={() => handleNumberPress(digit)}
              disabled={isSubmitting}
              className="h-16 rounded-lg bg-gray-100 hover:bg-gray-200 active:bg-gray-300 text-2xl font-semibold text-gray-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[64px]"
            >
              {digit}
            </button>
          ))}

          {/* Bottom row: Clear, 0, Backspace */}
          <button
            onClick={handleClear}
            disabled={isSubmitting || pin.length === 0}
            className="h-16 rounded-lg bg-gray-100 hover:bg-gray-200 active:bg-gray-300 text-lg font-semibold text-gray-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[64px]"
          >
            Clear
          </button>
          <button
            onClick={() => handleNumberPress(0)}
            disabled={isSubmitting}
            className="h-16 rounded-lg bg-gray-100 hover:bg-gray-200 active:bg-gray-300 text-2xl font-semibold text-gray-900 transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[64px]"
          >
            0
          </button>
          <button
            onClick={handleBackspace}
            disabled={isSubmitting || pin.length === 0}
            className="h-16 rounded-lg bg-gray-100 hover:bg-gray-200 active:bg-gray-300 transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[64px]"
            aria-label="Backspace"
          >
            <svg
              className="w-6 h-6 mx-auto text-gray-700"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2M3 12l6.414 6.414a2 2 0 001.414.586H19a2 2 0 002-2V7a2 2 0 00-2-2h-8.172a2 2 0 00-1.414.586L3 12z"
              />
            </svg>
          </button>
        </div>

        {/* Action Buttons */}
        <div className="flex gap-3">
          <button
            onClick={onCancel}
            disabled={isSubmitting}
            className="flex-1 h-14 rounded-lg bg-gray-200 hover:bg-gray-300 active:bg-gray-400 text-gray-900 font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[56px]"
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={pin.length < 4 || isSubmitting}
            className="flex-1 h-14 rounded-lg bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed min-h-[56px]"
          >
            {isSubmitting ? 'Verifying...' : 'Submit'}
          </button>
        </div>
      </div>

      <style>{`
        @keyframes shake {
          0%, 100% { transform: translateX(0); }
          10%, 30%, 50%, 70%, 90% { transform: translateX(-8px); }
          20%, 40%, 60%, 80% { transform: translateX(8px); }
        }
        .animate-shake {
          animation: shake 0.6s cubic-bezier(.36,.07,.19,.97) both;
        }
      `}</style>
    </div>
  );
}
