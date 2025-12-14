/**
 * Centralized localStorage keys used across the application.
 * Use these constants instead of hardcoded strings to improve maintainability.
 */

export const STORAGE_KEYS = {
  /**
   * Stores the selected location (room) IdKey for the Room Roster page.
   * Used by LocationPicker component and RosterPage.
   */
  SELECTED_LOCATION_ID_KEY: 'selectedLocationIdKey',
} as const;
