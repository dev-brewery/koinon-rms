/**
 * Setup Wizard State Management Hook
 * Manages multi-step onboarding wizard state and navigation
 */

import { useState, useCallback } from 'react';

export type WizardStep = 'welcome' | 'campus' | 'locations' | 'schedule' | 'checkin-setup' | 'complete';

export interface WizardState {
  currentStep: WizardStep;
  completedSteps: Set<WizardStep>;
  campusIdKey: string | null;
  campusName: string | null;
  locationIdKeys: string[];
  locationNames: string[];
  scheduleIdKey: string | null;
  scheduleName: string | null;
}

const STEP_ORDER: WizardStep[] = ['welcome', 'campus', 'locations', 'schedule', 'checkin-setup', 'complete'];

export function useSetupWizard() {
  const [state, setState] = useState<WizardState>({
    currentStep: 'welcome',
    completedSteps: new Set(),
    campusIdKey: null,
    campusName: null,
    locationIdKeys: [],
    locationNames: [],
    scheduleIdKey: null,
    scheduleName: null,
  });

  const goToStep = useCallback((step: WizardStep) => {
    setState(prev => ({ ...prev, currentStep: step }));
  }, []);

  const markStepComplete = useCallback((step: WizardStep) => {
    setState(prev => ({
      ...prev,
      completedSteps: new Set([...prev.completedSteps, step]),
    }));
  }, []);

  const goToNext = useCallback(() => {
    setState(prev => {
      const currentIndex = STEP_ORDER.indexOf(prev.currentStep);
      const nextIndex = currentIndex + 1;
      if (nextIndex >= STEP_ORDER.length) return prev;

      const nextStep = STEP_ORDER[nextIndex];
      return {
        ...prev,
        currentStep: nextStep,
        completedSteps: new Set([...prev.completedSteps, prev.currentStep]),
      };
    });
  }, []);

  const goToPrev = useCallback(() => {
    setState(prev => {
      const currentIndex = STEP_ORDER.indexOf(prev.currentStep);
      const prevIndex = currentIndex - 1;
      if (prevIndex < 0) return prev;
      return { ...prev, currentStep: STEP_ORDER[prevIndex] };
    });
  }, []);

  const setCampus = useCallback((idKey: string, name: string) => {
    setState(prev => ({ ...prev, campusIdKey: idKey, campusName: name }));
  }, []);

  const addLocation = useCallback((idKey: string, name: string) => {
    setState(prev => ({
      ...prev,
      locationIdKeys: [...prev.locationIdKeys, idKey],
      locationNames: [...prev.locationNames, name],
    }));
  }, []);

  const setSchedule = useCallback((idKey: string, name: string) => {
    setState(prev => ({ ...prev, scheduleIdKey: idKey, scheduleName: name }));
  }, []);

  const currentStepIndex = STEP_ORDER.indexOf(state.currentStep);
  const isFirstStep = currentStepIndex === 0;
  const isLastStep = currentStepIndex === STEP_ORDER.length - 1;

  return {
    state,
    stepOrder: STEP_ORDER,
    currentStepIndex,
    isFirstStep,
    isLastStep,
    goToStep,
    goToNext,
    goToPrev,
    markStepComplete,
    setCampus,
    addLocation,
    setSchedule,
  };
}
