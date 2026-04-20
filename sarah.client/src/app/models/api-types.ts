export type DashboardItemType = 0 | 1 | 2 | 3 | 4;

export const DashboardItemType = {
  NUMBER_0: 0 as DashboardItemType,
  NUMBER_1: 1 as DashboardItemType,
  NUMBER_2: 2 as DashboardItemType,
  NUMBER_3: 3 as DashboardItemType,
  NUMBER_4: 4 as DashboardItemType
};

export type DashboardItemTypeDto = DashboardItemType;

export const DashboardItemTypeDto = {
  NUMBER_0: 0 as DashboardItemTypeDto,
  NUMBER_1: 1 as DashboardItemTypeDto,
  NUMBER_2: 2 as DashboardItemTypeDto,
  NUMBER_3: 3 as DashboardItemTypeDto,
  NUMBER_4: 4 as DashboardItemTypeDto
};

export interface ExtendedPropertyDto {
  key?: string | null;
  value?: string | null;
}

export interface DashboardItemDto {
  id?: number;
  itemId?: number;
  itemType?: DashboardItemType;
  title?: string | null;
  description?: string | null;
  subtype?: string | null;
  extendedProperties?: Array<ExtendedPropertyDto> | null;
  position?: number;
}

export interface CreateDashboardItemDto extends DashboardItemDto {}

export interface ReorderDashboardItemsDto {
  orderedIds?: number[] | null;
}

export interface StatusDto {
  hostname?: string | null;
  port?: number;
  isAuthenticated?: boolean;
  username?: string | null;
  controllerStatus?: string | null;
}

export interface NamedLocationDto {
  longitude?: number;
  latitude?: number;
  name?: string | null;
}

export interface LocationDto {
  longitude?: number;
  latitude?: number;
}

export interface NetworkElementDto {
  id?: number;
  type?: string | null;
}

export interface TrackerDto {
  id?: number;
  name?: string | null;
}

export interface PersonDto {
  id?: number;
  name?: string | null;
  gpsTrackerID?: number;
  gpsTrackerName?: string | null;
  currentGeoFence?: string | null;
  currentPosition?: string | null;
  isAtHome?: boolean;
  mobilePhoneHostname?: string | null;
  isFavourite?: boolean;
}
