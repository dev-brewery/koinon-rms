/**
 * Welcome Step
 * Step 1: Organization name + timezone
 */

import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';

const TIMEZONES = [
  { value: 'America/New_York', label: 'Eastern Time (ET)' },
  { value: 'America/Chicago', label: 'Central Time (CT)' },
  { value: 'America/Denver', label: 'Mountain Time (MT)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (PT)' },
  { value: 'America/Anchorage', label: 'Alaska Time (AKT)' },
  { value: 'Pacific/Honolulu', label: 'Hawaii Time (HT)' },
  { value: 'America/Phoenix', label: 'Arizona (no DST)' },
  { value: 'Europe/London', label: 'London (GMT/BST)' },
  { value: 'Europe/Paris', label: 'Central European Time (CET)' },
  { value: 'Australia/Sydney', label: 'Sydney (AEST)' },
  { value: 'Pacific/Auckland', label: 'New Zealand (NZST)' },
];

const welcomeSchema = z.object({
  organizationName: z.string().min(1, 'Organization name is required').max(100),
  timezone: z.string().min(1, 'Timezone is required'),
});

type WelcomeFormData = z.infer<typeof welcomeSchema>;

const ORG_NAME_KEY = 'koinon_wizard_org_name';
const TIMEZONE_KEY = 'koinon_wizard_timezone';

interface WelcomeStepProps {
  onNext: () => void;
}

export function WelcomeStep({ onNext }: WelcomeStepProps) {
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<WelcomeFormData>({
    resolver: zodResolver(welcomeSchema),
    defaultValues: {
      organizationName: '',
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Chicago',
    },
  });

  // Restore from localStorage on mount
  useEffect(() => {
    const savedName = localStorage.getItem(ORG_NAME_KEY);
    const savedTz = localStorage.getItem(TIMEZONE_KEY);
    if (savedName) setValue('organizationName', savedName);
    if (savedTz) setValue('timezone', savedTz);
  }, [setValue]);

  const onSubmit = (data: WelcomeFormData) => {
    localStorage.setItem(ORG_NAME_KEY, data.organizationName);
    localStorage.setItem(TIMEZONE_KEY, data.timezone);
    onNext();
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="text-center">
        <div className="mx-auto w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mb-4">
          <svg className="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold text-gray-900">Welcome to Koinon RMS</h2>
        <p className="mt-2 text-gray-600 max-w-md mx-auto">
          Let's get your church set up in just a few steps. We'll create your campus, add rooms,
          and configure service times so check-in is ready to go.
        </p>
      </div>

      {/* What we'll set up */}
      <div className="bg-blue-50 border border-blue-100 rounded-lg p-4">
        <h3 className="text-sm font-semibold text-blue-900 mb-2">What this wizard sets up:</h3>
        <ul className="space-y-1 text-sm text-blue-800">
          <li className="flex items-center gap-2">
            <svg className="w-4 h-4 text-blue-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            Your campus location
          </li>
          <li className="flex items-center gap-2">
            <svg className="w-4 h-4 text-blue-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            Check-in rooms (e.g. Nursery, Elementary)
          </li>
          <li className="flex items-center gap-2">
            <svg className="w-4 h-4 text-blue-500 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20" aria-hidden="true">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
            </svg>
            Service schedule and check-in window
          </li>
        </ul>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <Input
          label="Organization Name"
          placeholder="e.g. Grace Community Church"
          error={errors.organizationName?.message}
          {...register('organizationName')}
        />

        <Select
          label="Primary Timezone"
          options={TIMEZONES}
          error={errors.timezone?.message}
          {...register('timezone')}
        />

        <div className="flex justify-end pt-2">
          <Button type="submit" size="lg">
            Get Started
          </Button>
        </div>
      </form>
    </div>
  );
}
