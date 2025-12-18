export interface TargetField {
  value: string;
  label: string;
  group: 'Required' | 'Contact' | 'Family' | 'Address' | 'Personal' | 'Status' | 'Context';
  required?: boolean;
}

export const PEOPLE_TARGET_FIELDS: TargetField[] = [
  // Required
  { value: 'FirstName', label: 'First Name', group: 'Required', required: true },
  { value: 'LastName', label: 'Last Name', group: 'Required', required: true },
  
  // Contact
  { value: 'Email', label: 'Email', group: 'Contact' },
  { value: 'MobilePhone', label: 'Mobile Phone', group: 'Contact' },
  { value: 'HomePhone', label: 'Home Phone', group: 'Contact' },
  { value: 'WorkPhone', label: 'Work Phone', group: 'Contact' },
  
  // Family
  { value: 'ExternalFamilyId', label: 'Family ID', group: 'Family' },
  { value: 'FamilyRole', label: 'Family Role', group: 'Family' },
  { value: 'FamilyName', label: 'Family Name', group: 'Family' },
  
  // Address
  { value: 'Street1', label: 'Street Address 1', group: 'Address' },
  { value: 'Street2', label: 'Street Address 2', group: 'Address' },
  { value: 'City', label: 'City', group: 'Address' },
  { value: 'State', label: 'State', group: 'Address' },
  { value: 'PostalCode', label: 'Postal Code', group: 'Address' },
  { value: 'Country', label: 'Country', group: 'Address' },
  
  // Personal
  { value: 'NickName', label: 'Nick Name', group: 'Personal' },
  { value: 'Gender', label: 'Gender', group: 'Personal' },
  { value: 'BirthDate', label: 'Birth Date', group: 'Personal' },
  { value: 'MaritalStatus', label: 'Marital Status', group: 'Personal' },
  
  // Status
  { value: 'ConnectionStatus', label: 'Connection Status', group: 'Status' },
  { value: 'RecordStatus', label: 'Record Status', group: 'Status' },
  { value: 'Campus', label: 'Campus', group: 'Status' },
];

export const ATTENDANCE_TARGET_FIELDS: TargetField[] = [
  // Required
  { value: 'PersonIdentifier', label: 'Person Identifier', group: 'Required', required: true },
  { value: 'AttendanceDate', label: 'Attendance Date', group: 'Required', required: true },
  
  // Context
  { value: 'GroupName', label: 'Group Name', group: 'Context' },
  { value: 'ScheduleName', label: 'Schedule Name', group: 'Context' },
  { value: 'LocationName', label: 'Location Name', group: 'Context' },
];
