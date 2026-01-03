/**
 * LocationTreeNode Component
 * Recursive component for rendering hierarchical location items
 */

import { useState } from 'react';
import type { LocationDto } from '@/types/location';
import { cn } from '@/lib/utils';

interface LocationTreeNodeProps {
  location: LocationDto;
  level: number;
  onEdit: (location: LocationDto) => void;
  onDelete: (idKey: string) => void;
  onAddChild: (parentIdKey: string) => void;
}

export function LocationTreeNode({
  location,
  level,
  onEdit,
  onDelete,
  onAddChild,
}: LocationTreeNodeProps) {
  const [isExpanded, setIsExpanded] = useState(true);
  const hasChildren = location.children && location.children.length > 0;

  // Calculate capacity display
  const capacityText = (() => {
    if (location.softRoomThreshold && location.firmRoomThreshold) {
      return `Capacity: ${location.softRoomThreshold}/${location.firmRoomThreshold}`;
    } else if (location.firmRoomThreshold) {
      return `Capacity: ${location.firmRoomThreshold}`;
    }
    return null;
  })();

  return (
    <div className="select-none">
      {/* Current Location Row */}
      <div
        className={cn(
          'group flex items-center gap-2 py-2 px-3 rounded hover:bg-gray-50 transition-colors',
          !location.isActive && 'opacity-60'
        )}
        style={{ paddingLeft: `${level * 1.5 + 0.75}rem` }}
      >
        {/* Expand/Collapse Toggle */}
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className={cn(
            'flex-shrink-0 w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 transition-colors',
            !hasChildren && 'invisible'
          )}
          aria-label={isExpanded ? 'Collapse' : 'Expand'}
        >
          <svg
            className={cn(
              'w-4 h-4 text-gray-600 transition-transform',
              isExpanded && 'rotate-90'
            )}
            fill="none"
            strokeWidth="2"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
          </svg>
        </button>

        {/* Location Info */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="font-semibold text-gray-900">{location.name}</span>
            
            {location.campusName && (
              <span className="text-sm text-gray-500">({location.campusName})</span>
            )}
            
            {capacityText && (
              <span className="text-xs text-gray-600 bg-gray-100 px-2 py-0.5 rounded">
                {capacityText}
              </span>
            )}
            
            {!location.isActive && (
              <span className="text-xs font-medium bg-gray-200 text-gray-700 px-2 py-0.5 rounded">
                Inactive
              </span>
            )}
          </div>
        </div>

        {/* Action Buttons (visible on hover) */}
        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            onClick={() => onAddChild(location.idKey)}
            className="p-1.5 rounded hover:bg-gray-200 text-gray-600 hover:text-blue-600 transition-colors"
            aria-label="Add child location"
            title="Add child location"
          >
            <svg className="w-4 h-4" fill="none" strokeWidth="2" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
            </svg>
          </button>

          <button
            onClick={() => onEdit(location)}
            className="p-1.5 rounded hover:bg-gray-200 text-gray-600 hover:text-blue-600 transition-colors"
            aria-label="Edit location"
            title="Edit location"
          >
            <svg className="w-4 h-4" fill="none" strokeWidth="2" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>

          <button
            onClick={() => onDelete(location.idKey)}
            className="p-1.5 rounded hover:bg-gray-200 text-gray-600 hover:text-red-600 transition-colors"
            aria-label="Delete location"
            title="Delete location"
          >
            <svg className="w-4 h-4" fill="none" strokeWidth="2" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </div>

      {/* Recursive Children */}
      {hasChildren && isExpanded && (
        <div>
          {location.children.map((child) => (
            <LocationTreeNode
              key={child.idKey}
              location={child}
              level={level + 1}
              onEdit={onEdit}
              onDelete={onDelete}
              onAddChild={onAddChild}
            />
          ))}
        </div>
      )}
    </div>
  );
}
