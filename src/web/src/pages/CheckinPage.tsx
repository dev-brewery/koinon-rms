import React, { useState, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  KioskLayout,
  PhoneSearch,
  FamilySearch,
  FamilyMemberList,
  CheckinConfirmation,
  IdleWarningModal,
  PrintStatus,
} from '@/components/checkin';
import type { OpportunitySelection } from '@/components/checkin';
import { Button, Card } from '@/components/ui';
import {
  useCheckinSearch,
  useCheckinOpportunities,
  useRecordAttendance,
} from '@/hooks/useCheckin';
import { useIdleTimeout } from '@/hooks/useIdleTimeout';
import type { CheckinFamilyDto, CheckinRequestItem, LabelDto } from '@/services/api/types';
import { createSelectionKey, getTotalActivitiesCount } from '@/utils/checkinHelpers';
import { printBridgeClient, type PrinterInfo } from '@/services/printing/PrintBridgeClient';

type CheckinStep = 'search' | 'select-family' | 'select-members' | 'confirmation';
type SearchMode = 'phone' | 'name';

// Idle timeout configuration
const IDLE_CONFIG = {
  timeout: 60 * 1000, // 60 seconds total
  warningTime: 50 * 1000, // Warning at 50 seconds (10s countdown)
};

export function CheckinPage() {
  // Query client for cache management
  const queryClient = useQueryClient();

  // State
  const [step, setStep] = useState<CheckinStep>('search');
  const [searchMode, setSearchMode] = useState<SearchMode>('phone');
  const [searchValue, setSearchValue] = useState<string>('');
  const [selectedFamily, setSelectedFamily] = useState<CheckinFamilyDto | null>(null);
  const [selectedCheckins, setSelectedCheckins] = useState<
    Map<string, OpportunitySelection[]>
  >(new Map());
  const [checkinError, setCheckinError] = useState<string | null>(null);
  const [printerAvailable, setPrinterAvailable] = useState<PrinterInfo | null>(null);
  const [printStatus, setPrintStatus] = useState<'idle' | 'printing' | 'success' | 'error'>('idle');
  const [printError, setPrintError] = useState<string | null>(null);

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

  // Effect: Check printer availability on mount
  useEffect(() => {
    const checkPrinter = async () => {
      const result = await printBridgeClient.getDefaultZebraPrinter();
      if (result.success && result.data) {
        setPrinterAvailable(result.data);
      }
    };
    checkPrinter();
  }, []);

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
    // Clear any previous errors
    setCheckinError(null);
    setPrintError(null);
    setPrintStatus('idle');

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
      setCheckinError(null);
      setStep('confirmation');
    } catch (error) {
      // Log error details for debugging
      console.error('Check-in failed:', error);

      // Show user-friendly error message
      setCheckinError(
        'Check-in failed. Please try again or contact the welcome desk for assistance.'
      );
    }
  };

  const handlePrintLabels = async (labels?: LabelDto[]) => {
    if (!printerAvailable) {
      setPrintError('No printer available');
      setPrintStatus('error');
      return;
    }

    const labelsToPrint = labels || recordAttendanceMutation.data?.labels;

    if (!labelsToPrint || labelsToPrint.length === 0) {
      setPrintError('No labels to print');
      setPrintStatus('error');
      return;
    }

    setPrintStatus('printing');
    setPrintError(null);

    try {
      // Extract ZPL content from labels
      const zplContents = labelsToPrint.map(label => label.printData);

      // Send to print bridge
      const result = await printBridgeClient.printBatch(printerAvailable.name, zplContents);

      if (result.success) {
        setPrintStatus('success');
      } else {
        setPrintStatus('error');
        setPrintError(result.error);
      }
    } catch (error) {
      console.error('Print failed:', error);
      setPrintStatus('error');
      setPrintError(error instanceof Error ? error.message : 'Print failed');
    }
  };

  const handleReset = () => {
    // Clear TanStack Query cache to prevent privacy leak
    queryClient.removeQueries({ queryKey: ['checkin-search'] });
    queryClient.removeQueries({ queryKey: ['checkin-opportunities'] });
    queryClient.removeQueries({ queryKey: ['checkin'] });

    setStep('search');
    setSearchValue('');
    setSelectedFamily(null);
    setSelectedCheckins(new Map());
    setCheckinError(null);
    setPrintStatus('idle');
    setPrintError(null);
  };

  const handleDone = () => {
    handleReset();
  };

  // Idle timeout - always active for privacy protection
  const { isWarning, secondsRemaining, resetTimer } = useIdleTimeout({
    timeout: IDLE_CONFIG.timeout,
    warningTime: IDLE_CONFIG.warningTime,
    onTimeout: handleReset,
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
          {/* Printer Status */}
          <div className="max-w-2xl mx-auto">
            <PrintStatus
              onPrinterAvailable={(printer) => setPrinterAvailable(printer)}
              onPrinterUnavailable={() => setPrinterAvailable(null)}
            />
          </div>

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
      {step === 'select-members' && opportunitiesQuery.data && (() => {
        // Calculate total activities count once for performance
        const totalActivities = getTotalActivitiesCount(selectedCheckins);

        return (
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

                {/* Check-in Error */}
                {checkinError && (
                  <div className="mt-4">
                    <Card className="bg-red-50 border border-red-200">
                      <p className="text-red-800 text-center font-medium">
                        {checkinError}
                      </p>
                    </Card>
                  </div>
                )}

                {/* Check-in Button */}
                <div className="mt-8 sticky bottom-0 bg-gradient-to-t from-blue-100 via-blue-100 to-transparent pt-6 pb-4">
                  <Button
                    onClick={handleCheckIn}
                    disabled={selectedCheckins.size === 0}
                    loading={recordAttendanceMutation.isPending}
                    size="lg"
                    className="w-full text-xl"
                  >
                    Check In {selectedCheckins.size > 0 && `(${totalActivities} ${totalActivities === 1 ? 'activity' : 'activities'})`}
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
        );
      })()}

      {/* Step 4: Confirmation */}
      {step === 'confirmation' && recordAttendanceMutation.data && (
        <CheckinConfirmation
          attendances={recordAttendanceMutation.data.attendances}
          onDone={handleDone}
          onPrintLabels={
            recordAttendanceMutation.data.labels.length > 0 && printerAvailable
              ? () => handlePrintLabels()
              : undefined
          }
          printStatus={printStatus}
          printError={printError}
          printerAvailable={!!printerAvailable}
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
