export interface CampusSummaryDto {
  idKey: string;
  name: string;
  shortCode?: string;
}

export interface CampusDto {
  idKey: string;
  guid: string;
  name: string;
  shortCode?: string;
  description?: string;
  isActive: boolean;
  url?: string;
  phoneNumber?: string;
  timeZoneId?: string;
  serviceTimes?: string;
  order: number;
  createdDateTime?: string;
  modifiedDateTime?: string;
}

export interface CreateCampusRequest {
  name: string;
  shortCode?: string;
  description?: string;
  url?: string;
  phoneNumber?: string;
  timeZoneId?: string;
  serviceTimes?: string;
  order?: number;
}

export interface UpdateCampusRequest {
  name?: string;
  shortCode?: string;
  description?: string;
  isActive?: boolean;
  url?: string;
  phoneNumber?: string;
  timeZoneId?: string;
  serviceTimes?: string;
  order?: number;
}
