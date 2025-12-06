import React from 'react';
import { cn } from '@/lib/utils';

export interface CardProps {
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}

export function Card({ children, className, onClick }: CardProps) {
  const Component = onClick ? 'button' : 'div';

  return (
    <Component
      onClick={onClick}
      className={cn(
        'bg-white rounded-lg shadow-md p-4',
        onClick && 'hover:shadow-lg transition-shadow cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500',
        className
      )}
    >
      {children}
    </Component>
  );
}
