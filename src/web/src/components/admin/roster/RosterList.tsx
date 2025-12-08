/**
 * RosterList Component
 * Displays the list of children currently checked into a room
 */

import { useState } from 'react';
import type { RoomRosterDto } from '@/services/api/types';
import { ChildCard } from './ChildCard';
import { Card } from '@/components/ui';

interface RosterListProps {
  roster?: RoomRosterDto;
  isLoading: boolean;
}

type SortBy = 'name' | 'checkInTime' | 'age';
type SortDirection = 'asc' | 'desc';

export function RosterList({ roster, isLoading }: RosterListProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortBy>('name');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  if (isLoading) {
    return (
      <Card className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-1/4"></div>
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-24 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </Card>
    );
  }

  if (!roster) {
    return null;
  }

  // Filter children by search query
  const filteredChildren = roster.children.filter((child) =>
    child.fullName.toLowerCase().includes(searchQuery.toLowerCase())
  );

  // Sort children
  const sortedChildren = [...filteredChildren].sort((a, b) => {
    let comparison = 0;

    switch (sortBy) {
      case 'name':
        comparison = a.lastName.localeCompare(b.lastName);
        if (comparison === 0) {
          comparison = a.firstName.localeCompare(b.firstName);
        }
        break;
      case 'checkInTime':
        comparison = new Date(a.checkInTime).getTime() - new Date(b.checkInTime).getTime();
        break;
      case 'age':
        comparison = (a.age ?? 0) - (b.age ?? 0);
        break;
    }

    return sortDirection === 'asc' ? comparison : -comparison;
  });

  const capacityPercentage = roster.capacity
    ? (roster.totalCount / roster.capacity) * 100
    : 0;

  return (
    <div className="space-y-4">
      {/* Header with stats */}
      <Card className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-semibold text-gray-900">
              {roster.locationName}
            </h2>
            <p className="text-sm text-gray-600 mt-1">
              {roster.totalCount} {roster.totalCount === 1 ? 'child' : 'children'} checked in
              {roster.capacity && ` / ${roster.capacity} capacity`}
            </p>
          </div>

          {/* Capacity indicator */}
          {roster.capacity && (
            <div className="text-right">
              <div
                className={`text-2xl font-bold ${
                  roster.isAtCapacity
                    ? 'text-red-600'
                    : roster.isNearCapacity
                    ? 'text-yellow-600'
                    : 'text-green-600'
                }`}
              >
                {Math.round(capacityPercentage)}%
              </div>
              <div className="w-32 h-2 bg-gray-200 rounded-full mt-2">
                <div
                  className={`h-2 rounded-full ${
                    roster.isAtCapacity
                      ? 'bg-red-500'
                      : roster.isNearCapacity
                      ? 'bg-yellow-500'
                      : 'bg-green-500'
                  }`}
                  style={{ width: `${Math.min(capacityPercentage, 100)}%` }}
                ></div>
              </div>
            </div>
          )}
        </div>
      </Card>

      {/* Search and sort controls */}
      <Card className="p-4">
        <div className="flex flex-col sm:flex-row gap-4">
          {/* Search */}
          <div className="flex-1">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search children..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          {/* Sort */}
          <div className="flex gap-2">
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as SortBy)}
              className="px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500"
            >
              <option value="name">Sort by Name</option>
              <option value="checkInTime">Sort by Check-in Time</option>
              <option value="age">Sort by Age</option>
            </select>

            <button
              onClick={() =>
                setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc')
              }
              className="px-3 py-2 border border-gray-300 rounded-md shadow-sm hover:bg-gray-50"
              title={`Sort ${sortDirection === 'asc' ? 'descending' : 'ascending'}`}
            >
              <svg
                className={`w-5 h-5 transform ${
                  sortDirection === 'desc' ? 'rotate-180' : ''
                }`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M7 11l5-5m0 0l5 5m-5-5v12"
                />
              </svg>
            </button>
          </div>
        </div>
      </Card>

      {/* Children list */}
      {sortedChildren.length === 0 ? (
        <Card className="p-12 text-center">
          <svg
            className="w-16 h-16 text-gray-400 mx-auto mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
            />
          </svg>
          <p className="text-gray-600">
            {searchQuery ? 'No children match your search' : 'No children checked in'}
          </p>
        </Card>
      ) : (
        <div className="space-y-3">
          {sortedChildren.map((child) => (
            <ChildCard key={child.attendanceIdKey} child={child} />
          ))}
        </div>
      )}
    </div>
  );
}
