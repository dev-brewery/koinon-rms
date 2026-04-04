import { useState, useRef, useEffect, useId } from 'react';
import { cn } from '@/lib/utils';

export interface AccessibleSelectOption {
  value: string;
  label: string;
}

interface AccessibleSelectProps {
  id?: string;
  label?: string;
  value: string;
  options: AccessibleSelectOption[];
  onChange: (value: string) => void;
  onBlur?: () => void;
  className?: string;
  error?: string;
}

/**
 * Hybrid select component that works with Playwright's .selectOption() API.
 *
 * Uses a native <select> as the labeled element (enables getByLabel + .selectOption()),
 * but intercepts mousedown to prevent the OS native picker and instead opens a custom
 * DOM dropdown whose options are visible to Playwright's getByRole('option') queries.
 * Native <option> elements remain in the DOM for programmatic access only.
 */
export function AccessibleSelect({
  id,
  label,
  value,
  options,
  onChange,
  onBlur,
  className,
  error,
}: AccessibleSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const generatedId = useId();
  const selectId = id ?? generatedId;
  const containerRef = useRef<HTMLDivElement>(null);


  function handleMouseDown(e: React.MouseEvent<HTMLSelectElement>) {
    // Prevent the OS native picker from opening; open custom DOM dropdown instead.
    e.preventDefault();
    setIsOpen((prev) => !prev);
  }

  function handleChange(e: React.ChangeEvent<HTMLSelectElement>) {
    // Handle programmatic value changes (e.g., Playwright's .selectOption()).
    onChange(e.target.value);
    setIsOpen(false);
  }

  function handleOptionClick(optionValue: string) {
    onChange(optionValue);
    setIsOpen(false);
    onBlur?.();
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLSelectElement>) {
    if (e.key === 'Escape') {
      setIsOpen(false);
    } else if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      setIsOpen((prev) => !prev);
    }
  }

  // Close dropdown when focus moves outside the container.
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (isOpen && containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        onBlur?.();
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen, onBlur]);

  return (
    <div ref={containerRef} className="relative w-full">
      {label && (
        <label htmlFor={selectId} className="block text-sm font-medium text-gray-700 mb-1">
          {label}
        </label>
      )}

      {/* Native select: the element associated with the label via htmlFor.
          Enables Playwright's .selectOption() and getByLabel() locator.
          Styled to look like a dropdown button; OS picker is suppressed via mousedown. */}
      <select
        id={selectId}
        value={value}
        onChange={handleChange}
        onMouseDown={handleMouseDown}
        onKeyDown={handleKeyDown}
        onBlur={(e) => {
          if (!containerRef.current?.contains(e.relatedTarget as Node)) {
            setIsOpen(false);
            onBlur?.();
          }
        }}
        className={cn(
          'w-full px-3 py-2 border border-gray-300 rounded-lg bg-white',
          'focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
          'appearance-none cursor-pointer',
          error && 'border-red-500 focus:ring-red-500',
          className,
        )}
      >
        {options.map((option) => (
          // aria-hidden keeps native options out of the a11y tree so getByRole('option')
          // resolves only to the custom DOM options rendered in the listbox below.
          // Playwright's .selectOption() uses DOM text/value and is unaffected.
          <option key={option.value} value={option.value} aria-hidden="true">
            {option.label}
          </option>
        ))}
      </select>

      {/* Custom dropdown: options as real DOM elements visible to Playwright.
          Only rendered when open so getByRole('option') finds them only after click. */}
      <div
        role="listbox"
        className={cn(
          'absolute z-50 w-full mt-1 bg-white border border-gray-300 rounded-lg shadow-lg',
          !isOpen && 'hidden',
        )}
      >
        {options.map((option) => (
          <div
            key={option.value}
            role="option"
            aria-label={option.label}
            aria-selected={option.value === value}
            onClick={() => handleOptionClick(option.value)}
            className={cn(
              'px-3 py-2 cursor-pointer hover:bg-gray-100',
              option.value === value && 'bg-primary-50 font-medium',
            )}
          >
            {option.label}
          </div>
        ))}
      </div>

      {error && <p className="text-sm text-red-600 mt-1">{error}</p>}
    </div>
  );
}
