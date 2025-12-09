/**
 * Take Attendance Modal Component
 * Modal for recording attendance at a group meeting
 */

import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/Button';
import type { MyGroupMemberDetailDto } from '@/services/api/types';

interface TakeAttendanceModalProps {
  isOpen: boolean;
  onClose: () => void;
  members: MyGroupMemberDetailDto[];
  onSubmit: (occurrenceDate: string, attendedPersonIds: string[]) => Promise<void>;
  isSubmitting: boolean;
}

interface AttendanceState {
  [personIdKey: string]: boolean;
}

export function TakeAttendanceModal({
  isOpen,
  onClose,
  members,
  onSubmit,
  isSubmitting,
}: TakeAttendanceModalProps) {
  // Default to today
  const today = new Date().toISOString().split('T')[0];
  const [occurrenceDate, setOccurrenceDate] = useState(today);
  const [attendance, setAttendance] = useState<AttendanceState>({});

  // Add escape key handler
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  const handleToggleAttendance = (personIdKey: string) => {
    setAttendance((prev) => ({
      ...prev,
      [personIdKey]: !prev[personIdKey],
    }));
  };

  const handleSubmit = async () => {
    const attendedIds = members
      .filter(m => attendance[m.personIdKey])
      .map(m => m.personIdKey);

    await onSubmit(occurrenceDate, attendedIds);

    // Reset form
    setOccurrenceDate(today);
    setAttendance({});
  };

  const handleClose = () => {
    // Reset form
    setOccurrenceDate(today);
    setAttendance({});
    onClose();
  };

  if (!isOpen) return null;

  const attendedCount = Object.values(attendance).filter((a) => a).length;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={handleClose}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="text-lg font-medium text-gray-900">Take Attendance</h3>
                <p className="text-sm text-gray-500 mt-1">
                  {attendedCount} of {members.length} attended
                </p>
              </div>
              <button
                onClick={handleClose}
                className="text-gray-400 hover:text-gray-500"
                aria-label="Close"
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Date Picker */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Meeting Date
              </label>
              <input
                type="date"
                value={occurrenceDate}
                onChange={(e) => setOccurrenceDate(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            {/* Members List */}
            <div className="mb-4 max-h-96 overflow-y-auto border border-gray-200 rounded-lg">
              {members.length === 0 ? (
                <div className="p-4 text-center text-gray-500">No members to record</div>
              ) : (
                <div className="divide-y divide-gray-200">
                  {members.map((member) => (
                    <div key={member.idKey} className="p-3">
                      <div className="flex items-center gap-3">
                        <input
                          type="checkbox"
                          id={`attend-${member.idKey}`}
                          checked={attendance[member.personIdKey] || false}
                          onChange={() => handleToggleAttendance(member.personIdKey)}
                          className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                        />
                        <label
                          htmlFor={`attend-${member.idKey}`}
                          className="flex-1 text-sm font-medium text-gray-900 cursor-pointer"
                        >
                          {member.firstName} {member.lastName}
                          {member.role.isLeader && (
                            <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                              {member.role.name}
                            </span>
                          )}
                        </label>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
            <Button onClick={handleSubmit} loading={isSubmitting} disabled={isSubmitting}>
              Record Attendance
            </Button>
            <Button variant="outline" onClick={handleClose} disabled={isSubmitting}>
              Cancel
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
