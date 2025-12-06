import type { OpportunitySelection } from '@/components/checkin';

/**
 * Creates a unique key for identifying a specific opportunity selection.
 * Used for consistent selection matching across check-in components.
 *
 * @param groupId - The group ID key
 * @param locationId - The location ID key
 * @param scheduleId - The schedule ID key
 * @returns A unique string key in format "groupId|locationId|scheduleId"
 */
export const createSelectionKey = (
  groupId: string,
  locationId: string,
  scheduleId: string
): string => `${groupId}|${locationId}|${scheduleId}`;

/**
 * Calculates the total count of selected activities across all people.
 * Centralizes logic used in both UI display and check-in submission.
 *
 * @param selectedCheckins - Map of person ID keys to their selected opportunities
 * @returns Total number of selected activities across all people
 */
export const getTotalActivitiesCount = (
  selectedCheckins: Map<string, OpportunitySelection[]>
): number => {
  let count = 0;
  selectedCheckins.forEach((selections) => {
    if (Array.isArray(selections)) {
      count += selections.length;
    }
  });
  return count;
};
