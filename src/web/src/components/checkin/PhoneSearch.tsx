import { useState } from 'react';
import { Button } from '@/components/ui/Button';

export interface PhoneSearchProps {
  onSearch: (phone: string) => void;
  loading?: boolean;
}

/**
 * Phone number entry with large numpad for kiosk
 */
export function PhoneSearch({ onSearch, loading }: PhoneSearchProps) {
  const [phone, setPhone] = useState('');

  const handleDigit = (digit: string) => {
    if (phone.length < 10) {
      setPhone(phone + digit);
    }
  };

  const handleClear = () => {
    setPhone('');
  };

  const handleBackspace = () => {
    setPhone(phone.slice(0, -1));
  };

  const handleSearch = () => {
    if (phone.length >= 4) {
      onSearch(phone);
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
          <div className="bg-gray-100 rounded-lg p-6 text-center">
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
          disabled={phone.length < 4}
          loading={loading}
          size="lg"
          className="w-full text-xl"
        >
          Search
        </Button>

        {/* Helper Text */}
        <p className="text-center text-sm text-gray-500 mt-4">
          Enter at least 4 digits to search
        </p>
      </div>
    </div>
  );
}
