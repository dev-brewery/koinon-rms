# Koinon RMS API Contracts

This document defines the REST API contracts for the Koinon RMS project. These contracts serve as the interface between the React frontend and ASP.NET Core backend.

---

## API Design Principles

1. **RESTful Resources**: URLs represent resources, HTTP methods represent actions
2. **JSON Payloads**: All requests/responses use `application/json`
3. **IdKey in URLs**: Use Base64-encoded IdKey, not integer IDs (prevents enumeration)
4. **Consistent Response Envelopes**: Wrap data in standard response objects
5. **HTTP Status Codes**: Use appropriate codes (200, 201, 204, 400, 401, 403, 404, 422, 500)
6. **Pagination**: Use `page` and `pageSize` query parameters
7. **Versioning**: URL prefix `/api/v1/`

---

## Common Types

### Standard Response Wrapper

```typescript
// Success response
interface ApiResponse<T> {
  data: T;
  meta?: {
    page?: number;
    pageSize?: number;
    totalCount?: number;
    totalPages?: number;
  };
}

// Error response
interface ApiError {
  error: {
    code: string;           // Machine-readable code
    message: string;        // Human-readable message
    details?: {             // Field-level validation errors
      [field: string]: string[];
    };
    traceId?: string;       // For support/debugging
  };
}
```

### Common Query Parameters

```typescript
interface PaginationParams {
  page?: number;        // Default: 1
  pageSize?: number;    // Default: 25, Max: 100
}

interface SortParams {
  sortBy?: string;      // Field name
  sortDir?: 'asc' | 'desc';  // Default: 'asc'
}
```

### Common Field Types

```typescript
type IdKey = string;        // Base64-encoded integer ID (22 chars)
type DateOnly = string;     // ISO 8601 date: "2024-01-15"
type DateTime = string;     // ISO 8601 datetime: "2024-01-15T10:30:00Z"
type Guid = string;         // UUID format: "550e8400-e29b-41d4-a716-446655440000"
```

---

## Authentication

### POST /api/v1/auth/login

Authenticate and receive JWT tokens.

**Request:**
```typescript
interface LoginRequest {
  username: string;
  password: string;
}
```

**Response (200):**
```typescript
interface LoginResponse {
  data: {
    accessToken: string;      // JWT, expires in 15 minutes
    refreshToken: string;     // Opaque token, expires in 7 days
    expiresAt: DateTime;      // Access token expiration
    user: {
      idKey: IdKey;
      firstName: string;
      lastName: string;
      email: string;
      photoUrl?: string;
    };
  };
}
```

**Error (401):**
```typescript
{
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Invalid username or password"
  }
}
```

---

### POST /api/v1/auth/refresh

Refresh an expired access token.

**Request:**
```typescript
interface RefreshRequest {
  refreshToken: string;
}
```

**Response (200):**
```typescript
interface RefreshResponse {
  data: {
    accessToken: string;
    expiresAt: DateTime;
  };
}
```

---

### POST /api/v1/auth/logout

Invalidate refresh token.

**Request:**
```typescript
interface LogoutRequest {
  refreshToken: string;
}
```

**Response (204):** No content

---

## People

### GET /api/v1/people

Search and list people.

**Query Parameters:**
```typescript
interface PeopleSearchParams extends PaginationParams, SortParams {
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
```

**Response (200):**
```typescript
interface PeopleListResponse {
  data: PersonSummaryDto[];
  meta: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

interface PersonSummaryDto {
  idKey: IdKey;
  firstName: string;
  nickName?: string;
  lastName: string;
  fullName: string;
  email?: string;
  photoUrl?: string;
  age?: number;
  gender: 'Unknown' | 'Male' | 'Female';
  connectionStatus?: DefinedValueDto;
  recordStatus?: DefinedValueDto;
  primaryCampus?: CampusSummaryDto;
}
```

---

### GET /api/v1/people/{idKey}

Get a single person with full details.

**Response (200):**
```typescript
interface PersonDetailResponse {
  data: PersonDetailDto;
}

interface PersonDetailDto {
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
  gender: 'Unknown' | 'Male' | 'Female';
  maritalStatus?: DefinedValueDto;
  anniversaryDate?: DateOnly;
  
  // Contact
  email?: string;
  isEmailActive: boolean;
  emailPreference: 'EmailAllowed' | 'NoMassEmails' | 'DoNotEmail';
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

interface PhoneNumberDto {
  idKey: IdKey;
  number: string;
  numberFormatted: string;
  extension?: string;
  phoneType?: DefinedValueDto;
  isMessagingEnabled: boolean;
  isUnlisted: boolean;
}
```

---

### POST /api/v1/people

Create a new person.

**Request:**
```typescript
interface CreatePersonRequest {
  firstName: string;             // Required, max 50
  nickName?: string;             // Max 50
  middleName?: string;           // Max 50
  lastName: string;              // Required, max 50
  
  titleValueId?: IdKey;
  suffixValueId?: IdKey;
  
  email?: string;                // Valid email format
  gender?: 'Unknown' | 'Male' | 'Female';
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

interface CreatePhoneNumberRequest {
  number: string;                 // Required
  extension?: string;
  phoneTypeValueId?: IdKey;       // Default: Mobile
  isMessagingEnabled?: boolean;   // Default: true for Mobile
}
```

**Response (201):**
```typescript
interface CreatePersonResponse {
  data: PersonDetailDto;
}
```

**Headers:**
```
Location: /api/v1/people/{idKey}
```

---

### PUT /api/v1/people/{idKey}

Update an existing person.

**Request:**
```typescript
interface UpdatePersonRequest {
  firstName?: string;
  nickName?: string;
  middleName?: string;
  lastName?: string;
  
  titleValueId?: IdKey | null;    // null to clear
  suffixValueId?: IdKey | null;
  
  email?: string | null;
  isEmailActive?: boolean;
  emailPreference?: 'EmailAllowed' | 'NoMassEmails' | 'DoNotEmail';
  
  gender?: 'Unknown' | 'Male' | 'Female';
  birthDate?: DateOnly | null;
  maritalStatusValueId?: IdKey | null;
  anniversaryDate?: DateOnly | null;
  
  connectionStatusValueId?: IdKey;
  recordStatusValueId?: IdKey;
  
  primaryCampusId?: IdKey | null;
}
```

**Response (200):**
```typescript
interface UpdatePersonResponse {
  data: PersonDetailDto;
}
```

---

### DELETE /api/v1/people/{idKey}

Soft-delete a person (set record status to Inactive).

**Response (204):** No content

---

### GET /api/v1/people/{idKey}/family

Get the person's family members.

**Response (200):**
```typescript
interface PersonFamilyResponse {
  data: {
    family: FamilyDetailDto;
    members: FamilyMemberDto[];
  };
}

interface FamilyMemberDto {
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  isPersonPrimaryFamily: boolean;
}
```

---

### GET /api/v1/people/{idKey}/groups

Get groups the person belongs to (excluding family).

**Query Parameters:**
```typescript
interface PersonGroupsParams extends PaginationParams {
  groupTypeId?: IdKey;           // Filter by group type
  includeInactive?: boolean;     // Include inactive memberships
}
```

**Response (200):**
```typescript
interface PersonGroupsResponse {
  data: GroupMembershipDto[];
  meta: PaginationMeta;
}

interface GroupMembershipDto {
  group: GroupSummaryDto;
  role: GroupTypeRoleDto;
  status: 'Inactive' | 'Active' | 'Pending';
  dateAdded?: DateTime;
}
```

---

## Families

### GET /api/v1/families

List families.

**Query Parameters:**
```typescript
interface FamiliesSearchParams extends PaginationParams {
  q?: string;                    // Search by family name or member names
  campusId?: IdKey;
  includeInactive?: boolean;
}
```

**Response (200):**
```typescript
interface FamiliesListResponse {
  data: FamilySummaryDto[];
  meta: PaginationMeta;
}

interface FamilySummaryDto {
  idKey: IdKey;
  name: string;
  campus?: CampusSummaryDto;
  memberCount: number;
  primaryAddress?: AddressDto;
}
```

---

### GET /api/v1/families/{idKey}

Get family details with members.

**Response (200):**
```typescript
interface FamilyDetailResponse {
  data: FamilyDetailDto;
}

interface FamilyDetailDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  campus?: CampusSummaryDto;
  members: FamilyMemberDto[];
  addresses: FamilyAddressDto[];
  createdDateTime: DateTime;
  modifiedDateTime?: DateTime;
}

interface FamilyAddressDto {
  idKey: IdKey;
  locationType?: DefinedValueDto;  // Home, Work, Previous
  address: AddressDto;
  isMailingAddress: boolean;
  isMappedAddress: boolean;
}

interface AddressDto {
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  formattedAddress: string;        // Single-line format
  formattedHtmlAddress: string;    // Multi-line HTML format
}
```

---

### POST /api/v1/families

Create a new family.

**Request:**
```typescript
interface CreateFamilyRequest {
  name: string;                   // Required
  campusId?: IdKey;
  
  members?: CreateFamilyMemberRequest[];
  
  address?: CreateAddressRequest;
}

interface CreateFamilyMemberRequest {
  personId?: IdKey;               // Existing person
  // OR create new person inline
  person?: CreatePersonRequest;
  roleId: IdKey;                  // Adult or Child role
}

interface CreateAddressRequest {
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
```

**Response (201):**
```typescript
interface CreateFamilyResponse {
  data: FamilyDetailDto;
}
```

---

### PUT /api/v1/families/{idKey}

Update family details.

**Request:**
```typescript
interface UpdateFamilyRequest {
  name?: string;
  campusId?: IdKey | null;
}
```

**Response (200):**
```typescript
interface UpdateFamilyResponse {
  data: FamilyDetailDto;
}
```

---

### POST /api/v1/families/{idKey}/members

Add a member to a family.

**Request:**
```typescript
interface AddFamilyMemberRequest {
  personId?: IdKey;               // Existing person
  person?: CreatePersonRequest;   // Or create new
  roleId: IdKey;                  // Adult or Child
  setAsPrimaryFamily?: boolean;   // Default: true if person has no primary family
}
```

**Response (201):**
```typescript
interface AddFamilyMemberResponse {
  data: FamilyMemberDto;
}
```

---

### DELETE /api/v1/families/{idKey}/members/{personIdKey}

Remove a member from a family.

**Query Parameters:**
```typescript
interface RemoveFamilyMemberParams {
  removeFromAllGroups?: boolean;  // Also remove from family's groups
}
```

**Response (204):** No content

---

### PUT /api/v1/families/{idKey}/address

Update family address (affects all members).

**Request:**
```typescript
interface UpdateFamilyAddressRequest {
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country?: string;
}
```

**Response (200):**
```typescript
interface UpdateFamilyAddressResponse {
  data: FamilyAddressDto;
}
```

---

## Check-in

### GET /api/v1/checkin/configuration

Get check-in configuration for current kiosk/campus.

**Query Parameters:**
```typescript
interface CheckinConfigParams {
  kioskId?: IdKey;               // Kiosk device ID
  campusId?: IdKey;              // Or campus ID
}
```

**Response (200):**
```typescript
interface CheckinConfigResponse {
  data: CheckinConfigDto;
}

interface CheckinConfigDto {
  areas: CheckinAreaDto[];
  currentSchedules: ScheduleDto[];
  searchType: 'PhoneNumber' | 'Name' | 'PhoneAndName';
  securityCodeLength: number;
  securityCodeAlphanumeric: boolean;
  autoSelectDaysBack: number;
  autoSelectFamily: boolean;
  preventDuplicateCheckin: boolean;
  allowCheckout: boolean;
}

interface CheckinAreaDto {
  idKey: IdKey;
  name: string;
  groups: CheckinGroupDto[];
}

interface CheckinGroupDto {
  idKey: IdKey;
  name: string;
  locations: CheckinLocationDto[];
}

interface CheckinLocationDto {
  idKey: IdKey;
  name: string;
  currentCount: number;
  softThreshold?: number;
  firmThreshold?: number;
  isOpen: boolean;
  schedules: ScheduleDto[];
}

interface ScheduleDto {
  idKey: IdKey;
  name: string;
  startTime: string;             // "09:00"
  isActive: boolean;
  checkInWindowOpen: boolean;
}
```

---

### POST /api/v1/checkin/search

Search for families to check in.

**Request:**
```typescript
interface CheckinSearchRequest {
  searchValue: string;           // Phone number or name
  searchType?: 'Phone' | 'Name' | 'Auto';  // Default: Auto
}
```

**Response (200):**
```typescript
interface CheckinSearchResponse {
  data: CheckinFamilyDto[];
}

interface CheckinFamilyDto {
  idKey: IdKey;
  name: string;
  members: CheckinPersonDto[];
  lastCheckIn?: DateTime;
}

interface CheckinPersonDto {
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
```

---

### GET /api/v1/checkin/opportunities/{familyIdKey}

Get available check-in opportunities for a family.

**Query Parameters:**
```typescript
interface CheckinOpportunitiesParams {
  scheduleId?: IdKey;            // Filter to specific schedule
}
```

**Response (200):**
```typescript
interface CheckinOpportunitiesResponse {
  data: {
    family: CheckinFamilyDto;
    opportunities: PersonOpportunitiesDto[];
  };
}

interface PersonOpportunitiesDto {
  person: CheckinPersonDto;
  currentAttendance: CurrentAttendanceDto[];  // Already checked in
  availableOptions: CheckinOptionDto[];
}

interface CurrentAttendanceDto {
  attendanceIdKey: IdKey;
  group: string;
  location: string;
  schedule: string;
  securityCode: string;
  checkInTime: DateTime;
  canCheckOut: boolean;
}

interface CheckinOptionDto {
  groupIdKey: IdKey;
  groupName: string;
  locations: CheckinLocationOptionDto[];
}

interface CheckinLocationOptionDto {
  locationIdKey: IdKey;
  locationName: string;
  schedules: CheckinScheduleOptionDto[];
  currentCount: number;
  softThreshold?: number;
  firmThreshold?: number;
}

interface CheckinScheduleOptionDto {
  scheduleIdKey: IdKey;
  scheduleName: string;
  startTime: string;
  isSelected: boolean;           // Auto-selected based on config
}
```

---

### POST /api/v1/checkin/attendance

Record check-in attendance.

**Request:**
```typescript
interface RecordAttendanceRequest {
  checkins: CheckinRequestItem[];
}

interface CheckinRequestItem {
  personIdKey: IdKey;
  groupIdKey: IdKey;
  locationIdKey: IdKey;
  scheduleIdKey: IdKey;
}
```

**Response (201):**
```typescript
interface RecordAttendanceResponse {
  data: {
    attendances: AttendanceResultDto[];
    labels: LabelDto[];
  };
}

interface AttendanceResultDto {
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

interface LabelDto {
  attendanceIdKey: IdKey;
  labelType: 'Child' | 'Parent' | 'NameTag';
  printData: string;             // ZPL or other format
  printerAddress?: string;       // IP or device name
}
```

---

### POST /api/v1/checkin/checkout

Record check-out.

**Request:**
```typescript
interface CheckoutRequest {
  attendanceIdKey: IdKey;
}
```

**Response (200):**
```typescript
interface CheckoutResponse {
  data: {
    attendanceIdKey: IdKey;
    checkOutTime: DateTime;
  };
}
```

---

### GET /api/v1/checkin/labels/{attendanceIdKey}

Get printable label for an attendance record.

**Query Parameters:**
```typescript
interface LabelParams {
  labelType?: 'Child' | 'Parent' | 'NameTag' | 'All';
  format?: 'ZPL' | 'PDF';        // Default: ZPL
}
```

**Response (200):**
```typescript
interface LabelResponse {
  data: LabelDto[];
}
```

---

## Groups

### GET /api/v1/groups

List groups.

**Query Parameters:**
```typescript
interface GroupsSearchParams extends PaginationParams {
  q?: string;
  groupTypeId?: IdKey;
  parentGroupId?: IdKey;
  campusId?: IdKey;
  includeInactive?: boolean;
}
```

**Response (200):**
```typescript
interface GroupsListResponse {
  data: GroupSummaryDto[];
  meta: PaginationMeta;
}

interface GroupSummaryDto {
  idKey: IdKey;
  name: string;
  description?: string;
  groupType: GroupTypeSummaryDto;
  campus?: CampusSummaryDto;
  memberCount: number;
  isActive: boolean;
}
```

---

### GET /api/v1/groups/{idKey}

Get group details.

**Response (200):**
```typescript
interface GroupDetailResponse {
  data: GroupDetailDto;
}

interface GroupDetailDto {
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
```

---

### GET /api/v1/groups/{idKey}/members

Get group members.

**Query Parameters:**
```typescript
interface GroupMembersParams extends PaginationParams {
  status?: 'Active' | 'Inactive' | 'Pending' | 'All';
  roleId?: IdKey;
}
```

**Response (200):**
```typescript
interface GroupMembersResponse {
  data: GroupMemberDetailDto[];
  meta: PaginationMeta;
}

interface GroupMemberDetailDto {
  idKey: IdKey;
  person: PersonSummaryDto;
  role: GroupTypeRoleDto;
  status: 'Inactive' | 'Active' | 'Pending';
  dateAdded?: DateTime;
  note?: string;
}
```

---

### POST /api/v1/groups/{idKey}/members

Add a member to a group.

**Request:**
```typescript
interface AddGroupMemberRequest {
  personIdKey: IdKey;
  roleIdKey: IdKey;
  status?: 'Inactive' | 'Active' | 'Pending';  // Default: Active
  note?: string;
}
```

**Response (201):**
```typescript
interface AddGroupMemberResponse {
  data: GroupMemberDetailDto;
}
```

---

### DELETE /api/v1/groups/{idKey}/members/{memberIdKey}

Remove a member from a group.

**Response (204):** No content

---

## Reference Data

### GET /api/v1/defined-types

List defined types.

**Response (200):**
```typescript
interface DefinedTypesResponse {
  data: DefinedTypeDto[];
}

interface DefinedTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  values: DefinedValueDto[];
}

interface DefinedValueDto {
  idKey: IdKey;
  guid: Guid;
  value: string;
  description?: string;
  isActive: boolean;
  order: number;
}
```

---

### GET /api/v1/defined-types/{idKeyOrGuid}/values

Get values for a specific defined type.

**Response (200):**
```typescript
interface DefinedValuesResponse {
  data: DefinedValueDto[];
}
```

---

### GET /api/v1/campuses

List campuses.

**Query Parameters:**
```typescript
interface CampusesParams {
  includeInactive?: boolean;
}
```

**Response (200):**
```typescript
interface CampusesResponse {
  data: CampusDto[];
}

interface CampusDto {
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

interface CampusSummaryDto {
  idKey: IdKey;
  name: string;
  shortCode?: string;
}
```

---

### GET /api/v1/group-types

List group types.

**Response (200):**
```typescript
interface GroupTypesResponse {
  data: GroupTypeDto[];
}

interface GroupTypeDto {
  idKey: IdKey;
  guid: Guid;
  name: string;
  description?: string;
  groupTerm: string;
  groupMemberTerm: string;
  iconCssClass?: string;
  roles: GroupTypeRoleDto[];
}

interface GroupTypeSummaryDto {
  idKey: IdKey;
  name: string;
  iconCssClass?: string;
}

interface GroupTypeRoleDto {
  idKey: IdKey;
  name: string;
  isLeader: boolean;
  order: number;
}
```

---

## Error Codes

Standard error codes used across all endpoints:

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `INVALID_CREDENTIALS` | 401 | Authentication failed |
| `TOKEN_EXPIRED` | 401 | JWT token expired |
| `UNAUTHORIZED` | 401 | Not authenticated |
| `FORBIDDEN` | 403 | Authenticated but not authorized |
| `NOT_FOUND` | 404 | Resource not found |
| `CONFLICT` | 409 | Resource conflict (e.g., duplicate) |
| `UNPROCESSABLE_ENTITY` | 422 | Business rule violation |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

**Example Error Response:**
```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "email": ["Invalid email format"],
      "firstName": ["First name is required", "First name cannot exceed 50 characters"]
    },
    "traceId": "00-abc123-def456-00"
  }
}
```

---

## Rate Limiting

API endpoints are rate-limited per authentication context:

| Context | Limit |
|---------|-------|
| Authenticated user | 1000 requests/minute |
| Kiosk device | 500 requests/minute |
| Unauthenticated | 100 requests/minute |

**Rate Limit Headers:**
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1640000000
```

**Rate Limit Exceeded (429):**
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests. Please try again in 60 seconds."
  }
}
```

---

## Webhook Events (Future)

For external integrations, the following webhook events will be supported:

| Event | Description |
|-------|-------------|
| `person.created` | New person created |
| `person.updated` | Person details updated |
| `family.created` | New family created |
| `family.memberAdded` | Member added to family |
| `attendance.recorded` | Check-in recorded |
| `attendance.checkedOut` | Check-out recorded |

---

## OpenAPI Specification

The complete OpenAPI 3.0 specification will be available at:

- **Development**: `http://localhost:5000/swagger`
- **Production**: `https://api.yourdomain.com/swagger`

Auto-generated TypeScript types will be available via:
```bash
npx openapi-typescript http://localhost:5000/swagger/v1/swagger.json -o src/services/api/types.generated.ts
```
