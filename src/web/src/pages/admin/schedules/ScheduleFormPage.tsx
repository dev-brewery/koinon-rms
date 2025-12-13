/**
 * Schedule Form Page
 * Create or edit a schedule
 */

import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useSchedule, useCreateSchedule, useUpdateSchedule } from '@/hooks/useSchedules';
import { WeeklySchedulePicker, CheckinWindowPreview } from '@/components/admin/schedules';
import { scheduleFormSchema } from '@/schemas/schedule.schema';

export function ScheduleFormPage() {
  const { idKey } = useParams<{ idKey: string }>();
  const navigate = useNavigate();
  const isEditing = !!idKey;

  const { data: schedule, isLoading } = useSchedule(idKey);
  const createSchedule = useCreateSchedule();
  const updateSchedule = useUpdateSchedule();

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [dayOfWeek, setDayOfWeek] = useState<number | undefined>();
  const [timeOfDay, setTimeOfDay] = useState<string | undefined>();
  const [checkInStartOffset, setCheckInStartOffset] = useState(60);
  const [checkInEndOffset, setCheckInEndOffset] = useState(60);
  const [isActive, setIsActive] = useState(true);
  const [isPublic, setIsPublic] = useState(true);
  const [order, setOrder] = useState(0);
  const [effectiveStartDate, setEffectiveStartDate] = useState('');
  const [effectiveEndDate, setEffectiveEndDate] = useState('');
  const [autoInactivate, setAutoInactivate] = useState(false);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  // Load existing schedule data
  useEffect(() => {
    if (schedule) {
      setName(schedule.name);
      setDescription(schedule.description || '');
      setDayOfWeek(schedule.weeklyDayOfWeek);
      setTimeOfDay(schedule.weeklyTimeOfDay);
      setCheckInStartOffset(schedule.checkInStartOffsetMinutes || 60);
      setCheckInEndOffset(schedule.checkInEndOffsetMinutes || 60);
      setIsActive(schedule.isActive);
      setIsPublic(schedule.isPublic);
      setOrder(schedule.order);
      setEffectiveStartDate(schedule.effectiveStartDate || '');
      setEffectiveEndDate(schedule.effectiveEndDate || '');
      setAutoInactivate(schedule.autoInactivateWhenComplete);
    }
  }, [schedule]);

  const validateField = (fieldName: string, value: unknown) => {
    const formData = {
      name,
      description,
      dayOfWeek,
      timeOfDay,
      checkInStartOffset,
      checkInEndOffset,
      isActive,
      isPublic,
      order,
      effectiveStartDate,
      effectiveEndDate,
      autoInactivate,
      [fieldName]: value,
    };

    const result = scheduleFormSchema.safeParse(formData);
    if (!result.success) {
      const error = result.error.issues.find(issue => issue.path[0] === fieldName);
      if (error) {
        setValidationErrors(prev => ({ ...prev, [fieldName]: error.message }));
      } else {
        setValidationErrors(prev => {
          const { [fieldName]: removed, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setValidationErrors(prev => {
        const { [fieldName]: removed, ...rest } = prev;
        return rest;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate all fields before submit
    const formData = {
      name,
      description,
      dayOfWeek,
      timeOfDay,
      checkInStartOffset,
      checkInEndOffset,
      isActive,
      isPublic,
      order,
      effectiveStartDate,
      effectiveEndDate,
      autoInactivate,
    };

    const result = scheduleFormSchema.safeParse(formData);
    if (!result.success) {
      const errors: Record<string, string> = {};
      result.error.issues.forEach(issue => {
        const fieldName = issue.path[0] as string;
        errors[fieldName] = issue.message;
      });
      setValidationErrors(errors);
      return;
    }

    setValidationErrors({});

    const request = {
      name,
      description: description || undefined,
      weeklyDayOfWeek: dayOfWeek,
      weeklyTimeOfDay: timeOfDay,
      checkInStartOffsetMinutes: checkInStartOffset,
      checkInEndOffsetMinutes: checkInEndOffset,
      isActive,
      isPublic,
      order,
      effectiveStartDate: effectiveStartDate || undefined,
      effectiveEndDate: effectiveEndDate || undefined,
      autoInactivateWhenComplete: autoInactivate,
    };

    try {
      if (isEditing && idKey) {
        const updated = await updateSchedule.mutateAsync({ idKey, request });
        navigate(`/admin/schedules/${updated.idKey}`);
      } else {
        const created = await createSchedule.mutateAsync(request);
        navigate(`/admin/schedules/${created.idKey}`);
      }
    } catch {
      // Error is handled by TanStack Query error state
    }
  };

  if (isEditing && isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="inline-block w-8 h-8 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin" />
      </div>
    );
  }

  const isPending = createSchedule.isPending || updateSchedule.isPending;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to={isEditing ? `/admin/schedules/${idKey}` : '/admin/schedules'}
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900">
            {isEditing ? 'Edit Schedule' : 'Create Schedule'}
          </h1>
          <p className="mt-1 text-gray-600">
            {isEditing ? 'Update schedule details' : 'Add a new service time schedule'}
          </p>
        </div>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Form */}
          <div className="lg:col-span-2 space-y-6">
            {/* Basic Info */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Basic Information</h2>

              <div className="space-y-4">
                {/* Name */}
                <div>
                  <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                    Name <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    id="name"
                    required
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    onBlur={() => validateField('name', name)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    placeholder="e.g., Sunday Morning Service"
                  />
                  {validationErrors.name && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.name}</p>
                  )}
                </div>

                {/* Description */}
                <div>
                  <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    onBlur={() => validateField('description', description)}
                    rows={3}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                    placeholder="Optional description"
                  />
                  {validationErrors.description && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.description}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Schedule Time */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Schedule Time</h2>
              <WeeklySchedulePicker
                dayOfWeek={dayOfWeek}
                timeOfDay={timeOfDay}
                onDayChange={setDayOfWeek}
                onTimeChange={setTimeOfDay}
              />
            </div>

            {/* Check-in Window */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Check-in Window</h2>

              <div className="grid grid-cols-2 gap-4 mb-4">
                {/* Start Offset */}
                <div>
                  <label htmlFor="startOffset" className="block text-sm font-medium text-gray-700 mb-1">
                    Opens Before (minutes)
                  </label>
                  <input
                    type="number"
                    id="startOffset"
                    min="0"
                    value={checkInStartOffset}
                    onChange={(e) => setCheckInStartOffset(Number(e.target.value))}
                    onBlur={() => validateField('checkInStartOffset', checkInStartOffset)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  {validationErrors.checkInStartOffset && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.checkInStartOffset}</p>
                  )}
                </div>

                {/* End Offset */}
                <div>
                  <label htmlFor="endOffset" className="block text-sm font-medium text-gray-700 mb-1">
                    Closes After (minutes)
                  </label>
                  <input
                    type="number"
                    id="endOffset"
                    min="0"
                    value={checkInEndOffset}
                    onChange={(e) => setCheckInEndOffset(Number(e.target.value))}
                    onBlur={() => validateField('checkInEndOffset', checkInEndOffset)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  {validationErrors.checkInEndOffset && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.checkInEndOffset}</p>
                  )}
                </div>
              </div>

              <CheckinWindowPreview
                dayOfWeek={dayOfWeek}
                timeOfDay={timeOfDay}
                checkInStartOffsetMinutes={checkInStartOffset}
                checkInEndOffsetMinutes={checkInEndOffset}
              />
            </div>

            {/* Effective Dates */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Effective Dates</h2>

              <div className="grid grid-cols-2 gap-4">
                {/* Start Date */}
                <div>
                  <label htmlFor="startDate" className="block text-sm font-medium text-gray-700 mb-1">
                    Start Date
                  </label>
                  <input
                    type="date"
                    id="startDate"
                    value={effectiveStartDate}
                    onChange={(e) => setEffectiveStartDate(e.target.value)}
                    onBlur={() => validateField('effectiveStartDate', effectiveStartDate)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  {validationErrors.effectiveStartDate && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.effectiveStartDate}</p>
                  )}
                </div>

                {/* End Date */}
                <div>
                  <label htmlFor="endDate" className="block text-sm font-medium text-gray-700 mb-1">
                    End Date
                  </label>
                  <input
                    type="date"
                    id="endDate"
                    value={effectiveEndDate}
                    onChange={(e) => setEffectiveEndDate(e.target.value)}
                    onBlur={() => validateField('effectiveEndDate', effectiveEndDate)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                  />
                  {validationErrors.effectiveEndDate && (
                    <p className="text-sm text-red-600 mt-1">{validationErrors.effectiveEndDate}</p>
                  )}
                </div>
              </div>

              <div className="mt-4">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={autoInactivate}
                    onChange={(e) => setAutoInactivate(e.target.checked)}
                    className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <span className="ml-2 text-sm text-gray-700">
                    Auto-inactivate when end date is reached
                  </span>
                </label>
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Status */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Status</h2>

              <div className="space-y-3">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={isActive}
                    onChange={(e) => setIsActive(e.target.checked)}
                    className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <span className="ml-2 text-sm text-gray-700">Active</span>
                </label>

                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={isPublic}
                    onChange={(e) => setIsPublic(e.target.checked)}
                    className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                  />
                  <span className="ml-2 text-sm text-gray-700">Public</span>
                </label>
              </div>
            </div>

            {/* Order */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Display Order</h2>

              <div>
                <label htmlFor="order" className="block text-sm font-medium text-gray-700 mb-1">
                  Order
                </label>
                <input
                  type="number"
                  id="order"
                  min="0"
                  value={order}
                  onChange={(e) => setOrder(Number(e.target.value))}
                  onBlur={() => validateField('order', order)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                />
                {validationErrors.order && (
                  <p className="text-sm text-red-600 mt-1">{validationErrors.order}</p>
                )}
                <p className="mt-1 text-xs text-gray-500">
                  Lower numbers appear first
                </p>
              </div>
            </div>

            {/* Actions */}
            <div className="bg-white rounded-lg border border-gray-200 p-6">
              <div className="space-y-3">
                <button
                  type="submit"
                  disabled={isPending || !name}
                  className="w-full px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isPending
                    ? isEditing
                      ? 'Saving...'
                      : 'Creating...'
                    : isEditing
                    ? 'Save Changes'
                    : 'Create Schedule'}
                </button>

                <Link
                  to={isEditing ? `/admin/schedules/${idKey}` : '/admin/schedules'}
                  className="block w-full px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-center"
                >
                  Cancel
                </Link>
              </div>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
