import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { Button } from './Button';

export interface ErrorStateProps {
  icon?: ReactNode;
  title: string;
  message?: string;
  onRetry?: () => void;
  className?: string;
}

/**
 * Renders an icon placeholder for error states.
 * Shows a generic error icon when no custom icon is provided.
 */
function DefaultErrorIcon() {
  return (
    <svg
      className="w-12 h-12 text-red-400"
      fill="none"
      stroke="currentColor"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={2}
        d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
      />
    </svg>
  );
}

/**
 * Error state component for displaying when an error occurs.
 *
 * @remarks
 * This component uses a fixed h3 heading level. Ensure it is placed within
 * a proper document hierarchy where h3 is semantically correct (e.g., under
 * a main h1 or section h2 heading).
 *
 * @example
 * ```tsx
 * <ErrorState
 *   title="Failed to load data"
 *   message="Unable to connect to server"
 *   onRetry={() => refetch()}
 * />
 * ```
 */
export function ErrorState({ icon, title, message, onRetry, className }: ErrorStateProps) {
  return (
    <div className={cn('flex flex-col items-center justify-center p-12 text-center', className)}>
      <div className="mb-4">
        {icon || <DefaultErrorIcon />}
      </div>

      <h3 className="text-lg font-semibold text-red-600 mb-2">
        {title}
      </h3>

      {message && (
        <p className="text-sm text-gray-500 max-w-md mb-6">
          {message}
        </p>
      )}

      {onRetry && (
        <Button
          variant="primary"
          onClick={onRetry}
        >
          Try Again
        </Button>
      )}
    </div>
  );
}
