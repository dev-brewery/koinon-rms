namespace Koinon.Domain.Data;

/// <summary>
/// Contains well-known system GUIDs for DefinedTypes and DefinedValues.
/// These GUIDs must match the values seeded in the database migrations.
/// </summary>
public static class SystemGuid
{
    /// <summary>
    /// System GUIDs for well-known DefinedTypes.
    /// </summary>
    public static class DefinedType
    {
        /// <summary>
        /// Person Record Status (Active, Inactive, Pending).
        /// </summary>
        public static readonly Guid PersonRecordStatus = new("8522BADD-2871-45A5-81DD-C76DA07E2E7E");

        /// <summary>
        /// Person Connection Status (Member, Attendee, Visitor, Prospect).
        /// </summary>
        public static readonly Guid PersonConnectionStatus = new("2E6540EA-63F0-40FE-BE50-F2A84735E600");

        /// <summary>
        /// Person Title (Mr., Mrs., Ms., Dr., etc.).
        /// </summary>
        public static readonly Guid PersonTitle = new("4784CD23-518B-43EE-9B97-225BF6E07846");

        /// <summary>
        /// Person Suffix (Jr., Sr., III, etc.).
        /// </summary>
        public static readonly Guid PersonSuffix = new("16F85B3C-B3E8-434B-A3E2-8A6B9EE0D56E");

        /// <summary>
        /// Phone Number Type (Mobile, Home, Work).
        /// </summary>
        public static readonly Guid PhoneNumberType = new("8345DD45-73C6-4F5E-BEBD-B77FC83F18FD");

        /// <summary>
        /// Group Location Type (Meeting Location, Home, etc.).
        /// </summary>
        public static readonly Guid GroupLocationType = new("2E68D37C-FB7B-4AA5-9E09-3785D52156CB");

        /// <summary>
        /// Location Type (Building, Room, Address, etc.).
        /// </summary>
        public static readonly Guid LocationType = new("3285DCEF-FAA4-43B9-9338-983F4A384ABA");

        /// <summary>
        /// Campus Status (Active, Inactive).
        /// </summary>
        public static readonly Guid CampusStatus = new("840C414E-A261-4D18-946B-0F9B14174B4E");

        /// <summary>
        /// Transaction Type (Contribution, Event Registration, etc.).
        /// </summary>
        public static readonly Guid TransactionType = new("2AACBE45-9C69-4D47-9F30-DDCE7D39E1B4");
    }

    /// <summary>
    /// System GUIDs for well-known DefinedValues.
    /// </summary>
    public static class DefinedValue
    {
        // Record Status Values
        /// <summary>
        /// Record Status: Active.
        /// </summary>
        public static readonly Guid RecordStatusActive = new("618F906C-C33D-4FA3-8AEF-E58CB7B63F1E");

        /// <summary>
        /// Record Status: Inactive.
        /// </summary>
        public static readonly Guid RecordStatusInactive = new("1DAD99D5-41A9-4865-8366-F269902B80A4");

        /// <summary>
        /// Record Status: Pending.
        /// </summary>
        public static readonly Guid RecordStatusPending = new("283999EC-7346-42E3-B807-BCE9B2BABB49");

        // Connection Status Values
        /// <summary>
        /// Connection Status: Member.
        /// </summary>
        public static readonly Guid ConnectionStatusMember = new("41540783-D9EF-4C70-8F1D-C9E83D91ED5F");

        /// <summary>
        /// Connection Status: Attendee.
        /// </summary>
        public static readonly Guid ConnectionStatusAttendee = new("39F491C5-D6AC-4A9B-8AC0-C431CB17D588");

        /// <summary>
        /// Connection Status: Visitor.
        /// </summary>
        public static readonly Guid ConnectionStatusVisitor = new("B91BA046-BC1E-400C-B85D-638C1F4E0CE2");

        /// <summary>
        /// Connection Status: Prospect.
        /// </summary>
        public static readonly Guid ConnectionStatusProspect = new("368DD475-242C-49C4-A42C-7278BE690CC2");

        // Phone Types
        /// <summary>
        /// Phone Type: Mobile.
        /// </summary>
        public static readonly Guid PhoneTypeMobile = new("407E7E45-7B2E-4FCD-9605-ECB1339F2453");

        /// <summary>
        /// Phone Type: Home.
        /// </summary>
        public static readonly Guid PhoneTypeHome = new("AA8732FB-2CEA-4C76-8D6D-6AAA2C6A4303");

        /// <summary>
        /// Phone Type: Work.
        /// </summary>
        public static readonly Guid PhoneTypeWork = new("2CC66D5A-F61C-4B74-9AF9-590A9847C13C");

        // Transaction Type Values
        /// <summary>
        /// Transaction Type: Contribution.
        /// </summary>
        public static readonly Guid TransactionTypeContribution = new("2D607262-52D6-4724-910D-424651F01C8B");

        /// <summary>
        /// Transaction Type: Event Registration.
        /// </summary>
        public static readonly Guid TransactionTypeEventRegistration = new("4B0B5C34-8E8A-4F1E-9D3A-5B7F8E2A3C4D");

        /// <summary>
        /// Transaction Type: Pledge.
        /// </summary>
        public static readonly Guid TransactionTypePledge = new("7C9E2F45-6A1B-4D8E-A2C3-8F7E9B4A5D6C");

        /// <summary>
        /// Transaction Type: Refund.
        /// </summary>
        public static readonly Guid TransactionTypeRefund = new("9E4D6B78-3C2A-4F5E-B1D7-6A8C9E3F2B5D");
    }

    /// <summary>
    /// System GUIDs for well-known Campus entities.
    /// </summary>
    public static class Campus
    {
        /// <summary>
        /// Main Campus (default campus).
        /// </summary>
        public static readonly Guid Main = new("76882AE3-1CE8-42A6-A2B6-8C0B29CF8CF8");
    }

    /// <summary>
    /// System GUIDs for well-known LocationType DefinedValues.
    /// </summary>
    public static class LocationType
    {
        /// <summary>
        /// Location Type: Campus.
        /// </summary>
        public static readonly Guid Campus = new("C0D7AE35-7901-4396-870E-3AAF472AAE88");

        /// <summary>
        /// Location Type: Building.
        /// </summary>
        public static readonly Guid Building = new("D0B5F0BB-4E2E-4E94-B24B-5A6A89A52F9E");

        /// <summary>
        /// Location Type: Room.
        /// </summary>
        public static readonly Guid Room = new("107C6DA1-266D-4E1C-A443-1CD37064601D");
    }

    /// <summary>
    /// System GUIDs for well-known GroupType entities.
    /// </summary>
    public static class GroupType
    {
        /// <summary>
        /// Family GroupType (represents family units).
        /// </summary>
        public static readonly Guid Family = new("790E3215-3B10-442B-AF69-616C0DCB998E");

        /// <summary>
        /// Security Role GroupType (represents security/permission groups).
        /// </summary>
        public static readonly Guid SecurityRole = new("AECE949F-704C-483E-A4FB-93D5E4720C4C");

        /// <summary>
        /// Small Group GroupType (represents small groups/life groups).
        /// </summary>
        public static readonly Guid SmallGroup = new("50FCFB30-F51A-49DF-86F4-2B176EA1820B");

        /// <summary>
        /// Serving Team GroupType (represents volunteer teams).
        /// </summary>
        public static readonly Guid ServingTeam = new("2C42B2D4-1C5F-4AD5-A9AD-08631B872AC4");

        /// <summary>
        /// Check-in Template GroupType (represents check-in area configurations).
        /// </summary>
        public static readonly Guid CheckInTemplate = new("6E7AD783-7614-4721-ABC1-35842113EF59");
    }

    /// <summary>
    /// System GUIDs for well-known GroupTypeRole entities.
    /// </summary>
    public static class GroupTypeRole
    {
        /// <summary>
        /// Family Adult role (adult members of a family).
        /// </summary>
        public static readonly Guid FamilyAdult = new("2639F9A5-2AAE-4E48-A8C3-4FFE86681E42");

        /// <summary>
        /// Family Child role (child members of a family).
        /// </summary>
        public static readonly Guid FamilyChild = new("C8B1814F-6AA7-4055-B2D7-48FE20429CB9");
    }
}
