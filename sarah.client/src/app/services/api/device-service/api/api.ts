export * from './animations.service';
import { AnimationsClient } from './animations.service';
export * from './animations.serviceInterface';
export * from './devices.service';
import { DevicesClient } from './devices.service';
export * from './devices.serviceInterface';
export const APIS = [AnimationsClient, DevicesClient];
