/**
 * Audit Log Filters Component
 * Provides filtering controls for audit log search
 */

import { useState } from 'react';
import { cn } from '@/lib/utils';
import type { AuditLogSearchParams, AuditAction } from '@/services/api/types';
import { PersonSearchBar } from '@/components/admin/people/PersonSearchBar';

// ============================================================================
// Types
// ============================================================================

export interface AuditLogFiltersProps {
  filters: AuditLogSearchParams;
  onFiltersChange: (filters: AuditLogSearchParams) => void;
}

type PresetOption = 'today' | 'last7' | 'last30' | 'custom';

// ============================================================================
// Component
// ============================================================================

export function AuditLogFilters({ filters, onFiltersChange }: AuditLogFiltersProps) {
  const [selectedPreset, setSelectedPreset] = useState<PresetOption>('last7');
  const [isCustom, setIsCustom] = useState(false);

  const handlePresetChange = (preset: PresetOption) => {
    setSelectedPreset(preset);

    if (preset === 'custom') {
      setIsCustom(true);
      return;
    }

    setIsCustom(false);

    const today = new Date();
    const endDate = today.toISOString().split('T')[0];
    let startDate: string;

    switch (preset) {
      case 'today':
        startDate = endDate;
        break;
      case 'last7':
        startDate = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0];
        break;
      case 'last30':
        startDate = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
          .toISOString()
          .split('T')[0];
        break;
      default:
        return;
    }

    onFiltersChange({
      ...filters,
      startDate,
      endDate,
    });
  };

  const handleCustomDateChange = (field: 'startDate' | 'endDate', dateValue: string) => {
    onFiltersChange({
      ...filters,
      [field]: dateValue,
    });
  };

  const handleEntityTypeChange = (value: string) => {
    onFiltersChange({
      ...filters,
      entityType: value || undefined,
    });
  };

  const handleActionTypeChange = (value: string) => {
    onFiltersChange({
      ...filters,
      actionType: value ? (value as AuditAction) : undefined,
    });
  };

  const handlePersonSearch = (value: string) => {
    onFiltersChange({
      ...filters,
      personIdKey: value || undefined,
    });
  };

  const handleClearFilters = () => {
    onFiltersChange({
      page: 1,
      pageSize: filters.pageSize || 20,
    });
    setSelectedPreset('last7');
    setIsCustom(false);
  };

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-6">
      {/* Date Range Presets */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-3">
          Date Range
        </label>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => handlePresetChange('today')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md transition-colors',
              selectedPreset === 'today' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Today
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('last7')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md transition-colors',
              selectedPreset === 'last7' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Last 7 Days
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('last30')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md transition-colors',
              selectedPreset === 'last30' && !isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Last 30 Days
          </button>
          <button
            type="button"
            onClick={() => handlePresetChange('custom')}
            className={cn(
              'px-4 py-2 text-sm font-medium rounded-md transition-colors',
              isCustom
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            )}
          >
            Custom
          </button>
        </div>
      </div>

      {/* Custom Date Range */}
      {isCustom && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="startDate"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              Start Date
            </label>
            <input
              type="date"
              id="startDate"
              value={filters.startDate || ''}
              onChange={(e) => handleCustomDateChange('startDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label
              htmlFor="endDate"
              className="block text-sm font-medium text-gray-700 mb-1"
            >
              End Date
            </label>
            <input
              type="date"
              id="endDate"
              value={filters.endDate || ''}
              onChange={(e) => handleCustomDateChange('endDate', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      )}

      {/* Entity Type Filter */}
      <div>
        <label
          htmlFor="entityType"
          className="block text-sm font-medium text-gray-700 mb-1"
        >
          Entity Type
        </label>
        <select
          id="entityType"
          value={filters.entityType || ''}
          onChange={(e) => handleEntityTypeChange(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Entity Types</option>
          <option value="Person">Person</option>
          <option value="Family">Family</option>
          <option value="Group">Group</option>
          <option value="GroupMember">Group Member</option>
          <option value="Attendance">Attendance</option>
          <option value="Communication">Communication</option>
          <option value="FinancialBatch">Financial Batch</option>
          <option value="FinancialTransaction">Financial Transaction</option>
        </select>
      </div>

      {/* Action Type Filter */}
      <div>
        <label
          htmlFor="actionType"
          className="block text-sm font-medium text-gray-700 mb-1"
        >
          Action Type
        </label>
        <select
          id="actionType"
          value={filters.actionType || ''}
          onChange={(e) => handleActionTypeChange(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">All Actions</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
          <option value="View">View</option>
          <option value="Export">Export</option>
          <option value="Login">Login</option>
          <option value="Logout">Logout</option>
          <option value="Search">Search</option>
          <option value="Other">Other</option>
        </select>
      </div>

      {/* Person Search */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          User (who performed action)
        </label>
        <PersonSearchBar
          value={filters.personIdKey || ''}
          onChange={handlePersonSearch}
          placeholder="Search by user name..."
        />
      </div>

      {/* Clear Filters Button */}
      <div className="pt-4 border-t border-gray-200">
        <button
          type="button"
          onClick={handleClearFilters}
          className="w-full px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 transition-colors"
        >
          Clear All Filters
        </button>
      </div>
    </div>
  );
}
