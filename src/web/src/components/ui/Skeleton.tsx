import type { CSSProperties } from 'react';
import { cn } from '@/lib/utils';

export interface SkeletonProps {
  className?: string;
  variant?: 'text' | 'circular' | 'rectangular';
  width?: string | number;
  height?: string | number;
}

function BaseSkeleton({ className, variant = 'rectangular', width, height }: SkeletonProps) {
  const variantClasses = {
    text: 'rounded',
    circular: 'rounded-full',
    rectangular: 'rounded-lg',
  };

  const styles: CSSProperties = {};
  if (width !== undefined) {
    styles.width = typeof width === 'number' ? `${width}px` : width;
  }
  if (height !== undefined) {
    styles.height = typeof height === 'number' ? `${height}px` : height;
  }

  return (
    <div
      className={cn(
        'bg-gray-200 animate-pulse',
        variantClasses[variant],
        className
      )}
      style={styles}
      role="status"
      aria-label="Loading content"
    >
      <span className="sr-only">Loading...</span>
    </div>
  );
}

function SkeletonCard({ className }: { className?: string }) {
  return (
    <div className={cn('space-y-3 p-4', className)}>
      <BaseSkeleton variant="rectangular" height={120} />
      <div className="space-y-2">
        <BaseSkeleton variant="text" height={16} width="75%" />
        <BaseSkeleton variant="text" height={16} width="50%" />
      </div>
    </div>
  );
}

function SkeletonAvatar({ size = 40, className }: { size?: number; className?: string }) {
  return (
    <BaseSkeleton
      variant="circular"
      width={size}
      height={size}
      className={className}
    />
  );
}

function SkeletonText({ width, lines = 1, className }: { width?: string | number; lines?: number; className?: string }) {
  if (lines === 1) {
    return <BaseSkeleton variant="text" height={16} width={width} className={className} />;
  }

  return (
    <div className={cn('space-y-2', className)}>
      {/* Array index as key is safe here: static decorative content that never reorders */}
      {Array.from({ length: lines }).map((_, index) => (
        <BaseSkeleton
          key={index}
          variant="text"
          height={16}
          width={index === lines - 1 ? '75%' : '100%'}
        />
      ))}
    </div>
  );
}

export const Skeleton = Object.assign(BaseSkeleton, {
  Card: SkeletonCard,
  Avatar: SkeletonAvatar,
  Text: SkeletonText,
});
