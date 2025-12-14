import { cn } from '@/lib/utils';

export interface LoadingProps {
  variant?: 'spinner' | 'dots' | 'skeleton';
  size?: 'sm' | 'md' | 'lg';
  text?: string;
  className?: string;
}

const spinnerSizes = {
  sm: 'w-4 h-4',
  md: 'w-8 h-8',
  lg: 'w-12 h-12',
};

const dotSizes = {
  sm: 'w-1.5 h-1.5',
  md: 'w-2.5 h-2.5',
  lg: 'w-4 h-4',
};

const textSizes = {
  sm: 'text-sm',
  md: 'text-base',
  lg: 'text-lg',
};

function Spinner({ size = 'md', className }: { size?: 'sm' | 'md' | 'lg'; className?: string }) {
  return (
    <div
      className={cn(
        'inline-block border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin',
        spinnerSizes[size],
        className
      )}
      role="status"
      aria-label="Loading"
    />
  );
}

function Dots({ size = 'md', className }: { size?: 'sm' | 'md' | 'lg'; className?: string }) {
  const dotClass = cn('rounded-full bg-primary-600 animate-bounce', dotSizes[size]);

  return (
    <div className={cn('flex items-center gap-1.5', className)} role="status" aria-label="Loading">
      <div className={dotClass} style={{ animationDelay: '0ms' }} />
      <div className={dotClass} style={{ animationDelay: '150ms' }} />
      <div className={dotClass} style={{ animationDelay: '300ms' }} />
    </div>
  );
}

function SkeletonLoader({ size = 'md', className }: { size?: 'sm' | 'md' | 'lg'; className?: string }) {
  const heights = {
    sm: 'h-20',
    md: 'h-32',
    lg: 'h-48',
  };

  return (
    <div
      className={cn(
        'w-full rounded-lg bg-gray-200 animate-pulse',
        heights[size],
        className
      )}
      role="status"
      aria-label="Loading"
    />
  );
}

export function Loading({ variant = 'spinner', size = 'md', text, className }: LoadingProps) {
  const LoadingVariant = variant === 'dots' ? Dots : variant === 'skeleton' ? SkeletonLoader : Spinner;

  return (
    <div className={cn('flex flex-col items-center justify-center gap-3', className)} aria-busy="true">
      <LoadingVariant size={size} />
      {text && (
        <p className={cn('text-gray-600', textSizes[size])} aria-live="polite">
          {text}
        </p>
      )}
    </div>
  );
}
