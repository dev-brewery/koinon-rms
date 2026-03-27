/**
 * Wizard Step Indicator
 * Progress indicator showing all steps with active/completed state
 */

import { cn } from '@/lib/utils';
import type { WizardStep } from '@/hooks/useSetupWizard';

interface StepConfig {
  key: WizardStep;
  label: string;
  shortLabel: string;
}

const STEPS: StepConfig[] = [
  { key: 'welcome', label: 'Welcome', shortLabel: '1' },
  { key: 'campus', label: 'Campus', shortLabel: '2' },
  { key: 'locations', label: 'Rooms', shortLabel: '3' },
  { key: 'schedule', label: 'Schedule', shortLabel: '4' },
  { key: 'checkin-setup', label: 'Check-in', shortLabel: '5' },
  { key: 'complete', label: 'Complete', shortLabel: '6' },
];

interface WizardStepIndicatorProps {
  currentStep: WizardStep;
  completedSteps: Set<WizardStep>;
}

export function WizardStepIndicator({ currentStep, completedSteps }: WizardStepIndicatorProps) {
  return (
    <nav aria-label="Setup wizard progress">
      <ol className="flex items-center justify-center gap-0">
        {STEPS.map((step, index) => {
          const isActive = step.key === currentStep;
          const isCompleted = completedSteps.has(step.key);
          const isLast = index === STEPS.length - 1;

          return (
            <li key={step.key} className="flex items-center">
              {/* Step bubble */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    'w-9 h-9 rounded-full flex items-center justify-center text-sm font-semibold border-2 transition-colors',
                    isCompleted
                      ? 'bg-green-600 border-green-600 text-white'
                      : isActive
                      ? 'bg-blue-600 border-blue-600 text-white'
                      : 'bg-white border-gray-300 text-gray-500'
                  )}
                  aria-current={isActive ? 'step' : undefined}
                >
                  {isCompleted ? (
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M5 13l4 4L19 7" />
                    </svg>
                  ) : (
                    step.shortLabel
                  )}
                </div>
                <span
                  className={cn(
                    'mt-1 text-xs font-medium hidden sm:block',
                    isActive ? 'text-blue-600' : isCompleted ? 'text-green-600' : 'text-gray-400'
                  )}
                >
                  {step.label}
                </span>
              </div>

              {/* Connector line */}
              {!isLast && (
                <div
                  className={cn(
                    'w-12 h-0.5 mx-1 mb-4 sm:mb-5 transition-colors',
                    isCompleted ? 'bg-green-400' : 'bg-gray-200'
                  )}
                  aria-hidden="true"
                />
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
