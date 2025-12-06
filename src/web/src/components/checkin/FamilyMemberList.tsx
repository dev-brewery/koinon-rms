import { Card } from '@/components/ui/Card';
import type {
  CheckinPersonDto,
  PersonOpportunitiesDto,
  CheckinOptionDto,
} from '@/services/api/types';

export interface FamilyMemberListProps {
  opportunities: PersonOpportunitiesDto[];
  selectedCheckins: Map<string, { groupId: string; locationId: string; scheduleId: string }>;
  onToggleCheckin: (
    personId: string,
    groupId: string,
    locationId: string,
    scheduleId: string
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
          selectedOption={selectedCheckins.get(opp.person.idKey)}
          onSelect={(groupId, locationId, scheduleId) =>
            onToggleCheckin(opp.person.idKey, groupId, locationId, scheduleId)
          }
        />
      ))}
    </div>
  );
}

interface PersonCardProps {
  person: CheckinPersonDto;
  availableOptions: CheckinOptionDto[];
  currentAttendance: any[];
  selectedOption?: { groupId: string; locationId: string; scheduleId: string };
  onSelect: (groupId: string, locationId: string, scheduleId: string) => void;
}

function PersonCard({
  person,
  availableOptions,
  currentAttendance,
  selectedOption,
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
          <h3 className="text-xl font-semibold text-gray-900 mb-1">
            {person.fullName}
          </h3>
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
                    const isSelected =
                      selectedOption?.groupId === option.groupIdKey &&
                      selectedOption?.locationId === location.locationIdKey &&
                      selectedOption?.scheduleId === schedule.scheduleIdKey;

                    return (
                      <button
                        key={`${option.groupIdKey}-${location.locationIdKey}-${schedule.scheduleIdKey}`}
                        onClick={() =>
                          onSelect(
                            option.groupIdKey,
                            location.locationIdKey,
                            schedule.scheduleIdKey
                          )
                        }
                        className={`w-full text-left p-4 rounded-lg border-2 transition-all min-h-[64px] ${
                          isSelected
                            ? 'border-blue-600 bg-blue-50'
                            : 'border-gray-200 hover:border-blue-300'
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="font-semibold text-gray-900">
                              {option.groupName}
                            </p>
                            <p className="text-sm text-gray-600">
                              {location.locationName} • {schedule.startTime}
                            </p>
                          </div>
                          <div className="flex items-center">
                            {isSelected && (
                              <svg
                                className="w-6 h-6 text-blue-600"
                                fill="currentColor"
                                viewBox="0 0 20 20"
                              >
                                <path
                                  fillRule="evenodd"
                                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                                  clipRule="evenodd"
                                />
                              </svg>
                            )}
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
