import { Card } from '@/components/ui/Card';
import type {
  CheckinPersonDto,
  PersonOpportunitiesDto,
  CheckinOptionDto,
  CurrentAttendanceDto,
} from '@/services/api/types';
import { createSelectionKey } from '@/utils/checkinHelpers';

export interface OpportunitySelection {
  groupId: string;
  locationId: string;
  scheduleId: string;
  groupName: string;
  locationName: string;
  scheduleName: string;
  startTime: string;
}

export interface FamilyMemberListProps {
  opportunities: PersonOpportunitiesDto[];
  selectedCheckins: Map<string, OpportunitySelection[]>;
  onToggleCheckin: (
    personId: string,
    groupId: string,
    locationId: string,
    scheduleId: string,
    groupName: string,
    locationName: string,
    scheduleName: string,
    startTime: string
  ) => void;
}

/**
 * Display family members with checkboxes to check in
 */
export function FamilyMemberList({
  opportunities,
  selectedCheckins,
  onToggleCheckin,
}: FamilyMemberListProps) {
  return (
    <div className="space-y-6">
      {opportunities.map((opp) => (
        <PersonCard
          key={opp.person.idKey}
          person={opp.person}
          availableOptions={opp.availableOptions}
          currentAttendance={opp.currentAttendance}
          selectedOptions={selectedCheckins.get(opp.person.idKey) || []}
          onSelect={(groupId, locationId, scheduleId, groupName, locationName, scheduleName, startTime) =>
            onToggleCheckin(opp.person.idKey, groupId, locationId, scheduleId, groupName, locationName, scheduleName, startTime)
          }
        />
      ))}
    </div>
  );
}

interface PersonCardProps {
  person: CheckinPersonDto;
  availableOptions: CheckinOptionDto[];
  currentAttendance: CurrentAttendanceDto[];
  selectedOptions: OpportunitySelection[];
  onSelect: (
    groupId: string,
    locationId: string,
    scheduleId: string,
    groupName: string,
    locationName: string,
    scheduleName: string,
    startTime: string
  ) => void;
}

function PersonCard({
  person,
  availableOptions,
  currentAttendance,
  selectedOptions,
  onSelect,
}: PersonCardProps) {
  // If already checked in, show current attendance
  if (currentAttendance.length > 0) {
    return (
      <Card className="opacity-60">
        <div className="flex items-center gap-4">
          {person.photoUrl ? (
            <img
              src={person.photoUrl}
              alt={person.fullName}
              className="w-16 h-16 rounded-full object-cover"
            />
          ) : (
            <div className="w-16 h-16 rounded-full bg-gray-300 flex items-center justify-center text-2xl font-bold text-gray-600">
              {person.firstName[0]}
            </div>
          )}
          <div className="flex-1">
            <h3 className="text-xl font-semibold text-gray-900">
              {person.fullName}
            </h3>
            <p className="text-gray-600">
              {person.age && `Age ${person.age}`}
              {person.grade && ` • ${person.grade}`}
            </p>
            <div className="mt-2 inline-block bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm font-medium">
              Already Checked In - {currentAttendance[0].group}
            </div>
          </div>
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <div className="flex items-start gap-4">
        {person.photoUrl ? (
          <img
            src={person.photoUrl}
            alt={person.fullName}
            className="w-16 h-16 rounded-full object-cover"
          />
        ) : (
          <div className="w-16 h-16 rounded-full bg-gray-300 flex items-center justify-center text-2xl font-bold text-gray-600">
            {person.firstName[0]}
          </div>
        )}
        <div className="flex-1">
          <div className="flex items-center justify-between mb-1">
            <h3 className="text-xl font-semibold text-gray-900">
              {person.fullName}
            </h3>
            {selectedOptions.length > 0 && (
              <span className="bg-blue-600 text-white px-3 py-1 rounded-full text-sm font-medium">
                {selectedOptions.length} {selectedOptions.length === 1 ? 'activity' : 'activities'} selected
              </span>
            )}
          </div>
          <p className="text-gray-600 mb-3">
            {person.age && `Age ${person.age}`}
            {person.grade && ` • ${person.grade}`}
          </p>

          {/* Available Groups */}
          <div className="space-y-2">
            {availableOptions.map((option) =>
              option.locations.map((location) =>
                location.schedules
                  .filter((schedule) => schedule.isSelected)
                  .map((schedule) => {
                    const selectionKey = createSelectionKey(option.groupIdKey, location.locationIdKey, schedule.scheduleIdKey);
                    const isSelected = selectedOptions.some(
                      (sel) => createSelectionKey(sel.groupId, sel.locationId, sel.scheduleId) === selectionKey
                    );

                    return (
                      <button
                        key={`${option.groupIdKey}-${location.locationIdKey}-${schedule.scheduleIdKey}`}
                        onClick={() =>
                          onSelect(
                            option.groupIdKey,
                            location.locationIdKey,
                            schedule.scheduleIdKey,
                            option.groupName,
                            location.locationName,
                            schedule.scheduleName,
                            schedule.startTime
                          )
                        }
                        aria-label={`${isSelected ? 'Uncheck' : 'Check'} ${option.groupName} at ${location.locationName}, ${schedule.startTime}`}
                        className={`w-full text-left p-4 rounded-lg border-2 transition-all min-h-[64px] ${
                          isSelected
                            ? 'border-blue-600 bg-blue-50'
                            : 'border-gray-200 hover:border-blue-300'
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-3">
                            <div className={`flex-shrink-0 w-6 h-6 rounded border-2 flex items-center justify-center ${
                              isSelected
                                ? 'bg-blue-600 border-blue-600'
                                : 'border-gray-300 bg-white'
                            }`}>
                              {isSelected && (
                                <svg
                                  className="w-4 h-4 text-white"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={3}
                                    d="M5 13l4 4L19 7"
                                  />
                                </svg>
                              )}
                            </div>
                            <div>
                              <p className="font-semibold text-gray-900">
                                {option.groupName}
                              </p>
                              <p className="text-sm text-gray-600">
                                {location.locationName} • {schedule.startTime}
                              </p>
                            </div>
                          </div>
                        </div>
                      </button>
                    );
                  })
              )
            )}
          </div>
        </div>
      </div>
    </Card>
  );
}
