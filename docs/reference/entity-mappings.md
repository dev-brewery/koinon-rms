# Koinon RMS Entity Mappings

This document defines entity mappings for the Koinon RMS project. Use this as a reference when implementing entities and understanding relationships.

---

## Naming Conventions

| Aspect | Legacy Pattern | Koinon RMS |
|--------|----------------|------------|
| Table names | PascalCase (`Person`) | snake_case (`person`) |
| Column names | PascalCase (`FirstName`) | snake_case (`first_name`) |
| C# properties | PascalCase | PascalCase (unchanged) |
| Foreign keys | `EntityId` | `entity_id` in DB, `EntityId` in C# |
| Primary keys | `Id` (int) | `id` (int), plus `id_key` (string) |

---

## Core Infrastructure Entities

### Entity (Base Class)

All Koinon entities inherit from a base that provides common fields.

**Reference Pattern:**
```csharp
public abstract class Entity<T> : IEntity
{
    public int Id { get; set; }
    public Guid Guid { get; set; }
}
```

**Koinon Implementation:**
```csharp
public abstract class Entity : IEntity, IAuditable
{
    public int Id { get; set; }
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string IdKey => IdKeyConverter.ToIdKey(Id);
    
    // Audit fields
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public int? CreatedByPersonAliasId { get; set; }
    public int? ModifiedByPersonAliasId { get; set; }
}
```

**Database Table:** N/A (abstract base)

**Notes:**
- `IdKey` is a computed property that Base64-encodes the integer Id for URL safety
- All entities get audit fields by default

---

### DefinedType

Lookup table categories (like "Record Status", "Connection Status", "Phone Type").

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | Protected from deletion |
| FieldTypeId | int | field_type_id | FK to FieldType |
| Order | int | order | Display order |
| CategoryId | int? | category_id | FK to Category |
| Name | string(100) | name | Required |
| Description | string | description | |
| HelpText | string | help_text | |
| IsActive | bool | is_active | Default true |

**Database Table:** `defined_type`

**Indexes:**
- `ix_defined_type_name` on `name`
- `ix_defined_type_category_id` on `category_id`

---

### DefinedValue

Individual values within a DefinedType.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | Protected |
| DefinedTypeId | int | defined_type_id | FK, required |
| Order | int | order | Display order |
| Value | string(250) | value | Display value |
| Description | string | description | |
| IsActive | bool | is_active | Default true |

**Database Table:** `defined_value`

**Indexes:**
- `ix_defined_value_defined_type_id` on `defined_type_id`
- `ix_defined_value_value` on `value`

**Well-Known System Values:**

```csharp
public static class SystemDefinedValue
{
    // Record Status
    public static readonly Guid RECORD_STATUS_ACTIVE = new("618F906C-C33D-4FA3-8AEF-E58CB7B63F1E");
    public static readonly Guid RECORD_STATUS_INACTIVE = new("1DAD99D5-41A9-4865-8366-F269902B80A4");
    public static readonly Guid RECORD_STATUS_PENDING = new("283999EC-7346-42E3-B807-BCE9B2BABB49");
    
    // Connection Status
    public static readonly Guid CONNECTION_STATUS_MEMBER = new("41540783-D9EF-4C70-8F1D-C9E83D91ED5F");
    public static readonly Guid CONNECTION_STATUS_ATTENDEE = new("39F491C5-D6AC-4A9B-8AC0-C431CB17D588");
    public static readonly Guid CONNECTION_STATUS_VISITOR = new("B91BA046-BC1E-400C-B85D-638C1F4E0CE2");
    public static readonly Guid CONNECTION_STATUS_PROSPECT = new("368DD475-242C-49C4-A42C-7278BE690CC2");
    
    // Phone Types
    public static readonly Guid PHONE_TYPE_MOBILE = new("407E7E45-7B2E-4FCD-9605-ECB1339F2453");
    public static readonly Guid PHONE_TYPE_HOME = new("AA8732FB-2CEA-4C76-8D6D-6AAA2C6A4303");
    public static readonly Guid PHONE_TYPE_WORK = new("2CC66D5A-F61C-4B74-9AF9-590A9847C13C");
    
    // Group Member Status (may use DefinedValues instead of enums)
    // See GroupMemberStatus enum instead
}
```

---

## Person & Family Entities

### Person

The central entity representing an individual.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | System accounts |
| RecordTypeValueId | int? | record_type_value_id | FK DefinedValue |
| RecordStatusValueId | int? | record_status_value_id | FK DefinedValue |
| RecordStatusLastModifiedDateTime | DateTime? | record_status_last_modified | |
| RecordStatusReasonValueId | int? | record_status_reason_value_id | FK DefinedValue |
| ConnectionStatusValueId | int? | connection_status_value_id | FK DefinedValue |
| ReviewReasonValueId | int? | review_reason_value_id | FK DefinedValue |
| IsDeceased | bool | is_deceased | Default false |
| TitleValueId | int? | title_value_id | FK DefinedValue (Mr., Mrs., etc.) |
| FirstName | string(50) | first_name | Required |
| NickName | string(50) | nick_name | Display name |
| MiddleName | string(50) | middle_name | |
| LastName | string(50) | last_name | Required |
| SuffixValueId | int? | suffix_value_id | FK DefinedValue (Jr., Sr., etc.) |
| PhotoId | int? | photo_id | FK BinaryFile |
| BirthDay | int? | birth_day | 1-31 |
| BirthMonth | int? | birth_month | 1-12 |
| BirthYear | int? | birth_year | |
| Gender | enum | gender | 0=Unknown, 1=Male, 2=Female |
| MaritalStatusValueId | int? | marital_status_value_id | FK DefinedValue |
| AnniversaryDate | Date? | anniversary_date | |
| GraduationYear | int? | graduation_year | |
| GivingGroupId | int? | giving_group_id | FK Group (family for giving) |
| GivingId | string | giving_id | Computed: G{GivingGroupId} or P{Id} |
| GivingLeaderId | int? | giving_leader_id | FK Person |
| Email | string(75) | email | |
| IsEmailActive | bool | is_email_active | Default true |
| EmailNote | string | email_note | |
| EmailPreference | enum | email_preference | 0=EmailAllowed, 1=NoMassEmails, 2=DoNotEmail |
| CommunicationPreference | enum | communication_preference | 0=Email, 1=SMS |
| SystemNote | string | system_note | Internal notes |
| ViewedCount | int? | viewed_count | Profile view tracking |
| TopSignalColor | string(100) | top_signal_color | |
| TopSignalIconCssClass | string(100) | top_signal_icon_css_class | |
| TopSignalId | int? | top_signal_id | |
| AgeClassification | enum | age_classification | 0=Unknown, 1=Adult, 2=Child |
| PrimaryFamilyId | int? | primary_family_id | FK Group (denormalized) |
| PrimaryCampusId | int? | primary_campus_id | FK Campus (denormalized) |
| IsLockedAsChild | bool | is_locked_as_child | Prevents auto-adult |
| DeceasedDate | Date? | deceased_date | |
| ContributionFinancialAccountId | int? | contribution_financial_account_id | Preferred giving account |
| AccountProtectionProfile | enum | account_protection_profile | Security level |

**Database Table:** `person`

**Indexes:**
- `ix_person_last_name_first_name` on `(last_name, first_name)`
- `ix_person_email` on `email` WHERE `email IS NOT NULL`
- `ix_person_guid` UNIQUE on `guid`
- `ix_person_primary_family_id` on `primary_family_id`
- `ix_person_record_status_value_id` on `record_status_value_id`
- GIN index on `search_vector` for full-text search

**Full-Text Search Column:**
```sql
search_vector TSVECTOR GENERATED ALWAYS AS (
    setweight(to_tsvector('english', coalesce(first_name, '')), 'A') ||
    setweight(to_tsvector('english', coalesce(last_name, '')), 'A') ||
    setweight(to_tsvector('english', coalesce(nick_name, '')), 'B') ||
    setweight(to_tsvector('english', coalesce(email, '')), 'C')
) STORED
```

**Computed Properties (C# only, not in DB):**
```csharp
public string FullName => $"{NickName ?? FirstName} {LastName}";
public string FullNameReversed => $"{LastName}, {NickName ?? FirstName}";
public int? Age => CalculateAge(BirthYear, BirthMonth, BirthDay);
public DateTime? BirthDate => CreateBirthDate(BirthYear, BirthMonth, BirthDay);
```

---

### PersonAlias

Koinon uses PersonAlias for all foreign keys to Person, enabling person merging.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| Name | string | name | |
| PersonId | int | person_id | FK Person, required |
| AliasPersonId | int? | alias_person_id | FK Person (merged from) |
| AliasPersonGuid | Guid? | alias_person_guid | |

**Database Table:** `person_alias`

**Notes:**
- When Person A is merged into Person B, A's PersonAlias records point to B
- All FKs throughout system reference PersonAlias, not Person directly
- For Koinon MVP, we may simplify by using Person directly and adding alias support later

---

### PhoneNumber

Phone numbers associated with a person.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| PersonId | int | person_id | FK Person, required |
| CountryCode | string(3) | country_code | Default "1" |
| Number | string(20) | number | Digits only |
| NumberFormatted | string(50) | number_formatted | Display format |
| Extension | string(20) | extension | |
| NumberTypeValueId | int? | number_type_value_id | FK DefinedValue |
| IsMessagingEnabled | bool | is_messaging_enabled | Can receive SMS |
| IsUnlisted | bool | is_unlisted | Hide from directories |
| Description | string | description | |

**Database Table:** `phone_number`

**Indexes:**
- `ix_phone_number_person_id` on `person_id`
- `ix_phone_number_number` on `number`

---

## Group Entities

### GroupType

Defines the type/template for groups (Family, Security Role, Serving Team, etc.).

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| Name | string(100) | name | Required |
| Description | string | description | |
| GroupTerm | string(100) | group_term | "Group", "Family", "Team" |
| GroupMemberTerm | string(100) | group_member_term | "Member", "Participant" |
| DefaultGroupRoleId | int? | default_group_role_id | FK GroupTypeRole |
| AllowMultipleLocations | bool | allow_multiple_locations | |
| ShowInGroupList | bool | show_in_group_list | |
| ShowInNavigation | bool | show_in_navigation | |
| IconCssClass | string(100) | icon_css_class | |
| TakesAttendance | bool | takes_attendance | |
| GroupsRequireCampus | bool | groups_require_campus | |
| GroupAttendanceRequiresLocation | bool | group_attendance_requires_location | |
| GroupAttendanceRequiresSchedule | bool | group_attendance_requires_schedule | |
| Order | int | order | |
| InheritedGroupTypeId | int? | inherited_group_type_id | FK GroupType |
| AllowedScheduleTypes | enum | allowed_schedule_types | Flags: None, Weekly, Custom, Named |
| LocationSelectionMode | enum | location_selection_mode | Flags: None, Address, Named, Point, Polygon, GroupMember |
| GroupTypePurposeValueId | int? | group_type_purpose_value_id | FK DefinedValue |
| EnableGroupHistory | bool | enable_group_history | |
| GroupCapacityRule | enum | group_capacity_rule | None, Recommended, Hard |
| IsSchedulingEnabled | bool | is_scheduling_enabled | |
| AttendanceRule | enum | attendance_rule | None, AddOnCheckIn, AlreadyBelongs |
| AttendancePrintTo | enum | attendance_print_to | Default, Kiosk, Location |

**Database Table:** `group_type`

**Well-Known System GroupTypes:**
```csharp
public static class SystemGroupType
{
    public static readonly Guid FAMILY = new("790E3215-3B10-442B-AF69-616C0DCB998E");
    public static readonly Guid SECURITY_ROLE = new("AECE949F-704C-483E-A4FB-93D5E4720C4C");
    public static readonly Guid CHECK_IN_TEMPLATE = new("6E7AD783-7614-4721-ABC1-35842113EF59");
    public static readonly Guid SERVING_TEAM = new("2C42B2D4-1C5F-4AD5-A9AD-08631B872AC4");
    public static readonly Guid SMALL_GROUP = new("50FCFB30-F51A-49DF-86F4-2B176EA1820B");
}
```

---

### GroupTypeRole

Roles available within a GroupType (e.g., "Adult", "Child" for Family).

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| GroupTypeId | int | group_type_id | FK GroupType, required |
| Name | string(100) | name | Required |
| Description | string | description | |
| Order | int | order | |
| MaxCount | int? | max_count | |
| MinCount | int? | min_count | |
| IsLeader | bool | is_leader | |
| ReceiveRequirementsNotifications | bool | receive_requirements_notifications | |
| CanView | bool | can_view | |
| CanEdit | bool | can_edit | |
| CanManageMembers | bool | can_manage_members | |

**Database Table:** `group_type_role`

**Well-Known System Roles:**
```csharp
public static class SystemGroupTypeRole
{
    public static readonly Guid FAMILY_ADULT = new("2639F9A5-2AAE-4E48-A8C3-4FFE86681E42");
    public static readonly Guid FAMILY_CHILD = new("C8B1814F-6AA7-4055-B2D7-48FE20429CB9");
}
```

---

### Group

A collection of people with a specific purpose.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| ParentGroupId | int? | parent_group_id | FK Group (self-reference) |
| GroupTypeId | int | group_type_id | FK GroupType, required |
| CampusId | int? | campus_id | FK Campus |
| ScheduleId | int? | schedule_id | FK Schedule |
| Name | string(100) | name | Required |
| Description | string | description | |
| IsSecurityRole | bool | is_security_role | |
| IsActive | bool | is_active | Default true |
| Order | int | order | |
| GroupCapacity | int? | group_capacity | |
| RequiredSignatureDocumentTemplateId | int? | required_signature_document_template_id | |
| IsArchived | bool | is_archived | Soft delete |
| ArchivedDateTime | DateTime? | archived_date_time | |
| ArchivedByPersonAliasId | int? | archived_by_person_alias_id | |
| StatusValueId | int? | status_value_id | FK DefinedValue |
| ElevatedSecurityLevel | enum | elevated_security_level | None, Extreme, High |
| SchedulingMustMeetRequirements | bool | scheduling_must_meet_requirements | |
| AttendanceRecordRequiredForCheckIn | enum | attendance_record_required_for_check_in | |

**Database Table:** `group`

**Notes:** 
- "group" is a reserved word in SQL; ensure proper quoting
- Families are Groups where GroupType.Guid = FAMILY

---

### GroupMember

Links a Person to a Group with a specific role.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| GroupId | int | group_id | FK Group, required |
| PersonId | int | person_id | FK Person, required |
| GroupRoleId | int | group_role_id | FK GroupTypeRole, required |
| GroupMemberStatus | enum | group_member_status | 0=Inactive, 1=Active, 2=Pending |
| Note | string | note | |
| IsNotified | bool | is_notified | |
| GuestCount | int? | guest_count | For attendance |
| DateTimeAdded | DateTime? | date_time_added | |
| IsArchived | bool | is_archived | |
| ArchivedDateTime | DateTime? | archived_date_time | |
| ArchivedByPersonAliasId | int? | archived_by_person_alias_id | |
| ScheduleTemplateId | int? | schedule_template_id | |
| ScheduleStartDate | Date? | schedule_start_date | |
| ScheduleReminderEmailOffsetDays | int? | schedule_reminder_email_offset_days | |
| CommunicationPreference | enum | communication_preference | 0=Email, 1=SMS |
| InactiveDateTime | DateTime? | inactive_date_time | |

**Database Table:** `group_member`

**Indexes:**
- `ix_group_member_group_id` on `group_id`
- `ix_group_member_person_id` on `person_id`
- `ix_group_member_group_role_id` on `group_role_id`
- `uix_group_member_group_person_role` UNIQUE on `(group_id, person_id, group_role_id)` WHERE NOT `is_archived`

---

## Location Entities

### Campus

A physical church location/site.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IsSystem | bool | is_system | |
| Name | string(100) | name | Required |
| Description | string | description | |
| IsActive | bool | is_active | Default true |
| ShortCode | string(50) | short_code | Abbreviated name |
| Url | string(200) | url | Campus website |
| LocationId | int? | location_id | FK Location |
| PhoneNumber | string(50) | phone_number | |
| LeaderPersonAliasId | int? | leader_person_alias_id | FK PersonAlias |
| ServiceTimes | string | service_times | Formatted string |
| Order | int | order | |
| TimeZoneId | string(50) | time_zone_id | IANA timezone |
| CampusStatusValueId | int? | campus_status_value_id | FK DefinedValue |
| CampusTypeValueId | int? | campus_type_value_id | FK DefinedValue |

**Database Table:** `campus`

---

### Location

A physical place (address, room, building, geo-point).

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| ParentLocationId | int? | parent_location_id | FK Location (self-ref) |
| Name | string(100) | name | |
| IsActive | bool | is_active | Default true |
| LocationTypeValueId | int? | location_type_value_id | FK DefinedValue |
| GeoPoint | geography | geo_point | PostGIS POINT |
| GeoFence | geography | geo_fence | PostGIS POLYGON |
| Street1 | string(100) | street1 | |
| Street2 | string(100) | street2 | |
| City | string(50) | city | |
| County | string(50) | county | |
| State | string(50) | state | |
| Country | string(50) | country | |
| PostalCode | string(50) | postal_code | |
| Barcode | string(40) | barcode | |
| AssessorParcelId | string(50) | assessor_parcel_id | |
| StandardizeAttemptedDateTime | DateTime? | standardize_attempted_date_time | |
| StandardizeAttemptedServiceType | string(50) | standardize_attempted_service_type | |
| StandardizeAttemptedResult | string(200) | standardize_attempted_result | |
| StandardizedDateTime | DateTime? | standardized_date_time | |
| GeocodeAttemptedDateTime | DateTime? | geocode_attempted_date_time | |
| GeocodeAttemptedServiceType | string(50) | geocode_attempted_service_type | |
| GeocodeAttemptedResult | string(200) | geocode_attempted_result | |
| GeocodedDateTime | DateTime? | geocoded_date_time | |
| IsGeoPointLocked | bool | is_geo_point_locked | |
| PrinterDeviceId | int? | printer_device_id | FK Device |
| ImageId | int? | image_id | FK BinaryFile |
| SoftRoomThreshold | int? | soft_room_threshold | Warning capacity |
| FirmRoomThreshold | int? | firm_room_threshold | Hard capacity |

**Database Table:** `location`

**PostGIS Setup:**
```sql
CREATE EXTENSION IF NOT EXISTS postgis;

-- geo_point is GEOGRAPHY(POINT, 4326)
-- geo_fence is GEOGRAPHY(POLYGON, 4326)
```

---

## Attendance/Check-in Entities

### Schedule

Defines when something occurs (service times, group meetings).

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| Name | string(50) | name | |
| Description | string | description | |
| iCalendarContent | string | icalendar_content | iCal RRULE |
| CheckInStartOffsetMinutes | int? | check_in_start_offset_minutes | |
| CheckInEndOffsetMinutes | int? | check_in_end_offset_minutes | |
| EffectiveStartDate | Date? | effective_start_date | |
| EffectiveEndDate | Date? | effective_end_date | |
| CategoryId | int? | category_id | FK Category |
| WeeklyDayOfWeek | enum? | weekly_day_of_week | 0=Sunday through 6=Saturday |
| WeeklyTimeOfDay | TimeSpan? | weekly_time_of_day | |
| Order | int | order | |
| IsActive | bool | is_active | Default true |
| AutoInactivateWhenComplete | bool | auto_inactivate_when_complete | |
| IsPublic | bool | is_public | Show in public calendars |

**Database Table:** `schedule`

---

### Attendance

Records a person's attendance at a location/group/schedule.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| OccurrenceId | int | occurrence_id | FK AttendanceOccurrence |
| PersonAliasId | int? | person_alias_id | FK PersonAlias |
| DeviceId | int? | device_id | FK Device (kiosk) |
| AttendanceCodeId | int? | attendance_code_id | FK AttendanceCode |
| QualifierValueId | int? | qualifier_value_id | FK DefinedValue |
| StartDateTime | DateTime | start_date_time | Required |
| EndDateTime | DateTime? | end_date_time | Check-out time |
| RSVP | enum | rsvp | 0=No, 1=Yes, 2=Maybe, 3=Unknown |
| DidAttend | bool? | did_attend | |
| Note | string | note | |
| CampusId | int? | campus_id | FK Campus (denormalized) |
| ProcessedDateTime | DateTime? | processed_date_time | |
| IsFirstTime | bool | is_first_time | First time at this group |
| PresentDateTime | DateTime? | present_date_time | When marked present |
| PresentByPersonAliasId | int? | present_by_person_alias_id | Who marked present |
| CheckedOutByPersonAliasId | int? | checked_out_by_person_alias_id | |
| RequestedToAttend | bool | requested_to_attend | Scheduling |
| ScheduledToAttend | bool | scheduled_to_attend | Scheduling |
| DeclineReasonValueId | int? | decline_reason_value_id | FK DefinedValue |
| ScheduledByPersonAliasId | int? | scheduled_by_person_alias_id | |
| ScheduleConfirmationSent | bool | schedule_confirmation_sent | |

**Database Table:** `attendance`

**Indexes:**
- `ix_attendance_occurrence_id` on `occurrence_id`
- `ix_attendance_person_alias_id` on `person_alias_id`
- `ix_attendance_start_date_time` on `start_date_time`
- `ix_attendance_did_attend` on `did_attend` WHERE `did_attend = true`

---

### AttendanceCode

Security code printed on check-in labels.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| IssueDateTime | DateTime | issue_date_time | Required |
| Code | string(10) | code | The actual code |

**Database Table:** `attendance_code`

**Indexes:**
- `uix_attendance_code_issue_date_code` UNIQUE on `(DATE(issue_date_time), code)`

**Notes:**
- Codes must be unique per day
- Typically 3-4 characters, alphanumeric or numeric only
- Format configurable per check-in area

---

### AttendanceOccurrence

A specific occurrence of a group meeting at a location on a schedule.

**Field Mappings:**

| Field | Type | Column | Notes |
|------------|------|-----------------|-------|
| Id | int | id | PK |
| Guid | Guid | guid | Unique |
| GroupId | int? | group_id | FK Group |
| LocationId | int? | location_id | FK Location |
| ScheduleId | int? | schedule_id | FK Schedule |
| OccurrenceDate | Date | occurrence_date | Required |
| DidNotOccur | bool? | did_not_occur | Meeting was cancelled |
| SundayDate | Date | sunday_date | Week identifier |
| Notes | string | notes | |
| AnonymousAttendanceCount | int? | anonymous_attendance_count | Headcount without names |
| AttendanceTypeValueId | int? | attendance_type_value_id | FK DefinedValue |
| DeclineConfirmationMessage | string | decline_confirmation_message | |
| ShowDeclineReasons | bool | show_decline_reasons | |
| AcceptConfirmationMessage | string | accept_confirmation_message | |

**Database Table:** `attendance_occurrence`

**Indexes:**
- `uix_attendance_occurrence_group_location_schedule_date` UNIQUE on `(group_id, location_id, schedule_id, occurrence_date)`

---

## Enumerations

### Gender
```csharp
public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2
}
```

### EmailPreference
```csharp
public enum EmailPreference
{
    EmailAllowed = 0,
    NoMassEmails = 1,
    DoNotEmail = 2
}
```

### GroupMemberStatus
```csharp
public enum GroupMemberStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}
```

### RSVP
```csharp
public enum RSVP
{
    No = 0,
    Yes = 1,
    Maybe = 2,
    Unknown = 3
}
```

### CommunicationPreference
```csharp
public enum CommunicationPreference
{
    Email = 0,
    SMS = 1,
    PushNotification = 2
}
```

### AgeClassification
```csharp
public enum AgeClassification
{
    Unknown = 0,
    Adult = 1,
    Child = 2
}
```

---

## Entity Relationships Diagram (Simplified)

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│  Campus     │       │  GroupType  │       │DefinedType  │
└──────┬──────┘       └──────┬──────┘       └──────┬──────┘
       │                     │                     │
       │              ┌──────┴──────┐              │
       │              │GroupTypeRole│              │
       │              └──────┬──────┘              │
       │                     │                     │
       ▼                     ▼                     ▼
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│   Group     │◄──────│ GroupMember │───────►│DefinedValue │
│  (Family)   │       └──────┬──────┘       └─────────────┘
└──────┬──────┘              │                     ▲
       │                     │                     │
       │              ┌──────┴──────┐              │
       │              │   Person    │──────────────┘
       │              └──────┬──────┘
       │                     │
       │              ┌──────┴──────┐
       │              │ PhoneNumber │
       │              └─────────────┘
       │
       ▼
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│  Location   │◄──────│ Attendance  │───────►│  Schedule   │
└─────────────┘       │ Occurrence  │       └─────────────┘
                      └──────┬──────┘
                             │
                      ┌──────┴──────┐
                      │ Attendance  │
                      └──────┬──────┘
                             │
                      ┌──────┴──────┐
                      │Attendance   │
                      │   Code      │
                      └─────────────┘
```

---

## Migration Notes

1. **PersonAlias Simplification**: For MVP, consider using Person directly and adding alias support later. This simplifies FKs throughout the system.

2. **Attribute System**: Koinon's dynamic attribute system (EAV pattern) is complex. Defer to Phase 2+.

3. **GUIDs**: Use standard system GUIDs for well-known entity types (defined in SystemGuid class).

4. **Soft Deletes**: Use `is_archived` pattern for entities that support it (Group, GroupMember).

5. **Audit Fields**: Add `created_date_time`, `modified_date_time`, `created_by_person_alias_id`, `modified_by_person_alias_id` to all entities via base class.
