/**
 * Follow-up card component for individual follow-up items
 */

import { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';
import { formatDateTime } from '@/lib/utils';
import { FollowUpStatus, type FollowUpDto } from './api';
import { useUpdateFollowUpStatus } from './hooks';

interface FollowUpCardProps {
  followUp: FollowUpDto;
  className?: string;
}

const statusConfig: Record<
  FollowUpStatus,
  { label: string; className: string; bgClassName: string }
> = {
  [FollowUpStatus.Pending]: {
    label: 'Pending',
    className: 'text-yellow-800',
    bgClassName: 'bg-yellow-100',
  },
  [FollowUpStatus.Contacted]: {
    label: 'Contacted',
    className: 'text-blue-800',
    bgClassName: 'bg-blue-100',
  },
  [FollowUpStatus.NoResponse]: {
    label: 'No Response',
    className: 'text-gray-800',
    bgClassName: 'bg-gray-100',
  },
  [FollowUpStatus.Connected]: {
    label: 'Connected',
    className: 'text-green-800',
    bgClassName: 'bg-green-100',
  },
  [FollowUpStatus.Declined]: {
    label: 'Declined',
    className: 'text-red-800',
    bgClassName: 'bg-red-100',
  },
};

export function FollowUpCard({ followUp, className }: FollowUpCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [notes, setNotes] = useState(followUp.notes || '');
  const [showActions, setShowActions] = useState(false);
  const updateStatus = useUpdateFollowUpStatus();

  const statusInfo = statusConfig[followUp.status];

  const handleStatusChange = (newStatus: FollowUpStatus) => {
    updateStatus.mutate(
      {
        idKey: followUp.idKey,
        status: newStatus,
        notes: isEditing ? notes : followUp.notes,
      },
      {
        onSuccess: () => {
          setShowActions(false);
          setIsEditing(false);
        },
      }
    );
  };

  const handleSaveNotes = () => {
    updateStatus.mutate(
      {
        idKey: followUp.idKey,
        status: followUp.status,
        notes,
      },
      {
        onSuccess: () => {
          setIsEditing(false);
        },
      }
    );
  };

  return (
    <Card className={cn('relative', className)}>
      <div className="flex flex-col gap-3">
        {/* Header */}
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-gray-900">
              {followUp.personName}
            </h3>
            <p className="text-sm text-gray-500">
              Created {formatDateTime(followUp.createdDateTime)}
            </p>
            {followUp.assignedToName && (
              <p className="text-sm text-gray-600 mt-1">
                Assigned to: <span className="font-medium">{followUp.assignedToName}</span>
              </p>
            )}
          </div>

          {/* Status Badge */}
          <span
            className={cn(
              'px-3 py-1 rounded-full text-xs font-medium',
              statusInfo.className,
              statusInfo.bgClassName
            )}
          >
            {statusInfo.label}
          </span>
        </div>

        {/* Notes Section */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Notes
          </label>
          {isEditing ? (
            <div className="space-y-2">
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="w-full min-h-[80px] px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                placeholder="Add notes about this follow-up..."
              />
              <div className="flex gap-2">
                <Button size="sm" onClick={handleSaveNotes} disabled={updateStatus.isPending}>
                  Save Notes
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => {
                    setIsEditing(false);
                    setNotes(followUp.notes || '');
                  }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          ) : (
            <div>
              <p className="text-sm text-gray-600 mb-2">
                {followUp.notes || 'No notes yet'}
              </p>
              <Button size="sm" variant="ghost" onClick={() => setIsEditing(true)}>
                Edit Notes
              </Button>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div className="border-t pt-3">
          {showActions ? (
            <div className="space-y-2">
              <p className="text-sm font-medium text-gray-700 mb-2">Update Status:</p>
              <div className="grid grid-cols-2 gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleStatusChange(FollowUpStatus.Contacted)}
                  disabled={updateStatus.isPending}
                >
                  Mark Contacted
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleStatusChange(FollowUpStatus.Connected)}
                  disabled={updateStatus.isPending}
                >
                  Mark Connected
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleStatusChange(FollowUpStatus.NoResponse)}
                  disabled={updateStatus.isPending}
                >
                  No Response
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={() => handleStatusChange(FollowUpStatus.Declined)}
                  disabled={updateStatus.isPending}
                >
                  Declined
                </Button>
              </div>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setShowActions(false)}
                className="w-full"
              >
                Cancel
              </Button>
            </div>
          ) : (
            <Button
              size="sm"
              variant="primary"
              onClick={() => setShowActions(true)}
              className="w-full"
              disabled={followUp.status === FollowUpStatus.Connected ||
                       followUp.status === FollowUpStatus.Declined}
            >
              Update Status
            </Button>
          )}
        </div>

        {/* Timestamps */}
        {followUp.contactedDateTime && (
          <p className="text-xs text-gray-500">
            Contacted: {formatDateTime(followUp.contactedDateTime)}
          </p>
        )}
        {followUp.completedDateTime && (
          <p className="text-xs text-gray-500">
            Completed: {formatDateTime(followUp.completedDateTime)}
          </p>
        )}
      </div>
    </Card>
  );
}
