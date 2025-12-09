/**
 * Volunteer Scheduler Component
 * Calendar view for assigning volunteers to schedules
 */

import { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ScheduleAssignmentCard } from './ScheduleAssignmentCard';
import type { ScheduleAssignmentDto } from '@/types/volunteer';
import type { GroupMemberDetailDto, GroupScheduleDto } from '@/services/api/types';

interface VolunteerSchedulerProps {
  members: GroupMemberDetailDto[];
  schedules: GroupScheduleDto[];
  assignments: ScheduleAssignmentDto[];
  onCreateAssignments: (memberIdKeys: string[], scheduleIdKey: string, dates: string[]) => void;
  isCreating?: boolean;
}

export function VolunteerScheduler({
  members,
  schedules,
  assignments,
  onCreateAssignments,
  isCreating = false,
}: VolunteerSchedulerProps) {
  const [selectedMembers, setSelectedMembers] = useState<string[]>([]);
  const [selectedSchedule, setSelectedSchedule] = useState<string>('');
  const [selectedDates, setSelectedDates] = useState<string[]>([]);
  const [dateInput, setDateInput] = useState('');

  const handleAddDate = () => {
    if (dateInput && !selectedDates.includes(dateInput)) {
      setSelectedDates([...selectedDates, dateInput].sort());
      setDateInput('');
    }
  };

  const handleRemoveDate = (date: string) => {
    setSelectedDates(selectedDates.filter((d) => d !== date));
  };

  const handleToggleMember = (memberIdKey: string) => {
    if (selectedMembers.includes(memberIdKey)) {
      setSelectedMembers(selectedMembers.filter((id) => id !== memberIdKey));
    } else {
      setSelectedMembers([...selectedMembers, memberIdKey]);
    }
  };

  const handleSubmit = () => {
    if (selectedMembers.length > 0 && selectedSchedule && selectedDates.length > 0) {
      onCreateAssignments(selectedMembers, selectedSchedule, selectedDates);
      // Reset form
      setSelectedMembers([]);
      setSelectedSchedule('');
      setSelectedDates([]);
    }
  };

  const canSubmit =
    selectedMembers.length > 0 &&
    selectedSchedule &&
    selectedDates.length > 0 &&
    !isCreating;

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-6">
      {/* Assignment Form */}
      <Card className="p-6">
        <h2 className="text-xl font-bold text-gray-900 mb-4">Create Assignments</h2>

        {/* Select Members */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Select Members ({selectedMembers.length} selected)
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 max-h-60 overflow-y-auto border border-gray-200 rounded-lg p-3">
            {members.map((member) => (
              <label
                key={member.idKey}
                className="flex items-center gap-2 p-2 hover:bg-gray-50 rounded cursor-pointer"
              >
                <input
                  type="checkbox"
                  checked={selectedMembers.includes(member.idKey)}
                  onChange={() => handleToggleMember(member.idKey)}
                  className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                />
                <span className="text-sm text-gray-900">{member.person.fullName}</span>
                <span className="text-xs text-gray-500">({member.role.name})</span>
              </label>
            ))}
          </div>
        </div>

        {/* Select Schedule */}
        <div className="mb-6">
          <label htmlFor="schedule" className="block text-sm font-medium text-gray-700 mb-2">
            Select Schedule
          </label>
          <select
            id="schedule"
            value={selectedSchedule}
            onChange={(e) => setSelectedSchedule(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value="">-- Select a schedule --</option>
            {schedules.map((schedule) => (
              <option key={schedule.idKey} value={schedule.schedule.idKey}>
                {schedule.schedule.name}
                {schedule.schedule.weeklyTimeOfDay && ` - ${schedule.schedule.weeklyTimeOfDay}`}
              </option>
            ))}
          </select>
        </div>

        {/* Select Dates */}
        <div className="mb-6">
          <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-2">
            Add Dates
          </label>
          <div className="flex gap-2 mb-3">
            <input
              type="date"
              id="date"
              value={dateInput}
              onChange={(e) => setDateInput(e.target.value)}
              className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
            <Button type="button" onClick={handleAddDate} disabled={!dateInput}>
              Add Date
            </Button>
          </div>

          {selectedDates.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {selectedDates.map((date) => (
                <span
                  key={date}
                  className="inline-flex items-center gap-2 px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm"
                >
                  {formatDate(date)}
                  <button
                    type="button"
                    onClick={() => handleRemoveDate(date)}
                    className="text-blue-600 hover:text-blue-800"
                  >
                    ×
                  </button>
                </span>
              ))}
            </div>
          )}
        </div>

        <div className="flex justify-end">
          <Button onClick={handleSubmit} disabled={!canSubmit} loading={isCreating}>
            Create Assignments
          </Button>
        </div>
      </Card>

      {/* Current Assignments */}
      <div>
        <h2 className="text-xl font-bold text-gray-900 mb-4">Current Assignments</h2>
        {assignments.length === 0 ? (
          <Card className="p-6 text-center text-gray-500">
            No assignments yet. Create your first assignment above.
          </Card>
        ) : (
          <div className="space-y-3">
            {assignments.map((assignment) => (
              <ScheduleAssignmentCard
                key={assignment.idKey}
                assignment={assignment}
                showActions={false}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
