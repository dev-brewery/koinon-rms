// Location TypeScript types matching backend DTOs

// LocationDto - Full location with all fields
export interface LocationDto {
  idKey: string;
  guid: string;
  name: string;
  description?: string;
  isActive: boolean;
  order: number;
  parentLocationIdKey?: string;
  parentLocationName?: string;
  campusIdKey?: string;
  campusName?: string;
  locationTypeName?: string;
  softRoomThreshold?: number;
  firmRoomThreshold?: number;
  staffToChildRatio?: number;
  overflowLocationIdKey?: string;
  overflowLocationName?: string;
  autoAssignOverflow: boolean;
  street1?: string;
  street2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  latitude?: number;
  longitude?: number;
  isGeoPointLocked: boolean;
  children: LocationDto[];
  createdDateTime?: string;
  modifiedDateTime?: string;
}

// LocationSummaryDto - Lightweight for lists (matches backend exactly)
export interface LocationSummaryDto {
  idKey: string;
  name: string;
  description?: string;
  isActive: boolean;
  parentLocationName?: string;
  campusName?: string;
  locationTypeName?: string;
}

// Request types for create/update
export interface CreateLocationRequest {
  name: string;
  description?: string;
  parentLocationIdKey?: string;
  campusIdKey?: string;
  locationTypeValueIdKey?: string;
  softRoomThreshold?: number;
  firmRoomThreshold?: number;
  staffToChildRatio?: number;
  overflowLocationIdKey?: string;
  autoAssignOverflow?: boolean;
  street1?: string;
  street2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  latitude?: number;
  longitude?: number;
  isGeoPointLocked?: boolean;
  order?: number;
}

export interface UpdateLocationRequest {
  name?: string;
  description?: string;
  parentLocationIdKey?: string;
  campusIdKey?: string;
  locationTypeValueIdKey?: string;
  softRoomThreshold?: number;
  firmRoomThreshold?: number;
  staffToChildRatio?: number;
  overflowLocationIdKey?: string;
  autoAssignOverflow?: boolean;
  street1?: string;
  street2?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  latitude?: number;
  longitude?: number;
  isGeoPointLocked?: boolean;
  order?: number;
  isActive?: boolean;
}
