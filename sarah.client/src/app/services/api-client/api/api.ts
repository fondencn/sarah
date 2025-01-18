export * from './baseData.service';
import { BaseDataService } from './baseData.service';
export * from './dashboard.service';
import { DashboardService } from './dashboard.service';
export * from './devices.service';
import { DevicesService } from './devices.service';
export * from './status.service';
import { StatusService } from './status.service';
export const APIS = [BaseDataService, DashboardService, DevicesService, StatusService];
