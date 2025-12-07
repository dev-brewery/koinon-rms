/**
 * TypeScript types for Koinon RMS API
 * Generated from docs/reference/api-contracts.md
 */

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

export interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, string[]>;
    traceId?: string;
  };
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
  username: string;
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
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  isPersonPrimaryFamily: boolean;
}

export interface FamilyAddressDto {
  idKey: IdKey;
  locationType?: DefinedValueDto;  // Home, Work, Previous
  address: AddressDto;
  isMailingAddress: boolean;
  isMappedAddress: boolean;
}

export interface AddressDto {
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
  campus?: CampusSummaryDto;
  memberCount: number;
  isActive: boolean;
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
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: GroupMemberStatus;
  dateAdded?: DateTime;
  note?: string;
}

export interface GroupMembershipDto {
  group: GroupSummaryDto;
  role: GroupTypeRoleDto;
  status: GroupMemberStatus;
  dateAdded?: DateTime;
}

export interface AddGroupMemberRequest {
  personIdKey: IdKey;
  roleIdKey: IdKey;
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
  name: string;
  groups: CheckinGroupDto[];
}

export interface CheckinGroupDto {
  idKey: IdKey;
  name: string;
  locations: CheckinLocationDto[];
}

export interface CheckinLocationDto {
  idKey: IdKey;
  name: string;
  currentCount: number;
  softThreshold?: number;
  firmThreshold?: number;
  isOpen: boolean;
  schedules: ScheduleDto[];
}

export interface ScheduleDto {
  idKey: IdKey;
  name: string;
  startTime: string;             // "09:00"
  isActive: boolean;
  checkInWindowOpen: boolean;
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

export interface RecordAttendanceRequest {
  checkins: CheckinRequestItem[];
}

export interface CheckinRequestItem {
  personIdKey: IdKey;
  groupIdKey: IdKey;
  locationIdKey: IdKey;
  scheduleIdKey: IdKey;
}

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
  attendanceIdKey: IdKey;
  labelType: 'Child' | 'Parent' | 'NameTag';
  printData: string;             // ZPL or other format
  printerAddress?: string;       // IP or device name
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
  shortCode?: string;
  isActive: boolean;
  url?: string;
  phoneNumber?: string;
  serviceTimes?: string;
  timeZoneId?: string;
}

export interface CampusSummaryDto {
  idKey: IdKey;
  name: string;
  shortCode?: string;
}

export interface CampusesParams {
  includeInactive?: boolean;
}

export interface GroupTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupTerm: string;
  groupMemberTerm: string;
  iconCssClass?: string;
  roles: GroupTypeRoleDto[];
}

export interface GroupTypeSummaryDto {
  idKey: IdKey;
  name: string;
  iconCssClass?: string;
}

export interface GroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
  order: number;
}
