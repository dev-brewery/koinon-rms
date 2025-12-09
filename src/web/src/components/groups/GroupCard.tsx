/**
 * GroupCard Component
 * Displays a single public group in card format
 */

import { useState } from 'react';
import type { PublicGroupDto } from '@/services/api/publicGroups';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { RequestToJoinModal } from './RequestToJoinModal';

export interface GroupCardProps {
  group: PublicGroupDto;
  onClick?: (group: PublicGroupDto) => void;
  className?: string;
  showRequestButton?: boolean;
}

export function GroupCard({ group, onClick, className, showRequestButton = true }: GroupCardProps) {
  const [showRequestModal, setShowRequestModal] = useState(false);

  const handleCardClick = () => {
    if (onClick) {
      onClick(group);
    }
  };

  const handleRequestClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    setShowRequestModal(true);
  };

  const Component = onClick ? 'button' : 'div';

  return (
    <>
      <Component
        onClick={handleCardClick}
        className={cn(
          'bg-white border border-gray-200 rounded-lg p-6 hover:shadow-lg transition-shadow text-left',
          onClick && 'cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500',
          className
        )}
      >
      {/* Header */}
      <div className="mb-4">
        <h3 className="text-xl font-semibold text-gray-900 mb-2">
          {group.name}
        </h3>
        {group.publicDescription && (
          <p className="text-sm text-gray-600 line-clamp-3">
            {group.publicDescription}
          </p>
        )}
      </div>

      {/* Badges */}
      <div className="flex flex-wrap gap-2 mb-4">
        {group.campusName && (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-blue-50 text-blue-700 text-xs font-medium rounded-full">
            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"
              />
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"
              />
            </svg>
            {group.campusName}
          </span>
        )}

        {group.groupTypeName && (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-purple-50 text-purple-700 text-xs font-medium rounded-full">
            <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"
              />
            </svg>
            {group.groupTypeName}
          </span>
        )}

        {group.hasOpenings ? (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-full">
            <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                clipRule="evenodd"
              />
            </svg>
            Open
          </span>
        ) : (
          <span className="inline-flex items-center gap-1 px-3 py-1 bg-gray-100 text-gray-700 text-xs font-medium rounded-full">
            Full
          </span>
        )}
      </div>

      {/* Meeting Schedule */}
      {group.meetingScheduleSummary && (
        <div className="flex items-start gap-2 text-sm text-gray-700 mb-3">
          <svg className="w-4 h-4 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
            />
          </svg>
          <span>{group.meetingScheduleSummary}</span>
        </div>
      )}

      {/* Member Count */}
      <div className="flex items-center gap-2 text-sm text-gray-700">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
          />
        </svg>
        <span>
          {group.memberCount}
          {group.capacity !== null && group.capacity !== undefined && ` / ${group.capacity}`}
          {' members'}
        </span>
      </div>

      {/* Request to Join Button */}
      {showRequestButton && group.hasOpenings && (
        <div className="mt-4 pt-4 border-t border-gray-200">
          <Button
            onClick={handleRequestClick}
            size="sm"
            className="w-full"
          >
            Request to Join
          </Button>
        </div>
      )}
    </Component>

    {/* Request to Join Modal */}
    <RequestToJoinModal
      isOpen={showRequestModal}
      onClose={() => setShowRequestModal(false)}
      groupIdKey={group.idKey}
      groupName={group.name}
    />
  </>
  );
}
