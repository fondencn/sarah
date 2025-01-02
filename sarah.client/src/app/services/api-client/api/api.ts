export * from './devices.service';
import { DevicesService } from './devices.service';
export * from './status.service';
import { StatusService } from './status.service';
export const APIS = [DevicesService, StatusService];
