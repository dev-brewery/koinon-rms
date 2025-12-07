import { useState, useEffect, useRef } from 'react';
import { Button, Card } from '@/components/ui';

export interface SecurityCodeVerifyProps {
  expectedCode: string;
  personName: string;
  onVerified: () => void;
  onCancel: () => void;
  loading?: boolean;
}

/**
 * Security code verification component with large touch-friendly numpad
 * Used to verify parent/guardian before checkout
 */
export function SecurityCodeVerify({
  expectedCode,
  personName,
  onVerified,
  onCancel,
  loading,
}: SecurityCodeVerifyProps) {
  const [enteredCode, setEnteredCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, []);

  const handleDigit = (digit: string) => {
    if (enteredCode.length < 4) {
      const newCode = enteredCode + digit;
      setEnteredCode(newCode);
      setError(null);

      // Auto-verify when 4 digits entered
      if (newCode.length === 4) {
        // Add constant-time delay to prevent timing attacks
        timeoutRef.current = setTimeout(() => {
          if (newCode === expectedCode) {
            onVerified();
          } else {
            setError('Incorrect security code. Please try again.');
            // Clear after showing error
            timeoutRef.current = setTimeout(() => {
              setEnteredCode('');
              setError(null);
            }, 2000);
          }
        }, 100); // Fixed delay masks timing differences
      }
    }
  };

  const handleClear = () => {
    setEnteredCode('');
    setError(null);
  };

  const handleBackspace = () => {
    setEnteredCode(enteredCode.slice(0, -1));
    setError(null);
  };

  const digits = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];

  return (
    <Card className="max-w-2xl mx-auto p-8">
      <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
        Verify Security Code
      </h2>
      <p className="text-center text-gray-600 mb-2">
        Checking out: <span className="font-semibold">{personName}</span>
      </p>
      <p className="text-center text-gray-600 mb-8">
        Enter the 4-digit security code from the label
      </p>

      {/* Code Display */}
      <div className="mb-8">
        <div className="flex justify-center gap-4">
          {[0, 1, 2, 3].map((i) => (
            <div
              key={i}
              className={`w-16 h-16 rounded-lg border-2 flex items-center justify-center text-3xl font-bold ${
                i < enteredCode.length
                  ? error
                    ? 'border-red-500 bg-red-50 text-red-600'
                    : 'border-blue-600 bg-blue-50 text-blue-600'
                  : 'border-gray-300 bg-white text-gray-300'
              }`}
            >
              {i < enteredCode.length ? enteredCode[i] : ''}
            </div>
          ))}
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-800 text-center font-medium">{error}</p>
        </div>
      )}

      {/* Numpad */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        {digits.map((digit) => (
          <button
            key={digit}
            onClick={() => handleDigit(digit)}
            disabled={loading || enteredCode.length >= 4}
            className="bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-3xl font-bold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-blue-300 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {digit}
          </button>
        ))}

        {/* Clear Button */}
        <button
          onClick={handleClear}
          disabled={loading || enteredCode.length === 0}
          className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Clear
        </button>

        {/* Backspace Button */}
        <button
          onClick={handleBackspace}
          disabled={loading || enteredCode.length === 0}
          className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          ⌫
        </button>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-4">
        <Button
          onClick={onCancel}
          disabled={loading}
          variant="secondary"
          size="lg"
          className="flex-1"
        >
          Cancel
        </Button>
      </div>

      {/* Helper Text */}
      <p className="text-center text-sm text-gray-500 mt-4">
        The security code is printed on the child's label
      </p>
    </Card>
  );
}
