/**
 * Schedule Step
 * Step 4: Configure a service time
 */

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { useCreateSchedule } from '@/hooks/useSchedules';
import { useToast } from '@/contexts/ToastContext';

const DAY_OPTIONS = [
  { value: '0', label: 'Sunday' },
  { value: '1', label: 'Monday' },
  { value: '2', label: 'Tuesday' },
  { value: '3', label: 'Wednesday' },
  { value: '4', label: 'Thursday' },
  { value: '5', label: 'Friday' },
  { value: '6', label: 'Saturday' },
];

const CHECKIN_OFFSET_OPTIONS = [
  { value: '15', label: '15 minutes before' },
  { value: '30', label: '30 minutes before' },
  { value: '45', label: '45 minutes before' },
  { value: '60', label: '60 minutes before (default)' },
  { value: '90', label: '90 minutes before' },
  { value: '120', label: '2 hours before' },
];

const scheduleSchema = z.object({
  name: z.string().min(1, 'Schedule name is required').max(100),
  dayOfWeek: z.string().min(1, 'Day is required'),
  timeOfDay: z.string().min(1, 'Service time is required').regex(/^\d{2}:\d{2}$/, 'Enter a valid time'),
  checkInOffsetMinutes: z.string().min(1, 'Check-in window is required'),
});

type ScheduleFormData = z.infer<typeof scheduleSchema>;

interface ScheduleStepProps {
  onNext: (scheduleIdKey: string, scheduleName: string) => void;
  onSkip: () => void;
  onBack: () => void;
}

export function ScheduleStep({ onNext, onSkip, onBack }: ScheduleStepProps) {
  const createSchedule = useCreateSchedule();
  const { error: showError } = useToast();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ScheduleFormData>({
    resolver: zodResolver(scheduleSchema),
    defaultValues: {
      name: 'Sunday Service',
      dayOfWeek: '0',
      timeOfDay: '09:00',
      checkInOffsetMinutes: '60',
    },
  });

  const onSubmit = async (data: ScheduleFormData) => {
    try {
      const result = await createSchedule.mutateAsync({
        name: data.name,
        weeklyDayOfWeek: parseInt(data.dayOfWeek, 10),
        weeklyTimeOfDay: `${data.timeOfDay}:00`, // API expects HH:mm:ss
        checkInStartOffsetMinutes: parseInt(data.checkInOffsetMinutes, 10),
        checkInEndOffsetMinutes: 0,
        isActive: true,
        isPublic: true,
      });
      onNext(result.idKey, result.name);
    } catch {
      showError('Error', 'Failed to create schedule. Please try again.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 bg-orange-100 rounded-full flex items-center justify-center flex-shrink-0">
            <svg className="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900">Configure Service Schedule</h2>
        </div>
        <p className="text-gray-600">
          Set up your primary service time. The check-in window controls when the kiosk opens before service.
          You can add additional service times from Settings later.
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label="Schedule Name"
          placeholder="e.g. Sunday Morning Service"
          error={errors.name?.message}
          {...register('name')}
        />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Select
            label="Day of Week"
            options={DAY_OPTIONS}
            error={errors.dayOfWeek?.message}
            {...register('dayOfWeek')}
          />

          <Input
            label="Service Time"
            type="time"
            error={errors.timeOfDay?.message}
            {...register('timeOfDay')}
          />
        </div>

        <Select
          label="Open Check-in"
          options={CHECKIN_OFFSET_OPTIONS}
          error={errors.checkInOffsetMinutes?.message}
          {...register('checkInOffsetMinutes')}
        />

        <div className="bg-blue-50 border border-blue-100 rounded-lg p-3">
          <p className="text-sm text-blue-800">
            Check-in closes at the service start time. You can adjust these settings from Settings &gt; Schedules after setup.
          </p>
        </div>

        <div className="flex items-center justify-between pt-2">
          <Button type="button" variant="outline" onClick={onBack}>
            Back
          </Button>
          <div className="flex items-center gap-3">
            <Button type="button" variant="ghost" onClick={onSkip}>
              Skip for now
            </Button>
            <Button type="submit" loading={createSchedule.isPending}>
              Create Schedule
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
