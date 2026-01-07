/**
 * TypeScript types for Koinon RMS API
 * Generated from docs/reference/api-contracts.md
 */

// Import and re-export types from other files for convenience
import { LabelType } from '@/types/labels';
export { LabelType };

// ============================================================================
// Common Types
// ============================================================================

export type IdKey = string;        // Base64-encoded integer ID (22 chars)
export type DateOnly = string;     // ISO 8601 date: "2024-01-15"
export type DateTime = string;     // ISO 8601 datetime: "2024-01-15T10:30:00Z"
export type Guid = string;         // UUID format

// ============================================================================
// Response Envelopes
// ============================================================================

export interface ApiResponse<T> {
  data: T;
  meta?: PaginationMeta;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedResult<T> {
  data: T[];
  meta: PaginationMeta;
}

/**
 * @deprecated Use ProblemDetails instead. Legacy format for backwards compatibility.
 */
export interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, string[]>;
    traceId?: string;
  };
}

/**
 * RFC 7807 Problem Details for HTTP APIs
 * Standard format for API error responses
 */
export interface ProblemDetails {
  type?: string;                        // URI reference identifying the problem type
  title?: string;                       // Short, human-readable summary
  status?: number;                      // HTTP status code
  detail?: string;                      // Human-readable explanation
  instance?: string;                    // URI reference to specific occurrence
  traceId?: string;                     // Correlation ID for debugging (extension)
  extensions?: Record<string, unknown>; // Additional problem-specific data (e.g., validation errors)
}

// ============================================================================
// Common Query Parameters
// ============================================================================

export interface PaginationParams {
  page?: number;        // Default: 1
  pageSize?: number;    // Default: 25, Max: 100
}

export interface SortParams {
  sortBy?: string;      // Field name
  sortDir?: 'asc' | 'desc';  // Default: 'asc'
}

// ============================================================================
// Authentication Types
// ============================================================================

export interface LoginRequest {
  email: string;
  password: string;
}

export interface TokenResponse {
  accessToken: string;      // JWT, expires in 15 minutes
  refreshToken: string;     // Opaque token, expires in 7 days
  expiresAt: DateTime;      // Access token expiration
  user: UserSummaryDto;
}

export interface UserSummaryDto {
  idKey: IdKey;
  firstName: string;
  lastName: string;
  email: string;
  photoUrl?: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;  // Backend rotates tokens
  expiresAt: DateTime;
}

export interface LogoutRequest {
  refreshToken: string;
}

// ============================================================================
// Person Types
// ============================================================================

export type Gender = 'Unknown' | 'Male' | 'Female';
export type EmailPreference = 'EmailAllowed' | 'NoMassEmails' | 'DoNotEmail';

export interface PersonSearchParams extends PaginationParams, SortParams {
  q?: string;                    // Full-text search query
  firstName?: string;            // Filter by first name (partial)
  lastName?: string;             // Filter by last name (partial)
  email?: string;                // Filter by email (partial)
  phone?: string;                // Filter by phone (partial, digits only)
  recordStatusId?: IdKey;        // Filter by record status
  connectionStatusId?: IdKey;    // Filter by connection status
  campusId?: IdKey;              // Filter by primary campus
  includeInactive?: boolean;     // Include inactive records (default: false)
}

export interface PersonSummaryDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  photoUrl?: string;
  age?: number;
  gender: Gender;
  connectionStatus?: DefinedValueDto;
  recordStatus?: DefinedValueDto;
  primaryCampus?: CampusSummaryDto;
}

export interface PersonDetailDto {
  idKey: IdKey;
  guid: Guid;

  // Names
  firstName: string;
  nickName?: string;
  middleName?: string;
  lastName: string;
  fullName: string;

  // Title/Suffix
  title?: DefinedValueDto;
  suffix?: DefinedValueDto;

  // Demographics
  birthDate?: DateOnly;
  age?: number;
  gender: Gender;
  maritalStatus?: DefinedValueDto;
  anniversaryDate?: DateOnly;

  // Contact
  email?: string;
  isEmailActive: boolean;
  emailPreference: EmailPreference;
  phoneNumbers: PhoneNumberDto[];

  // Status
  recordStatus?: DefinedValueDto;
  connectionStatus?: DefinedValueDto;
  isDeceased: boolean;

  // Photo
  photoUrl?: string;
  photoId?: IdKey;

  // Associations
  primaryFamily?: FamilySummaryDto;
  primaryCampus?: CampusSummaryDto;

  // Metadata
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

/**
 * PersonDto matches the backend C# PersonDto exactly.
 * Identical to PersonDetailDto - kept separate for backend/frontend type alignment.
 */
export interface PersonDto {
  idKey: IdKey;
  guid: Guid;

  // Names
  firstName: string;
  nickName?: string;
  middleName?: string;
  lastName: string;
  fullName: string;

  // Title/Suffix
  title?: DefinedValueDto;
  suffix?: DefinedValueDto;

  // Demographics
  birthDate?: DateOnly;
  age?: number;
  gender: Gender;
  maritalStatus?: DefinedValueDto;
  anniversaryDate?: DateOnly;

  // Contact
  email?: string;
  isEmailActive: boolean;
  emailPreference: EmailPreference;
  phoneNumbers: PhoneNumberDto[];

  // Status
  recordStatus?: DefinedValueDto;
  connectionStatus?: DefinedValueDto;
  isDeceased: boolean;

  // Photo
  photoUrl?: string;
  photoId?: IdKey;

  // Associations
  primaryFamily?: FamilySummaryDto;
  primaryCampus?: CampusSummaryDto;

  // Metadata
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface PhoneNumberDto {
  idKey: IdKey;
  number: string;
  numberFormatted: string;
  extension?: string;
  phoneType?: DefinedValueDto;
  isMessagingEnabled: boolean;
  isUnlisted: boolean;
}

export interface CreatePersonRequest {
  firstName: string;             // Required, max 50
  nickName?: string;             // Max 50
  middleName?: string;           // Max 50
  lastName: string;              // Required, max 50

  titleValueId?: IdKey;
  suffixValueId?: IdKey;

  email?: string;                // Valid email format
  gender?: Gender;
  birthDate?: DateOnly;
  maritalStatusValueId?: IdKey;
  anniversaryDate?: DateOnly;

  connectionStatusValueId?: IdKey;  // Default: Visitor
  recordStatusValueId?: IdKey;      // Default: Active

  phoneNumbers?: CreatePhoneNumberRequest[];

  // Optionally add to existing family
  familyId?: IdKey;
  familyRoleId?: IdKey;           // Adult or Child

  // Or create new family
  createFamily?: boolean;
  familyName?: string;            // Default: "{LastName} Family"
  campusId?: IdKey;
}

export interface CreatePhoneNumberRequest {
  number: string;                 // Required
  extension?: string;
  phoneTypeValueId?: IdKey;       // Default: Mobile
  isMessagingEnabled?: boolean;   // Default: true for Mobile
}

export interface UpdatePersonRequest {
  firstName?: string;
  nickName?: string;
  middleName?: string;
  lastName?: string;

  titleValueId?: IdKey | null;    // null to clear
  suffixValueId?: IdKey | null;

  email?: string | null;
  isEmailActive?: boolean;
  emailPreference?: EmailPreference;

  gender?: Gender;
  birthDate?: DateOnly | null;
  maritalStatusValueId?: IdKey | null;
  anniversaryDate?: DateOnly | null;

  connectionStatusValueId?: IdKey;
  recordStatusValueId?: IdKey;

  primaryCampusId?: IdKey | null;
}

// ============================================================================
// Family Types
// ============================================================================

export interface FamiliesSearchParams extends PaginationParams {
  q?: string;                    // Search by family name or member names
  campusId?: IdKey;
  includeInactive?: boolean;
}

export interface FamilySummaryDto {
  idKey: IdKey;
  name: string;
  campus?: CampusSummaryDto;
  memberCount: number;
  primaryAddress?: AddressDto;
}

export interface FamilyDetailDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  campus?: CampusSummaryDto;
  members: FamilyMemberDto[];
  addresses: FamilyAddressDto[];
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface FamilyMemberDto {
  idKey: IdKey;
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: string;
  isPersonPrimaryFamily: boolean;
  dateTimeAdded?: DateTime;
}

export interface FamilyAddressDto {
  idKey: IdKey;
  locationType?: DefinedValueDto;  // Home, Work, Previous
  address: AddressDto;
  isMailingAddress: boolean;
  isMappedAddress: boolean;
}

export interface AddressDto {
  idKey: IdKey;
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  formattedAddress: string;        // Single-line format
  formattedHtmlAddress: string;    // Multi-line HTML format
}

export interface CreateFamilyRequest {
  name: string;                   // Required
  description?: string;
  campusId?: IdKey;

  members?: CreateFamilyMemberRequest[];

  address?: CreateAddressRequest;
}

export interface CreateFamilyMemberRequest {
  personId?: IdKey;               // Existing person
  // OR create new person inline
  person?: CreatePersonRequest;
  roleId: IdKey;                  // Adult or Child role
}

export interface CreateAddressRequest {
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country?: string;               // Default: "US"
  locationTypeValueId?: IdKey;    // Default: Home
  isMailingAddress?: boolean;     // Default: true
  isMappedAddress?: boolean;      // Default: true
}

export interface UpdateFamilyRequest {
  name?: string;
  campusId?: IdKey | null;
}

export interface AddFamilyMemberRequest {
  personId?: IdKey;               // Existing person
  person?: CreatePersonRequest;   // Or create new
  roleId: IdKey;                  // Adult or Child
  setAsPrimaryFamily?: boolean;   // Default: true if person has no primary family
}

export interface UpdateFamilyAddressRequest {
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country?: string;
}

export interface RemoveFamilyMemberParams {
  removeFromAllGroups?: boolean;  // Also remove from family's groups
}

// ============================================================================
// Group Types
// ============================================================================

export type GroupMemberStatus = 'Inactive' | 'Active' | 'Pending';

export interface GroupsSearchParams extends PaginationParams {
  q?: string;
  groupTypeId?: IdKey;
  parentGroupId?: IdKey;
  campusId?: IdKey;
  includeInactive?: boolean;
}

export interface GroupSummaryDto {
  idKey: IdKey;
  name: string;
  description?: string;
  groupType: GroupTypeSummaryDto;
  groupTypeName: string;
  campus?: CampusSummaryDto;
  memberCount: number;
  isActive: boolean;
  isArchived: boolean;
}

export interface GroupDetailDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupType: GroupTypeSummaryDto;
  parentGroup?: GroupSummaryDto;
  campus?: CampusSummaryDto;
  capacity?: number;
  isActive: boolean;
  isArchived: boolean;
  schedule?: ScheduleDto;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface GroupMembersParams extends PaginationParams {
  status?: 'Active' | 'Inactive' | 'Pending' | 'All';
  roleId?: IdKey;
}

export interface GroupMemberDetailDto {
  idKey: IdKey;
  personIdKey: IdKey;
  firstName: string;
  lastName: string;
  fullName: string;
  gender: string;
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: GroupMemberStatus;
  dateAdded?: DateTime;
  note?: string;
  email?: string;
  phone?: string;
  photoUrl?: string;
  age?: number;
  dateTimeAdded?: DateTime;
  inactiveDateTime?: DateTime;
}

export interface GroupMembershipDto {
  group: GroupSummaryDto;
  role: GroupTypeRoleDto;
  status: GroupMemberStatus;
  dateAdded?: DateTime;
}

export interface AddGroupMemberRequest {
  personId: IdKey;
  roleId: IdKey;
  status?: GroupMemberStatus;  // Default: Active
  note?: string;
}

export interface PersonGroupsParams extends PaginationParams {
  groupTypeId?: IdKey;           // Filter by group type
  includeInactive?: boolean;     // Include inactive memberships
}

export interface PersonFamilyResponse {
  family: FamilyDetailDto;
  members: FamilyMemberDto[];
}

// ============================================================================
// Check-in Types
// ============================================================================

export type CheckinSearchType = 'PhoneNumber' | 'Name' | 'PhoneAndName';

export interface CheckinConfigParams {
  kioskId?: IdKey;               // Kiosk device ID
  campusId?: IdKey;              // Or campus ID
}

export interface CheckinConfigDto {
  areas: CheckinAreaDto[];
  currentSchedules: ScheduleDto[];
  searchType: CheckinSearchType;
  securityCodeLength: number;
  securityCodeAlphanumeric: boolean;
  autoSelectDaysBack: number;
  autoSelectFamily: boolean;
  preventDuplicateCheckin: boolean;
  allowCheckout: boolean;
}

export interface CheckinAreaDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupType: GroupTypeSummaryDto;
  locations: CheckinLocationDto[];
  schedule?: ScheduleDto;
  isActive: boolean;
  capacityStatus: CapacityStatus;
  minAgeMonths?: number;
  maxAgeMonths?: number;
  minGrade?: number;
  maxGrade?: number;
}

export interface CheckinGroupDto {
  idKey: IdKey;
  name: string;
  locations: CheckinLocationDto[];
}

export interface CheckinLocationDto {
  idKey: IdKey;
  name: string;
  fullPath: string;
  currentCount: number;
  softThreshold?: number;
  firmThreshold?: number;
  softCapacity?: number;
  hardCapacity?: number;
  capacityStatus: CapacityStatus;
  isActive: boolean;
  printerDeviceIdKey?: string;
  percentageFull: number;
  overflowLocationIdKey?: string;
  overflowLocationName?: string;
  autoAssignOverflow: boolean;
  isOpen: boolean;
  schedules: ScheduleDto[];
}

export interface ScheduleDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  startTime: string;             // "09:00"
  isActive: boolean;
  isCheckinActive: boolean;
  checkInWindowOpen: boolean;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
  checkinStartTime?: DateTime;
  checkinEndTime?: DateTime;
  isPublic: boolean;
  order: number;
  effectiveStartDate?: string;
  effectiveEndDate?: string;
  iCalendarContent?: string;
  autoInactivateWhenComplete: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface CheckinSearchRequest {
  searchValue: string;           // Phone number or name
  searchType?: 'Phone' | 'Name' | 'Auto';  // Default: Auto
}

export interface CheckinFamilyDto {
  idKey: IdKey;
  name: string;
  members: CheckinPersonDto[];
  lastCheckIn?: DateTime;
}

export interface CheckinPersonDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  age?: number;
  grade?: string;
  photoUrl?: string;
  lastCheckIn?: DateTime;
  allergies?: string;
  hasCriticalAllergies: boolean;
  specialNeeds?: string;
}

export interface CheckinOpportunitiesParams {
  scheduleId?: IdKey;            // Filter to specific schedule
}

export interface CheckinOpportunitiesResponse {
  family: CheckinFamilyDto;
  opportunities: PersonOpportunitiesDto[];
}

export interface PersonOpportunitiesDto {
  person: CheckinPersonDto;
  currentAttendance: CurrentAttendanceDto[];  // Already checked in
  availableOptions: CheckinOptionDto[];
}

export interface CurrentAttendanceDto {
  attendanceIdKey: IdKey;
  group: string;
  location: string;
  schedule: string;
  securityCode: string;
  checkInTime: DateTime;
  canCheckOut: boolean;
}

export interface CheckinOptionDto {
  groupIdKey: IdKey;
  groupName: string;
  locations: CheckinLocationOptionDto[];
}

export interface CheckinLocationOptionDto {
  locationIdKey: IdKey;
  locationName: string;
  schedules: CheckinScheduleOptionDto[];
  currentCount: number;
  softThreshold?: number;
  firmThreshold?: number;
}

export interface CheckinScheduleOptionDto {
  scheduleIdKey: IdKey;
  scheduleName: string;
  startTime: string;
  isSelected: boolean;           // Auto-selected based on config
}

// =============================================================================
// Kiosk Check-in Types (for /checkin/attendance endpoint)
// =============================================================================

/**
 * Request to check in a single person.
 * Matches backend CheckinRequestDto.
 */
export interface CheckinRequest {
  personIdKey: IdKey;
  locationIdKey: IdKey;
  scheduleIdKey?: IdKey;
  occurrenceDate?: DateOnly;
  deviceIdKey?: IdKey;
  generateSecurityCode?: boolean;
  note?: string;
}

/**
 * Request to check in multiple people at once (family batch check-in).
 * Matches backend BatchCheckinRequestDto.
 */
export interface BatchCheckinRequest {
  checkIns: CheckinRequest[];
  deviceIdKey?: IdKey;
}

/**
 * Result of a single check-in operation.
 * Matches backend CheckinResultDto.
 */
export interface CheckinResultDto {
  success: boolean;
  errorMessage?: string;
  attendanceIdKey?: IdKey;
  securityCode?: string;
  checkInTime?: DateTime;
  person?: CheckinPersonSummaryDto;
  location?: CheckinLocationSummaryDto;
}

/**
 * Minimal person summary for check-in operations.
 */
export interface CheckinPersonSummaryDto {
  idKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
  nickName?: string;
  age?: number;
  photoUrl?: string;
}

/**
 * Minimal location summary for check-in operations.
 */
export interface CheckinLocationSummaryDto {
  idKey: IdKey;
  name: string;
  fullPath: string;
}

/**
 * Result of a batch check-in operation.
 * Matches backend BatchCheckinResultDto.
 */
export interface BatchCheckinResultDto {
  results: CheckinResultDto[];
  successCount: number;
  failureCount: number;
  allSucceeded: boolean;
}

/**
 * Extended check-in request item for UI state management.
 * Includes group info for display purposes (not sent to backend).
 */
export interface CheckinRequestItem {
  personIdKey: IdKey;
  groupIdKey: IdKey;         // For UI display only
  locationIdKey: IdKey;
  scheduleIdKey: IdKey;
}

// =============================================================================
// Group Attendance Types (for /mygroups/attendance endpoint)
// =============================================================================

/**
 * Request to record attendance for a group meeting.
 * Matches backend RecordAttendanceRequest.
 */
export interface RecordAttendanceRequest {
  occurrenceDate: DateOnly;
  attendedPersonIds: string[];
  notes?: string;
}

/**
 * Response from kiosk check-in. Contains attendance results and labels.
 */
export interface RecordAttendanceResponse {
  attendances: AttendanceResultDto[];
  labels: LabelDto[];
}

export interface AttendanceResultDto {
  attendanceIdKey: IdKey;
  personIdKey: IdKey;
  personName: string;
  groupName: string;
  locationName: string;
  scheduleName: string;
  securityCode: string;
  checkInTime: DateTime;
  isFirstTime: boolean;
}

export interface LabelDto {
  type: LabelType;
  content: string;
  format: string;
  fields: Record<string, string>;
}

export interface CheckoutRequest {
  attendanceIdKey: IdKey;
}

export interface CheckoutResponse {
  attendanceIdKey: IdKey;
  checkOutTime: DateTime;
}

export interface LabelParams {
  labelType?: 'Child' | 'Parent' | 'NameTag' | 'All';
  format?: 'ZPL' | 'PDF';        // Default: ZPL
}

// ============================================================================
// Supervisor Mode Types
// ============================================================================

export interface SupervisorLoginRequest {
  pin: string;                   // 4-6 digit PIN
}

export interface SupervisorLoginResponse {
  sessionToken: string;          // Time-limited session token
  expiresAt: DateTime;           // Session expiration
  supervisor: SupervisorInfoDto;
}

export interface SupervisorInfoDto {
  idKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
}

// ============================================================================
// Reference Data Types
// ============================================================================

export interface DefinedTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  values: DefinedValueDto[];
}

export interface DefinedValueDto {
  idKey: IdKey;
  guid: Guid;
  value: string;
  description?: string;
  isActive: boolean;
  order: number;
}

export interface CampusDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  shortCode?: string;
  isActive: boolean;
  order: number;
  url?: string;
  phoneNumber?: string;
  serviceTimes?: string;
  timeZoneId?: string;
  createdDateTime?: DateTime;
  modifiedDateTime?: DateTime;
}

export interface CampusSummaryDto {
  idKey: IdKey;
  name: string;
  shortCode?: string;
}

export interface CampusesParams {
  includeInactive?: boolean;
}

// Location types moved to src/web/src/types/location.ts
// Import from there to use LocationDto and LocationSummaryDto

export interface GroupTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupTerm: string;
  groupMemberTerm: string;
  iconCssClass?: string;
  color?: string;
  takesAttendance: boolean;
  allowSelfRegistration: boolean;
  requiresMemberApproval: boolean;
  defaultIsPublic: boolean;
  defaultGroupCapacity?: number;
  isSystem: boolean;
  isArchived: boolean;
  order: number;
  groupCount: number;
  roles: GroupTypeRoleDto[];
}

export interface GroupTypeSummaryDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  iconCssClass?: string;
  isFamilyGroupType: boolean;
  allowMultipleLocations: boolean;
  roles: GroupTypeRoleDto[];
}

export interface GroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
  order: number;
}

// ============================================================================
// Group Type Admin Types
// ============================================================================

export interface GroupTypeAdminDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  iconCssClass?: string;
  color?: string;
  groupTerm: string;
  groupMemberTerm: string;
  takesAttendance: boolean;
  allowSelfRegistration: boolean;
  requiresMemberApproval: boolean;
  defaultIsPublic: boolean;
  defaultGroupCapacity?: number;
  isSystem: boolean;
  isArchived: boolean;
  order: number;
  groupCount: number;
}

export interface GroupTypeDetailAdminDto extends GroupTypeAdminDto {
  showInGroupList: boolean;
  showInNavigation: boolean;
  attendanceCountsAsWeekendService: boolean;
  sendAttendanceReminder: boolean;
  allowMultipleLocations: boolean;
  enableSpecificGroupRequirements: boolean;
  allowGroupSync: boolean;
  allowSpecificGroupMemberAttributes: boolean;
  showConnectionStatus: boolean;
  ignorePersonInactivated: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface CreateGroupTypeRequest {
  name: string;
  description?: string;
  iconCssClass?: string;
  color?: string;
  groupTerm?: string;
  groupMemberTerm?: string;
  takesAttendance?: boolean;
  allowSelfRegistration?: boolean;
  requiresMemberApproval?: boolean;
  defaultIsPublic?: boolean;
  defaultGroupCapacity?: number;
  showInGroupList?: boolean;
  showInNavigation?: boolean;
  order?: number;
}

export interface UpdateGroupTypeRequest {
  name?: string;
  description?: string;
  iconCssClass?: string;
  color?: string;
  groupTerm?: string;
  groupMemberTerm?: string;
  takesAttendance?: boolean;
  allowSelfRegistration?: boolean;
  requiresMemberApproval?: boolean;
  defaultIsPublic?: boolean;
  defaultGroupCapacity?: number;
  showInGroupList?: boolean;
  showInNavigation?: boolean;
  order?: number;
}

export interface GroupTypeGroupDto {
  idKey: IdKey;
  name: string;
  isActive: boolean;
  isArchived: boolean;
  memberCount: number;
}

// ============================================================================
// Group Mutation Types
// ============================================================================

export interface CreateGroupRequest {
  name: string;
  description?: string;
  groupTypeId: IdKey;
  parentGroupId?: IdKey;
  campusId?: IdKey;
  isActive?: boolean;
  isPublic?: boolean;
  allowGuests?: boolean;
  groupCapacity?: number;
  order?: number;
}

export interface UpdateGroupRequest {
  name?: string;
  description?: string;
  campusId?: IdKey;
  isActive?: boolean;
  isPublic?: boolean;
  allowGuests?: boolean;
  groupCapacity?: number;
  order?: number;
}

// ============================================================================
// Schedule Types
// ============================================================================

export interface ScheduleSearchParams extends PaginationParams {
  query?: string;
  dayOfWeek?: number;  // 0=Sunday, 6=Saturday
  includeInactive?: boolean;
}

export interface ScheduleSummaryDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  isActive: boolean;
  order: number;
}

export interface ScheduleDetailDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
  isActive: boolean;
  isCheckinActive: boolean;
  checkinStartTime?: DateTime;
  checkinEndTime?: DateTime;
  isPublic: boolean;
  order: number;
  effectiveStartDate?: DateOnly;
  effectiveEndDate?: DateOnly;
  iCalendarContent?: string;
  autoInactivateWhenComplete: boolean;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface ScheduleOccurrenceDto {
  occurrenceDateTime: DateTime;
  dayOfWeekName: string;
  formattedTime: string;
  checkInWindowStart?: DateTime;
  checkInWindowEnd?: DateTime;
  isCheckInWindowOpen: boolean;
}

export interface CreateScheduleRequest {
  name: string;
  description?: string;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
  isActive?: boolean;
  isPublic?: boolean;
  order?: number;
  effectiveStartDate?: DateOnly;
  effectiveEndDate?: DateOnly;
  iCalendarContent?: string;
  autoInactivateWhenComplete?: boolean;
}

export interface UpdateScheduleRequest {
  name?: string;
  description?: string;
  weeklyDayOfWeek?: number;
  weeklyTimeOfDay?: string;
  checkInStartOffsetMinutes?: number;
  checkInEndOffsetMinutes?: number;
  isActive?: boolean;
  isPublic?: boolean;
  order?: number;
  effectiveStartDate?: DateOnly | null;
  effectiveEndDate?: DateOnly | null;
  iCalendarContent?: string;
  autoInactivateWhenComplete?: boolean;
}

// ============================================================================
// Group Schedule Types
// ============================================================================

export interface GroupScheduleDto {
  idKey: IdKey;
  guid: Guid;
  schedule: ScheduleSummaryDto;
  order: number;
}

export interface AddGroupScheduleRequest {
  scheduleIdKey: IdKey;
  order?: number;
}

// ============================================================================
// Room Roster Types
// ============================================================================

export interface RosterChildDto {
  attendanceIdKey: IdKey;
  personIdKey: IdKey;
  fullName: string;
  firstName: string;
  lastName: string;
  nickName?: string;
  photoUrl?: string;
  age?: number;
  grade?: string;
  allergies?: string;
  hasCriticalAllergies: boolean;
  specialNeeds?: string;
  securityCode?: string;
  checkInTime: DateTime;
  parentName?: string;
  parentMobilePhone?: string;
  isFirstTime: boolean;
}

export interface RoomRosterDto {
  locationIdKey: IdKey;
  locationName: string;
  children: RosterChildDto[];
  totalCount: number;
  capacity?: number;
  generatedAt: DateTime;
  isAtCapacity: boolean;
  isNearCapacity: boolean;
}

// ============================================================================
// Room Capacity Types
// ============================================================================

export enum CapacityStatus {
  Available = 0,
  Warning = 1,
  Full = 2,
}

// ============================================================================
// My Groups Types (Group Leader Dashboard)
// ============================================================================

export interface MyGroupDto {
  idKey: IdKey;
  guid: string;
  name: string;
  description?: string;
  groupTypeName: string;
  isActive: boolean;
  memberCount: number;
  groupCapacity?: number;
  lastMeetingDate?: DateTime;
  campus?: CampusSummaryDto;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface MyGroupMemberDetailDto {
  idKey: IdKey;
  personIdKey: IdKey;
  firstName: string;
  lastName: string;
  fullName: string;
  email?: string;
  phone?: string;
  photoUrl?: string;
  age?: number;
  gender: string;
  role: GroupTypeRoleDto;
  status: string;
  dateTimeAdded?: DateTime;
  inactiveDateTime?: DateTime;
  note?: string;
}

export interface UpdateGroupMemberRequest {
  roleId?: IdKey;
  status?: string;
  note?: string;
}

export interface RecordGroupAttendanceRequest {
  occurrenceDate: DateOnly;
  attendedPersonIds: IdKey[];
  notes?: string;
}

// ============================================================================
// Group Membership Request Types
// ============================================================================

export type MembershipRequestStatus = 'Pending' | 'Approved' | 'Denied';

export interface GroupMemberRequestDto {
  idKey: IdKey;
  requester: PersonSummaryDto;
  group: GroupSummaryDto;
  status: MembershipRequestStatus;
  requestNote?: string;
  responseNote?: string;
  processedByPerson?: PersonSummaryDto;
  processedDateTime?: DateTime;
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

export interface SubmitMembershipRequestRequest {
  note?: string;
}

export interface ProcessMembershipRequestRequest {
  status: 'Approved' | 'Denied';
  note?: string;
}

// ============================================================================
// Audit Log Types
// ============================================================================

export enum AuditAction {
  Create = 'Create',
  Update = 'Update',
  Delete = 'Delete',
  View = 'View',
  Export = 'Export',
  Login = 'Login',
  Logout = 'Logout',
  Search = 'Search',
  Other = 'Other',
}

export enum ExportFormat {
  Csv = 'Csv',
  Json = 'Json',
  Excel = 'Excel',
}

export interface AuditLogDto {
  idKey: IdKey;
  actionType: AuditAction;
  entityType: string;
  entityIdKey: IdKey;
  personIdKey?: IdKey;
  personName?: string;
  timestamp: DateTime;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  changedProperties?: string[];
  additionalInfo?: string;
}

export interface AuditLogSearchParams extends PaginationParams {
  startDate?: DateTime;
  endDate?: DateTime;
  entityType?: string;
  actionType?: AuditAction;
  personIdKey?: IdKey;
  entityIdKey?: IdKey;
}

export interface AuditLogExportParams {
  startDate?: DateTime;
  endDate?: DateTime;
  entityType?: string;
  actionType?: AuditAction;
  personIdKey?: IdKey;
  format?: ExportFormat;
}

// ============================================================================
// Data Export Types
// ============================================================================

export type DataExportType = 'People' | 'Families' | 'Groups' | 'Contributions' | 'Attendance';
export type DataExportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export type DataExportFormat = 'CSV' | 'Excel';

export interface DataExportJobDto {
  idKey: IdKey;
  exportType: DataExportType;
  outputFormat: DataExportFormat;
  status: DataExportStatus;
  recordCount?: number;
  createdDateTime: DateTime;
  completedDateTime?: DateTime;
  errorMessage?: string;
  fileName?: string;
}

export interface CreateDataExportRequest {
  exportType: DataExportType;
  outputFormat: DataExportFormat;
  selectedFields: string[];
}

export interface ExportFieldDto {
  fieldName: string;
  displayName: string;
  description?: string;
  dataType: string;
  isRequired: boolean;
  isDefaultField: boolean;
}
