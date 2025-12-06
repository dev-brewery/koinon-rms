import React, { useState } from 'react';
import { Input, InputProps } from './Input';

export interface PhoneInputProps extends Omit<InputProps, 'value' | 'onChange'> {
  value: string;
  onChange: (value: string) => void;
}

/**
 * Format phone number as user types (US format)
 */
function formatPhoneNumber(value: string): string {
  // Remove all non-digits
  const digits = value.replace(/\D/g, '');

  // Format based on length
  if (digits.length <= 3) {
    return digits;
  } else if (digits.length <= 6) {
    return `(${digits.slice(0, 3)}) ${digits.slice(3)}`;
  } else if (digits.length <= 10) {
    return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
  } else {
    // Limit to 10 digits
    return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6, 10)}`;
  }
}

/**
 * Extract raw digits from formatted phone number
 */
function getDigitsOnly(value: string): string {
  return value.replace(/\D/g, '');
}

export function PhoneInput({ value, onChange, ...props }: PhoneInputProps) {
  const [formattedValue, setFormattedValue] = useState(formatPhoneNumber(value));

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    const formatted = formatPhoneNumber(newValue);
    const digits = getDigitsOnly(formatted);

    setFormattedValue(formatted);
    onChange(digits);
  };

  return (
    <Input
      {...props}
      type="tel"
      value={formattedValue}
      onChange={handleChange}
      placeholder="(555) 123-4567"
    />
  );
}
