/**
 * My Schedule Page
 * Displays logged-in user's upcoming serving assignments
 */

import { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { ScheduleAssignmentCard } from '@/components/volunteer/ScheduleAssignmentCard';
import { ConfirmDeclineModal } from '@/components/volunteer/ConfirmDeclineModal';
import { useMySchedule, useUpdateAssignmentStatus } from '@/hooks/useVolunteerSchedule';
import { VolunteerScheduleStatus } from '@/types/volunteer';
import type { ScheduleAssignmentDto } from '@/types/volunteer';

export function MySchedulePage() {
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [selectedAssignment, setSelectedAssignment] = useState<ScheduleAssignmentDto | null>(null);
  const [modalAction, setModalAction] = useState<'confirm' | 'decline'>('confirm');

  const { data, isLoading, error } = useMySchedule({ startDate, endDate });
  const updateStatusMutation = useUpdateAssignmentStatus();

  const handleConfirm = (assignment: ScheduleAssignmentDto) => {
    setSelectedAssignment(assignment);
    setModalAction('confirm');
  };

  const handleDecline = (assignment: ScheduleAssignmentDto) => {
    setSelectedAssignment(assignment);
    setModalAction('decline');
  };

  const handleSubmitModal = async (status: VolunteerScheduleStatus, declineReason?: string) => {
    if (!selectedAssignment) return;

    await updateStatusMutation.mutateAsync({
      assignmentIdKey: selectedAssignment.idKey,
      request: { status, declineReason },
    });

    setSelectedAssignment(null);
  };

  const handleClearFilters = () => {
    setStartDate('');
    setEndDate('');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading your schedule...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-600 mb-4">Failed to load schedule</p>
          <Button onClick={() => window.location.reload()}>Retry</Button>
        </div>
      </div>
    );
  }

  // Backend returns data already grouped by date
  const scheduleData = data || [];

  // Calculate summary stats
  const allAssignments = scheduleData.flatMap(d => d.assignments);
  const upcomingCount = allAssignments.length;
  const pendingResponseCount = allAssignments.filter(
    a => a.status === VolunteerScheduleStatus.Scheduled || a.status === VolunteerScheduleStatus.NoResponse
  ).length;

  const formatDateHeader = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">My Serving Schedule</h1>
          <p className="mt-2 text-gray-600">
            View and respond to your upcoming serving assignments
          </p>
        </div>

        {/* Summary Cards */}
        {scheduleData.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
            <Card className="p-6">
              <div className="flex items-center gap-4">
                <div className="flex-shrink-0 w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
                  <svg
                    className="w-6 h-6 text-blue-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                    />
                  </svg>
                </div>
                <div>
                  <p className="text-2xl font-bold text-gray-900">{upcomingCount}</p>
                  <p className="text-sm text-gray-600">Upcoming Assignments</p>
                </div>
              </div>
            </Card>

            <Card className="p-6">
              <div className="flex items-center gap-4">
                <div className="flex-shrink-0 w-12 h-12 bg-yellow-100 rounded-full flex items-center justify-center">
                  <svg
                    className="w-6 h-6 text-yellow-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
                <div>
                  <p className="text-2xl font-bold text-gray-900">{pendingResponseCount}</p>
                  <p className="text-sm text-gray-600">Pending Response</p>
                </div>
              </div>
            </Card>
          </div>
        )}

        {/* Date Range Filter */}
        <Card className="p-4 mb-6">
          <div className="flex flex-wrap items-end gap-4">
            <div className="flex-1 min-w-[200px]">
              <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-1">
                Start Date
              </label>
              <input
                type="date"
                id="startDate"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            <div className="flex-1 min-w-[200px]">
              <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-1">
                End Date
              </label>
              <input
                type="date"
                id="endDate"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
            {(startDate || endDate) && (
              <Button variant="outline" onClick={handleClearFilters}>
                Clear Filters
              </Button>
            )}
          </div>
        </Card>

        {/* Assignments List */}
        {scheduleData.length === 0 ? (
          <Card className="p-12 text-center">
            <svg
              className="mx-auto h-12 w-12 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-gray-900">No assignments</h3>
            <p className="mt-1 text-sm text-gray-500">
              You don't have any serving assignments {startDate || endDate ? 'in this date range' : 'yet'}.
            </p>
          </Card>
        ) : (
          <div className="space-y-6">
            {scheduleData.map((dateGroup) => (
              <div key={dateGroup.date}>
                <h2 className="text-lg font-semibold text-gray-900 mb-3">
                  {formatDateHeader(dateGroup.date)}
                </h2>
                <div className="space-y-3">
                  {dateGroup.assignments.map((assignment) => (
                    <ScheduleAssignmentCard
                      key={assignment.idKey}
                      assignment={assignment}
                      onConfirm={() => handleConfirm(assignment)}
                      onDecline={() => handleDecline(assignment)}
                      isUpdating={updateStatusMutation.isPending}
                      showActions={true}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Confirm/Decline Modal */}
      <ConfirmDeclineModal
        isOpen={!!selectedAssignment}
        onClose={() => setSelectedAssignment(null)}
        onSubmit={handleSubmitModal}
        isSubmitting={updateStatusMutation.isPending}
        action={modalAction}
        assignmentInfo={
          selectedAssignment
            ? {
                memberName: selectedAssignment.memberName,
                scheduleName: selectedAssignment.scheduleName,
                date: selectedAssignment.assignedDate,
              }
            : undefined
        }
      />
    </div>
  );
}
