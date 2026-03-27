/**
 * Setup Wizard Page
 * Full-page guided onboarding wizard for first-time admin setup
 */

import {
  WizardStepIndicator,
  WelcomeStep,
  CampusStep,
  LocationsStep,
  ScheduleStep,
  CheckinSetupStep,
  CompleteStep,
} from '@/components/admin/setup-wizard';
import { useSetupWizard } from '@/hooks/useSetupWizard';

export function SetupWizardPage() {
  const wizard = useSetupWizard();
  const { state, goToNext, goToPrev, setCampus, addLocation, setSchedule } = wizard;

  const handleWelcomeNext = () => {
    goToNext();
  };

  const handleCampusNext = (campusIdKey: string, campusName: string) => {
    setCampus(campusIdKey, campusName);
    goToNext();
  };

  const handleCampusSkip = () => {
    goToNext();
  };

  const handleLocationsNext = (locationIdKeys: string[], locationNames: string[]) => {
    locationIdKeys.forEach((idKey, i) => addLocation(idKey, locationNames[i]));
    goToNext();
  };

  const handleLocationsSkip = () => {
    goToNext();
  };

  const handleScheduleNext = (scheduleIdKey: string, scheduleName: string) => {
    setSchedule(scheduleIdKey, scheduleName);
    goToNext();
  };

  const handleScheduleSkip = () => {
    goToNext();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex flex-col">
      {/* Top bar */}
      <header className="bg-white border-b border-gray-200 px-4 py-4">
        <div className="max-w-2xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            </div>
            <span className="text-lg font-semibold text-gray-900">Koinon RMS Setup</span>
          </div>
          <span className="text-sm text-gray-500">
            Step {wizard.currentStepIndex + 1} of {wizard.stepOrder.length}
          </span>
        </div>
      </header>

      {/* Step indicator */}
      <div className="bg-white border-b border-gray-100 px-4 py-4">
        <div className="max-w-2xl mx-auto">
          <WizardStepIndicator
            currentStep={state.currentStep}
            completedSteps={state.completedSteps}
          />
        </div>
      </div>

      {/* Main content */}
      <main className="flex-1 flex items-start justify-center px-4 py-8">
        <div className="w-full max-w-2xl bg-white rounded-xl shadow-sm border border-gray-200 p-8">
          {state.currentStep === 'welcome' && (
            <WelcomeStep onNext={handleWelcomeNext} />
          )}

          {state.currentStep === 'campus' && (
            <CampusStep
              onNext={handleCampusNext}
              onSkip={handleCampusSkip}
              onBack={goToPrev}
            />
          )}

          {state.currentStep === 'locations' && (
            <LocationsStep
              campusIdKey={state.campusIdKey}
              campusName={state.campusName}
              onNext={handleLocationsNext}
              onSkip={handleLocationsSkip}
              onBack={goToPrev}
            />
          )}

          {state.currentStep === 'schedule' && (
            <ScheduleStep
              onNext={handleScheduleNext}
              onSkip={handleScheduleSkip}
              onBack={goToPrev}
            />
          )}

          {state.currentStep === 'checkin-setup' && (
            <CheckinSetupStep
              campusName={state.campusName}
              locationNames={state.locationNames}
              scheduleName={state.scheduleName}
              onNext={goToNext}
              onBack={goToPrev}
            />
          )}

          {state.currentStep === 'complete' && (
            <CompleteStep
              campusName={state.campusName}
              locationNames={state.locationNames}
              scheduleName={state.scheduleName}
            />
          )}
        </div>
      </main>
    </div>
  );
}
