import { useState, useRef, useEffect } from 'react';
import { Button } from '@/components/ui/Button';

export interface PhoneSearchProps {
  onSearch: (phone: string) => void;
  loading?: boolean;
  onInputChange?: (hasInput: boolean) => void;
}

/**
 * Phone number entry with large numpad for kiosk
 */
export function PhoneSearch({ onSearch, loading, onInputChange }: PhoneSearchProps) {
  const [phone, setPhone] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Auto-focus the hidden input on mount
  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  const handleDigit = (digit: string) => {
    if (phone.length < 10) {
      const newPhone = phone + digit;
      setPhone(newPhone);
      onInputChange?.(newPhone.length > 0);
    }
  };

  const handleClear = () => {
    setPhone('');
    onInputChange?.(false);
  };

  const handleBackspace = () => {
    const newPhone = phone.slice(0, -1);
    setPhone(newPhone);
    onInputChange?.(newPhone.length > 0);
  };

  const handleSearch = () => {
    if (phone.length < 10) {
      setValidationError('Please enter a valid phone number (at least 10 digits)');
      return;
    }
    setValidationError(null);
    onSearch(phone);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const rawValue = e.target.value;
    const digits = rawValue.replace(/\D/g, '').slice(0, 10);
    setPhone(digits);
    // Signal input activity based on raw value so non-digit input
    // still hides the search-mode toggles (strict-mode safe)
    onInputChange?.(rawValue.length > 0);
  };

  const handleInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const formatPhone = (value: string) => {
    if (value.length <= 3) return value;
    if (value.length <= 6) return `(${value.slice(0, 3)}) ${value.slice(3)}`;
    return `(${value.slice(0, 3)}) ${value.slice(3, 6)}-${value.slice(6)}`;
  };

  const digits = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0'];

  return (
    <div className="max-w-2xl mx-auto">
      <div className="bg-white rounded-2xl shadow-xl p-8">
        {/* Title */}
        <h2 className="text-3xl font-bold text-center mb-2 text-gray-900">
          Enter Phone Number
        </h2>
        <p className="text-center text-gray-600 mb-8">
          Enter your phone number to find your family
        </p>

        {/* Phone Display */}
        <div className="mb-8">
          <div className="bg-gray-100 rounded-lg p-6 text-center relative">
            <input
              ref={inputRef}
              data-testid="phone-input"
              type="tel"
              inputMode="numeric"
              value={phone}
              onChange={handleInputChange}
              onKeyDown={handleInputKeyDown}
              maxLength={10}
              className="absolute inset-0 w-full h-full opacity-0 cursor-default"
              aria-label="Phone number"
              autoFocus
            />
            <p className="text-4xl font-mono font-bold text-gray-900 min-h-[3rem]">
              {phone ? formatPhone(phone) : '\u00A0'}
            </p>
          </div>
        </div>

        {/* Numpad */}
        <div className="grid grid-cols-3 gap-4 mb-6">
          {digits.map((digit) => (
            <button
              key={digit}
              onClick={() => handleDigit(digit)}
              className="bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-3xl font-bold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-blue-300"
            >
              {digit}
            </button>
          ))}

          {/* Clear Button */}
          <button
            onClick={handleClear}
            className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300"
          >
            Clear
          </button>

          {/* Backspace Button */}
          <button
            onClick={handleBackspace}
            className="bg-gray-300 hover:bg-gray-400 active:bg-gray-500 text-gray-900 text-xl font-semibold rounded-xl py-6 min-h-[80px] transition-colors focus:outline-none focus:ring-4 focus:ring-gray-300"
          >
            ⌫
          </button>
        </div>

        {/* Search Button */}
        <Button
          onClick={handleSearch}
          loading={loading}
          disabled={loading || phone.length === 0}
          size="lg"
          className="w-full text-xl"
        >
          Search
        </Button>

        {/* Validation Error */}
        {validationError && (
          <p className="text-center text-sm text-red-600 mt-4" role="alert">
            {validationError}
          </p>
        )}

        {/* Helper Text */}
        {!validationError && (
          <p className="text-center text-sm text-gray-500 mt-4">
            Enter at least 10 digits to search
          </p>
        )}
      </div>
    </div>
  );
}
