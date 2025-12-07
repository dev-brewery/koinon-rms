import React, { useState } from 'react';
import { Button, Card } from '@/components/ui';

export interface PinEntryProps {
  onSubmit: (pin: string) => void;
  onCancel: () => void;
  loading?: boolean;
  error?: string | null;
}

/**
 * PIN entry component with large touch-friendly buttons
 * for supervisor mode authentication
 */
export function PinEntry({ onSubmit, onCancel, loading, error }: PinEntryProps) {
  const [pin, setPin] = useState('');

  const handleNumberClick = (digit: number) => {
    if (pin.length < 6) {
      setPin(pin + digit.toString());
    }
  };

  const handleClear = () => {
    setPin('');
  };

  const handleSubmit = () => {
    if (pin.length >= 4 && pin.length <= 6) {
      onSubmit(pin);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && pin.length >= 4) {
      handleSubmit();
    } else if (e.key === 'Backspace') {
      setPin(pin.slice(0, -1));
    } else if (/^\d$/.test(e.key) && pin.length < 6) {
      setPin(pin + e.key);
    }
  };

  return (
    <Card className="max-w-md mx-auto p-8">
      <h2 className="text-2xl font-bold text-center mb-6 text-gray-900">
        Supervisor PIN
      </h2>

      {/* PIN Display */}
      <div
        className="mb-6 flex justify-center gap-3"
        onKeyDown={handleKeyPress}
        tabIndex={0}
      >
        {[...Array(6)].map((_, i) => (
          <div
            key={i}
            className={`w-12 h-12 rounded-lg border-2 flex items-center justify-center text-2xl font-bold ${
              i < pin.length
                ? 'border-blue-600 bg-blue-50 text-blue-600'
                : 'border-gray-300 bg-white text-gray-300'
            }`}
          >
            {i < pin.length ? '•' : ''}
          </div>
        ))}
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-800 text-center text-sm">{error}</p>
        </div>
      )}

      {/* Number Pad */}
      <div className="grid grid-cols-3 gap-3 mb-6">
        {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((num) => (
          <button
            key={num}
            onClick={() => handleNumberClick(num)}
            disabled={loading || pin.length >= 6}
            className="min-h-[64px] text-2xl font-bold bg-white border-2 border-gray-300 rounded-lg hover:bg-gray-50 hover:border-blue-400 active:bg-blue-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {num}
          </button>
        ))}
        <button
          onClick={handleClear}
          disabled={loading || pin.length === 0}
          className="min-h-[64px] text-lg font-semibold bg-white border-2 border-gray-300 rounded-lg hover:bg-gray-50 hover:border-red-400 active:bg-red-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          Clear
        </button>
        <button
          onClick={() => handleNumberClick(0)}
          disabled={loading || pin.length >= 6}
          className="min-h-[64px] text-2xl font-bold bg-white border-2 border-gray-300 rounded-lg hover:bg-gray-50 hover:border-blue-400 active:bg-blue-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          0
        </button>
        <button
          onClick={() => setPin(pin.slice(0, -1))}
          disabled={loading || pin.length === 0}
          className="min-h-[64px] text-lg font-semibold bg-white border-2 border-gray-300 rounded-lg hover:bg-gray-50 hover:border-yellow-400 active:bg-yellow-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          ⌫
        </button>
      </div>

      {/* Action Buttons */}
      <div className="flex gap-3">
        <Button
          onClick={onCancel}
          disabled={loading}
          variant="secondary"
          size="lg"
          className="flex-1"
        >
          Cancel
        </Button>
        <Button
          onClick={handleSubmit}
          disabled={loading || pin.length < 4}
          loading={loading}
          size="lg"
          className="flex-1"
        >
          Submit
        </Button>
      </div>

      <p className="text-sm text-gray-600 text-center mt-4">
        Enter your 4-6 digit supervisor PIN
      </p>
    </Card>
  );
}
