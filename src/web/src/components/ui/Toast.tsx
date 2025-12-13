/**
 * Toast Notification Component
 * Displays temporary notification messages
 */

import { useEffect, useState } from 'react';
import { useToast, type Toast as ToastType } from '../../contexts/ToastContext';
import { cn } from '../../lib/utils';

// ============================================================================
// Constants
// ============================================================================

const ANIMATION_DURATION_MS = 200;

// ============================================================================
// Toast Container
// ============================================================================

export function ToastContainer() {
  const { toasts } = useToast();

  return (
    <div
      className="fixed bottom-0 right-0 z-50 p-4 space-y-3 pointer-events-none"
      aria-live="polite"
      aria-atomic="true"
    >
      {toasts.map(toast => (
        <Toast key={toast.id} toast={toast} />
      ))}
    </div>
  );
}

// ============================================================================
// Individual Toast
// ============================================================================

interface ToastProps {
  toast: ToastType;
}

function Toast({ toast }: ToastProps) {
  const { removeToast } = useToast();
  const [isExiting, setIsExiting] = useState(false);

  const handleClose = () => {
    setIsExiting(true);
  };

  // Auto-dismiss countdown
  useEffect(() => {
    if (toast.duration === undefined) return;

    const timer = setTimeout(() => {
      setIsExiting(true);
    }, toast.duration - ANIMATION_DURATION_MS);

    return () => clearTimeout(timer);
  }, [toast.duration]);

  // Exit animation cleanup
  useEffect(() => {
    if (!isExiting) return;

    const timer = setTimeout(() => {
      removeToast(toast.id);
    }, ANIMATION_DURATION_MS);

    return () => clearTimeout(timer);
  }, [isExiting, removeToast, toast.id]);

  const variantStyles = {
    success: {
      container: 'bg-green-50 border-green-200',
      icon: 'text-green-600',
      title: 'text-green-900',
      message: 'text-green-700',
    },
    error: {
      container: 'bg-red-50 border-red-200',
      icon: 'text-red-600',
      title: 'text-red-900',
      message: 'text-red-700',
    },
    warning: {
      container: 'bg-yellow-50 border-yellow-200',
      icon: 'text-yellow-600',
      title: 'text-yellow-900',
      message: 'text-yellow-700',
    },
    info: {
      container: 'bg-blue-50 border-blue-200',
      icon: 'text-blue-600',
      title: 'text-blue-900',
      message: 'text-blue-700',
    },
  };

  const styles = variantStyles[toast.variant];

  return (
    <div
      role="alert"
      className={cn(
        'pointer-events-auto w-96 max-w-full border rounded-lg shadow-lg p-4',
        'transition-all duration-200',
        isExiting
          ? 'opacity-0 translate-x-full'
          : 'opacity-100 translate-x-0',
        styles.container
      )}
    >
      <div className="flex items-start gap-3">
        {/* Icon */}
        <div className={cn('flex-shrink-0 mt-0.5', styles.icon)}>
          {toast.variant === 'success' && (
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
          )}
          {toast.variant === 'error' && (
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
          )}
          {toast.variant === 'warning' && (
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          )}
          {toast.variant === 'info' && (
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                clipRule="evenodd"
              />
            </svg>
          )}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <p className={cn('text-sm font-medium', styles.title)}>
            {toast.title}
          </p>
          <p className={cn('text-sm mt-1', styles.message)}>
            {toast.message}
          </p>
        </div>

        {/* Close Button */}
        <button
          type="button"
          onClick={handleClose}
          className={cn(
            'flex-shrink-0 rounded-lg p-1.5 inline-flex',
            'hover:bg-black/5 focus:outline-none focus:ring-2 focus:ring-offset-2',
            toast.variant === 'success' && 'focus:ring-green-500',
            toast.variant === 'error' && 'focus:ring-red-500',
            toast.variant === 'warning' && 'focus:ring-yellow-500',
            toast.variant === 'info' && 'focus:ring-blue-500'
          )}
          aria-label="Dismiss notification"
        >
          <svg
            className={cn('w-4 h-4', styles.icon)}
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path
              fillRule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clipRule="evenodd"
            />
          </svg>
        </button>
      </div>
    </div>
  );
}
