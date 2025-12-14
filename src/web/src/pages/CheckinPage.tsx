import { useState, useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  KioskLayout,
  PhoneSearch,
  FamilySearch,
  FamilyMemberList,
  CheckinConfirmation,
  IdleWarningModal,
  PrintStatus,
  QrScanner,
  OfflineQueueIndicator,
  PinEntry,
  SupervisorMode,
} from '@/components/checkin';
import type { OpportunitySelection } from '@/components/checkin';
import { Button, Card } from '@/components/ui';
import {
  useCheckinSearch,
  useCheckinOpportunities,
  useCheckinConfiguration,
} from '@/hooks/useCheckin';
import {
  useSupervisorAttendance,
  extractLocationIdKeys,
} from '@/hooks/useSupervisorAttendance';
import { useOfflineCheckin } from '@/hooks/useOfflineCheckin';
import { useIdleTimeout } from '@/hooks/useIdleTimeout';
import { useSupervisorMode } from '@/hooks/useSupervisorMode';
import type {
  CheckinFamilyDto,
  CheckinRequestItem,
  LabelDto,
  RecordAttendanceResponse,
} from '@/services/api/types';
import { createSelectionKey, getTotalActivitiesCount } from '@/utils/checkinHelpers';
import { printBridgeClient, type PrinterInfo } from '@/services/printing/PrintBridgeClient';
import { OfflineIndicator } from '@/components/pwa';
import { supervisorLogin, supervisorLogout, supervisorReprint, checkout } from '@/services/api/checkin';

type CheckinStep = 'search' | 'select-family' | 'select-members' | 'confirmation';
type SearchMode = 'phone' | 'name' | 'qr';

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
  const [qrScannedIdKey, setQrScannedIdKey] = useState<string | null>(null);
  const [selectedFamily, setSelectedFamily] = useState<CheckinFamilyDto | null>(null);
  const [selectedCheckins, setSelectedCheckins] = useState<
    Map<string, OpportunitySelection[]>
  >(new Map());
  const [checkinError, setCheckinError] = useState<string | null>(null);
  const [printerAvailable, setPrinterAvailable] = useState<PrinterInfo | null>(null);
  const [printStatus, setPrintStatus] = useState<'idle' | 'printing' | 'success' | 'error'>('idle');
  const [printError, setPrintError] = useState<string | null>(null);

  // Supervisor mode state
  const [showPinEntry, setShowPinEntry] = useState(false);
  const [pinError, setPinError] = useState<string | null>(null);
  const [isPinLoading, setIsPinLoading] = useState(false);
  const supervisorMode = useSupervisorMode();

  // Fetch check-in configuration for kiosk locations
  const configQuery = useCheckinConfiguration();

  // Extract location IdKeys from configuration for supervisor attendance
  const locationIdKeys = extractLocationIdKeys(configQuery.data?.areas);

  // Fetch current attendance for supervisor mode (only when active)
  const supervisorAttendanceQuery = useSupervisorAttendance(
    locationIdKeys,
    supervisorMode.isActive
  );

  // Store attendance response for confirmation page
  const recordAttendanceData = useRef<RecordAttendanceResponse | null>(null);

  // Track reset timeout for cleanup
  const resetTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Determine search type based on mode
  const getSearchType = (): 'Phone' | 'Name' | 'Auto' => {
    if (searchMode === 'phone') return 'Phone';
    if (searchMode === 'name') return 'Name';
    // QR mode uses 'Auto' which will interpret IdKey
    return 'Auto';
  };

  // Queries
  const searchQuery = useCheckinSearch(searchValue, getSearchType());

  const opportunitiesQuery = useCheckinOpportunities(selectedFamily?.idKey);

  const {
    recordCheckin,
    state: offlineState,
    syncQueue,
    isPending: isRecordingCheckin,
  } = useOfflineCheckin();

  // Handlers
  const handleSearch = (value: string) => {
    setSearchValue(value);
    // When query succeeds, move to family selection if multiple results
    // or directly to member selection if single result
  };

  const handleQrScan = (familyIdKey: string) => {
    // QR scan uses IdKey search - set mode to 'qr' and trigger search via effect
    // This prevents race condition where searchValue might be updated before searchMode
    setQrScannedIdKey(familyIdKey);
    setSearchMode('qr');
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

  // Effect: Cleanup reset timeout on unmount
  useEffect(() => {
    return () => {
      if (resetTimeoutRef.current) {
        clearTimeout(resetTimeoutRef.current);
      }
    };
  }, []);

  // Effect: Handle QR scan state sequencing to prevent race condition
  useEffect(() => {
    if (qrScannedIdKey && searchMode === 'qr') {
      setSearchValue(qrScannedIdKey);
      setQrScannedIdKey(null); // Clear after applying
    }
  }, [qrScannedIdKey, searchMode]);

  // Effect: Auto-advance when search returns single family
  useEffect(() => {
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
      const response = await recordCheckin({ checkins });
      setCheckinError(null);

      // If we got a response (online mode), show confirmation with labels
      if (response) {
        // Store response for confirmation page (create synthetic mutation data)
        recordAttendanceData.current = response;
        setStep('confirmation');
      } else {
        // Offline mode - check-in was queued
        // Show a different confirmation message
        setCheckinError(
          offlineState.mode === 'offline'
            ? 'You are offline. Check-ins have been queued and will sync when connection is restored.'
            : 'Check-in queued. Will sync shortly.'
        );
        // Reset after showing message (tracked for cleanup)
        if (resetTimeoutRef.current) {
          clearTimeout(resetTimeoutRef.current);
        }
        resetTimeoutRef.current = setTimeout(() => {
          handleReset();
        }, 3000);
      }
    } catch (error) {
      // Log error for debugging (important for production issue diagnosis)
      if (import.meta.env.DEV) {
        console.error('Check-in failed:', error);
      }
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

    const labelsToPrint = labels || recordAttendanceData.current?.labels;

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
    setQrScannedIdKey(null);
    setSelectedFamily(null);
    setSelectedCheckins(new Map());
    setCheckinError(null);
    setPrintStatus('idle');
    setPrintError(null);
    recordAttendanceData.current = null;
  };

  const handleDone = () => {
    handleReset();
  };

  // Supervisor mode handlers
  const handlePinSubmit = async (pin: string) => {
    setIsPinLoading(true);
    setPinError(null);
    try {
      const response = await supervisorLogin({ pin });
      supervisorMode.startSession({
        token: response.sessionToken,
        supervisor: response.supervisor,
        expiresAt: response.expiresAt,
      });
      setShowPinEntry(false);
    } catch (error) {
      setPinError('Invalid PIN. Please try again.');
    } finally {
      setIsPinLoading(false);
    }
  };

  const handleSupervisorReprint = async (attendanceIdKey: string) => {
    if (!supervisorMode.sessionToken) throw new Error('No session');
    supervisorMode.resetTimeout();
    return supervisorReprint(attendanceIdKey, supervisorMode.sessionToken);
  };

  const handleSupervisorCheckout = async (attendanceIdKey: string) => {
    supervisorMode.resetTimeout();
    await checkout(attendanceIdKey);
  };

  const handleSupervisorExit = async () => {
    if (supervisorMode.sessionToken) {
      try {
        await supervisorLogout(supervisorMode.sessionToken);
      } catch {
        // Ignore logout errors
      }
    }
    supervisorMode.endSession();
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
      <OfflineIndicator />
      <OfflineQueueIndicator state={offlineState} onSync={syncQueue} />
      <KioskLayout
        title={
          step === 'select-members' && selectedFamily
            ? selectedFamily.name
            : undefined
        }
        onReset={step !== 'search' ? handleReset : undefined}
        onSupervisorTrigger={() => setShowPinEntry(true)}
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
              onClick={() => setSearchMode('qr')}
              className={`px-8 py-4 rounded-lg font-semibold transition-colors min-h-[56px] ${
                searchMode === 'qr'
                  ? 'bg-blue-600 text-white'
                  : 'bg-white text-gray-700 border-2 border-gray-300'
              }`}
            >
              Scan QR Code
            </button>
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
          {searchMode === 'qr' ? (
            <QrScanner
              onScan={handleQrScan}
              onCancel={() => setSearchMode('phone')}
            />
          ) : searchMode === 'phone' ? (
            <PhoneSearch onSearch={handleSearch} loading={searchQuery.isFetching} />
          ) : (
            <FamilySearch onSearch={handleSearch} loading={searchQuery.isFetching} />
          )}

          {/* Error */}
          {searchQuery.isError && searchMode !== 'qr' && (
            <div className="max-w-2xl mx-auto mt-4">
              <Card className="bg-red-50 border border-red-200">
                <p className="text-red-800 text-center">
                  Search failed. Please try again.
                </p>
              </Card>
            </div>
          )}

          {/* No Results */}
          {searchQuery.data && searchQuery.data.length === 0 && searchMode !== 'qr' && (
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
                    loading={isRecordingCheckin}
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
      {step === 'confirmation' && recordAttendanceData.current && (
        <CheckinConfirmation
          attendances={recordAttendanceData.current.attendances}
          onDone={handleDone}
          onPrintLabels={
            recordAttendanceData.current.labels.length > 0 && printerAvailable
              ? () => handlePrintLabels()
              : undefined
          }
          printStatus={printStatus}
          printError={printError}
          printerAvailable={!!printerAvailable}
        />
      )}
      </KioskLayout>

      {/* PIN Entry Modal */}
      {showPinEntry && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <PinEntry
            onSubmit={handlePinSubmit}
            onCancel={() => {
              setShowPinEntry(false);
              setPinError(null);
            }}
            loading={isPinLoading}
            error={pinError}
          />
        </div>
      )}

      {/* Supervisor Mode Panel */}
      {supervisorMode.isActive && supervisorMode.supervisor && (
        <div className="fixed inset-0 bg-white z-40 overflow-auto">
          <div className="max-w-4xl mx-auto p-6">
            <SupervisorMode
              supervisor={supervisorMode.supervisor}
              currentAttendance={supervisorAttendanceQuery.data ?? []}
              onReprint={handleSupervisorReprint}
              onCheckout={handleSupervisorCheckout}
              onExit={handleSupervisorExit}
              printLabel={printerAvailable ? handlePrintLabels : undefined}
            />
          </div>
        </div>
      )}

      {/* Idle Warning Modal */}
      <IdleWarningModal
        isOpen={isWarning}
        secondsRemaining={secondsRemaining}
        onStayActive={resetTimer}
      />
    </>
  );
}
