/**
 * Toast Notification Context
 * Provides global toast notifications for success, error, warning, and info messages
 */

import { createContext, useContext, useState, useCallback, ReactNode, useEffect, useRef } from 'react';

// ============================================================================
// Constants
// ============================================================================

const DEFAULT_SUCCESS_DURATION_MS = 5000;
const DEFAULT_ERROR_DURATION_MS = 7000;
const DEFAULT_WARNING_DURATION_MS = 6000;
const DEFAULT_INFO_DURATION_MS = 5000;

// ============================================================================
// Types
// ============================================================================

export type ToastVariant = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  title: string;
  message: string;
  variant: ToastVariant;
  duration?: number; // milliseconds, undefined = no auto-dismiss
}

interface ToastContextValue {
  toasts: Toast[];
  addToast: (toast: Omit<Toast, 'id'>) => void;
  removeToast: (id: string) => void;
  success: (title: string, message: string, duration?: number) => void;
  error: (title: string, message: string, duration?: number) => void;
  warning: (title: string, message: string, duration?: number) => void;
  info: (title: string, message: string, duration?: number) => void;
}

// ============================================================================
// Context
// ============================================================================

const ToastContext = createContext<ToastContextValue | null>(null);

// ============================================================================
// Provider Component
// ============================================================================

interface ToastProviderProps {
  children: ReactNode;
}

let toastIdCounter = 0;

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const timeoutsRef = useRef<Map<string, NodeJS.Timeout>>(new Map());
  const mountedRef = useRef(true);

  const removeToast = useCallback((id: string) => {
    // Clear timeout if exists
    const timeoutId = timeoutsRef.current.get(id);
    if (timeoutId) {
      clearTimeout(timeoutId);
      timeoutsRef.current.delete(id);
    }
    // Only update state if still mounted
    if (mountedRef.current) {
      setToasts(prev => prev.filter(toast => toast.id !== id));
    }
  }, []);

  const addToast = useCallback(
    (toast: Omit<Toast, 'id'>) => {
      const id = `toast-${++toastIdCounter}`;
      const newToast: Toast = { ...toast, id };

      setToasts(prev => [...prev, newToast]);

      // Auto-dismiss after duration
      if (toast.duration !== undefined) {
        const timeoutId = setTimeout(() => {
          removeToast(id);
        }, toast.duration);
        timeoutsRef.current.set(id, timeoutId);
      }
    },
    [removeToast]
  );

  // Cleanup all timeouts on unmount
  useEffect(() => {
    const timeouts = timeoutsRef.current;
    return () => {
      mountedRef.current = false;
      timeouts.forEach(timeoutId => clearTimeout(timeoutId));
      timeouts.clear();
    };
  }, []);

  const success = useCallback(
    (title: string, message: string, duration = DEFAULT_SUCCESS_DURATION_MS) => {
      addToast({ title, message, variant: 'success', duration });
    },
    [addToast]
  );

  const error = useCallback(
    (title: string, message: string, duration = DEFAULT_ERROR_DURATION_MS) => {
      addToast({ title, message, variant: 'error', duration });
    },
    [addToast]
  );

  const warning = useCallback(
    (title: string, message: string, duration = DEFAULT_WARNING_DURATION_MS) => {
      addToast({ title, message, variant: 'warning', duration });
    },
    [addToast]
  );

  const info = useCallback(
    (title: string, message: string, duration = DEFAULT_INFO_DURATION_MS) => {
      addToast({ title, message, variant: 'info', duration });
    },
    [addToast]
  );

  return (
    <ToastContext.Provider
      value={{
        toasts,
        addToast,
        removeToast,
        success,
        error,
        warning,
        info,
      }}
    >
      {children}
    </ToastContext.Provider>
  );
}

// ============================================================================
// Hook
// ============================================================================

// eslint-disable-next-line react-refresh/only-export-components
export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
}
