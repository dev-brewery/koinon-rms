import React, { useState } from 'react';
import {
  KioskLayout,
  PhoneSearch,
  FamilySearch,
  FamilyMemberList,
  CheckinConfirmation,
  IdleWarningModal,
} from '@/components/checkin';
import type { OpportunitySelection } from '@/components/checkin';
import { Button, Card } from '@/components/ui';
import {
  useCheckinSearch,
  useCheckinOpportunities,
  useRecordAttendance,
} from '@/hooks/useCheckin';
import { useIdleTimeout } from '@/hooks/useIdleTimeout';
import type { CheckinFamilyDto, CheckinRequestItem } from '@/services/api/types';

type CheckinStep = 'search' | 'select-family' | 'select-members' | 'confirmation';
type SearchMode = 'phone' | 'name';

// Idle timeout configuration
const IDLE_CONFIG = {
  timeout: 60 * 1000, // 60 seconds total
  warningTime: 50 * 1000, // Warning at 50 seconds (10s countdown)
};

/**
 * Helper to get total count of selected activities across all people.
 * Centralizes logic used in both UI display and check-in submission.
 */
const getTotalActivitiesCount = (selectedCheckins: Map<string, OpportunitySelection[]>): number => {
  let count = 0;
  selectedCheckins.forEach((selections) => {
    if (Array.isArray(selections)) {
      count += selections.length;
    }
  });
  return count;
};

/**
 * Creates a unique key for identifying a specific opportunity selection.
 */
const createSelectionKey = (groupId: string, locationId: string, scheduleId: string): string =>
  `${groupId}|${locationId}|${scheduleId}`;

export function CheckinPage() {
  // State
  const [step, setStep] = useState<CheckinStep>('search');
  const [searchMode, setSearchMode] = useState<SearchMode>('phone');
  const [searchValue, setSearchValue] = useState<string>('');
  const [selectedFamily, setSelectedFamily] = useState<CheckinFamilyDto | null>(null);
  const [selectedCheckins, setSelectedCheckins] = useState<
    Map<string, OpportunitySelection[]>
  >(new Map());

  // Queries
  const searchQuery = useCheckinSearch(
    searchValue,
    searchMode === 'phone' ? 'Phone' : 'Name'
  );

  const opportunitiesQuery = useCheckinOpportunities(selectedFamily?.idKey);

  const recordAttendanceMutation = useRecordAttendance();

  // Handlers
  const handleSearch = (value: string) => {
    setSearchValue(value);
    // When query succeeds, move to family selection if multiple results
    // or directly to member selection if single result
  };

  // Effect: Auto-advance when search returns single family
  React.useEffect(() => {
    if (searchQuery.data && searchQuery.data.length === 1 && step === 'search') {
      setSelectedFamily(searchQuery.data[0]);
      setStep('select-members');
    } else if (searchQuery.data && searchQuery.data.length > 1 && step === 'search') {
      setStep('select-family');
    }
  }, [searchQuery.data, step]);

  const handleSelectFamily = (family: CheckinFamilyDto) => {
    setSelectedFamily(family);
    setStep('select-members');
  };

  const handleToggleCheckin = (
    personId: string,
    groupId: string,
    locationId: string,
    scheduleId: string,
    groupName: string,
    locationName: string,
    scheduleName: string,
    startTime: string
  ) => {
    const newSelected = new Map(selectedCheckins);
    const existingSelections = newSelected.get(personId) || [];

    // Check if this opportunity is already selected using consistent key
    const selectionKey = createSelectionKey(groupId, locationId, scheduleId);
    const selectionIndex = existingSelections.findIndex(
      (sel) => createSelectionKey(sel.groupId, sel.locationId, sel.scheduleId) === selectionKey
    );

    if (selectionIndex >= 0) {
      // Deselect - remove this opportunity
      const updatedSelections = existingSelections.filter((_, idx) => idx !== selectionIndex);
      if (updatedSelections.length === 0) {
        newSelected.delete(personId);
      } else {
        newSelected.set(personId, updatedSelections);
      }
    } else {
      // Select - add this opportunity
      const newSelection: OpportunitySelection = {
        groupId,
        locationId,
        scheduleId,
        groupName,
        locationName,
        scheduleName,
        startTime,
      };
      newSelected.set(personId, [...existingSelections, newSelection]);
    }

    setSelectedCheckins(newSelected);
  };

  const handleCheckIn = async () => {
    // Flatten all selections into a single array of check-in items
    const checkins: CheckinRequestItem[] = [];

    selectedCheckins.forEach((selections, personIdKey) => {
      selections.forEach((selection) => {
        checkins.push({
          personIdKey,
          groupIdKey: selection.groupId,
          locationIdKey: selection.locationId,
          scheduleIdKey: selection.scheduleId,
        });
      });
    });

    try {
      await recordAttendanceMutation.mutateAsync({ checkins });
      setStep('confirmation');
    } catch (error) {
      console.error('Check-in failed:', error);
      // Error handling would go here
    }
  };

  const handleReset = () => {
    setStep('search');
    setSearchValue('');
    setSelectedFamily(null);
    setSelectedCheckins(new Map());
  };

  const handleDone = () => {
    handleReset();
  };

  // Idle timeout - only active when NOT on search screen
  const { isWarning, secondsRemaining, resetTimer } = useIdleTimeout({
    timeout: IDLE_CONFIG.timeout,
    warningTime: IDLE_CONFIG.warningTime,
    onTimeout: handleReset,
    enabled: step !== 'search',
  });

  // Render
  return (
    <>
      <KioskLayout
        title={
          step === 'select-members' && selectedFamily
            ? selectedFamily.name
            : undefined
        }
        onReset={step !== 'search' ? handleReset : undefined}
      >
      {/* Step 1: Search */}
      {step === 'search' && (
        <div className="space-y-6">
          {/* Search Mode Toggle */}
          <div className="flex justify-center gap-4 mb-8">
            <button
              onClick={() => setSearchMode('phone')}
              className={`px-8 py-4 rounded-lg font-semibold transition-colors min-h-[56px] ${
                searchMode === 'phone'
                  ? 'bg-blue-600 text-white'
                  : 'bg-white text-gray-700 border-2 border-gray-300'
              }`}
            >
              Search by Phone
            </button>
            <button
              onClick={() => setSearchMode('name')}
              className={`px-8 py-4 rounded-lg font-semibold transition-colors min-h-[56px] ${
                searchMode === 'name'
                  ? 'bg-blue-600 text-white'
                  : 'bg-white text-gray-700 border-2 border-gray-300'
              }`}
            >
              Search by Name
            </button>
          </div>

          {/* Search Component */}
          {searchMode === 'phone' ? (
            <PhoneSearch onSearch={handleSearch} loading={searchQuery.isFetching} />
          ) : (
            <FamilySearch onSearch={handleSearch} loading={searchQuery.isFetching} />
          )}

          {/* Error */}
          {searchQuery.isError && (
            <div className="max-w-2xl mx-auto mt-4">
              <Card className="bg-red-50 border border-red-200">
                <p className="text-red-800 text-center">
                  Search failed. Please try again.
                </p>
              </Card>
            </div>
          )}

          {/* No Results */}
          {searchQuery.data && searchQuery.data.length === 0 && (
            <div className="max-w-2xl mx-auto mt-4">
              <Card className="bg-yellow-50 border border-yellow-200">
                <p className="text-yellow-900 text-center font-medium">
                  No families found. Please try a different search.
                </p>
              </Card>
            </div>
          )}
        </div>
      )}

      {/* Step 2: Select Family (if multiple results) */}
      {step === 'select-family' && searchQuery.data && (
        <div className="max-w-4xl mx-auto">
          <h2 className="text-3xl font-bold text-center mb-8 text-gray-900">
            Select Your Family
          </h2>
          <div className="grid gap-4">
            {searchQuery.data.map((family) => (
              <Card
                key={family.idKey}
                onClick={() => handleSelectFamily(family)}
                className="hover:shadow-xl transition-shadow cursor-pointer p-6"
              >
                <h3 className="text-2xl font-bold text-gray-900 mb-2">
                  {family.name}
                </h3>
                <p className="text-gray-600 mb-3">
                  {family.members.length}{' '}
                  {family.members.length === 1 ? 'member' : 'members'}
                </p>
                <div className="flex flex-wrap gap-2">
                  {family.members.map((member) => (
                    <span
                      key={member.idKey}
                      className="bg-gray-100 px-3 py-1 rounded-full text-sm"
                    >
                      {member.fullName}
                    </span>
                  ))}
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Step 3: Select Members */}
      {step === 'select-members' && opportunitiesQuery.data && (
        <div className="max-w-4xl mx-auto">
          <h2 className="text-3xl font-bold text-center mb-8 text-gray-900">
            Who's Checking In?
          </h2>

          {opportunitiesQuery.isLoading && (
            <div className="text-center py-12">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
              <p className="mt-4 text-gray-600">Loading options...</p>
            </div>
          )}

          {opportunitiesQuery.data && (
            <>
              <FamilyMemberList
                opportunities={opportunitiesQuery.data.opportunities}
                selectedCheckins={selectedCheckins}
                onToggleCheckin={handleToggleCheckin}
              />

              {/* Check-in Button */}
              <div className="mt-8 sticky bottom-0 bg-gradient-to-t from-blue-100 via-blue-100 to-transparent pt-6 pb-4">
                <Button
                  onClick={handleCheckIn}
                  disabled={selectedCheckins.size === 0}
                  loading={recordAttendanceMutation.isPending}
                  size="lg"
                  className="w-full text-xl"
                >
                  Check In {selectedCheckins.size > 0 && `(${getTotalActivitiesCount(selectedCheckins)} ${getTotalActivitiesCount(selectedCheckins) === 1 ? 'activity' : 'activities'})`}
                </Button>
              </div>
            </>
          )}

          {opportunitiesQuery.isError && (
            <Card className="bg-red-50 border border-red-200">
              <p className="text-red-800 text-center">
                Failed to load check-in options. Please try again.
              </p>
            </Card>
          )}
        </div>
      )}

      {/* Step 4: Confirmation */}
      {step === 'confirmation' && recordAttendanceMutation.data && (
        <CheckinConfirmation
          attendances={recordAttendanceMutation.data.attendances}
          onDone={handleDone}
          onPrintLabels={
            recordAttendanceMutation.data.labels.length > 0
              ? () => {
                  // TODO: Implement label printing (P0 - see docs/plans/ux-improvements-plan.md)
                }
              : undefined
          }
        />
      )}
      </KioskLayout>

      {/* Idle Warning Modal */}
      <IdleWarningModal
        isOpen={isWarning}
        secondsRemaining={secondsRemaining}
        onStayActive={resetTimer}
      />
    </>
  );
}
