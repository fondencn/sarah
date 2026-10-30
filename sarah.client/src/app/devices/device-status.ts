import { DeviceDtoModel as DeviceDto } from '../services/api/device-service/model/models';

export type DeviceStatusCategory = 'lamp' | 'wallplug' | 'doorsensor' | 'thermostat' | 'airquality' | 'motionsensor' | 'unknown';

export interface DeviceStatus {
  category: DeviceStatusCategory;
  isOn: boolean;
  doorIsOpen: boolean;
  lastOpenDurationMinutes: number | null;
  lastStateChanged: string | null;
  presence: boolean;
  brightness: number | null;
  color: string | null;
  powerConsumption: number | null;
  temperature: number | null;
  temperatureSetpoint: number | null;
  battery: number | null;
  luminance: number | null;
  humidity: number | null;
  co2: number | null;
  voc: number | null;
}

/**
 * Shared device state reader. Detail responses use ExtendedProperties; list
 * responses retain the original sub-DTO fallback used by the devices table.
 */
export function getDeviceStatus(device: DeviceDto, detail?: DeviceDto | null): DeviceStatus {
  const base = device as any;
  const detailed = detail as any;
  const props: Array<{ key?: string; value?: string }> =
    detailed?.extendedProperties ?? base.extendedProperties ?? [];
  const getProp = (key: string): string | undefined => props.find(p => p.key === key)?.value;
  const parseNumber = (value: string | number | null | undefined): number | null => {
    if (value === undefined || value === null || value === '') return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  };
  const propNumber = (key: string, fallback: unknown): number | null =>
    getProp(key) !== undefined ? parseNumber(getProp(key)) : parseNumber(fallback as number | string | null | undefined);
  const propBoolean = (key: string, fallback: boolean): boolean => {
    const value = getProp(key);
    return value !== undefined ? value.toLowerCase() === 'true' : fallback;
  };

  const lamp = detailed?.lamp ?? base.lamp;
  const wallPlug = detailed?.wallPlug ?? base.wallPlug;
  const doorSensor = detailed?.doorSensor ?? base.doorSensor;
  const thermostat = detailed?.thermostat ?? base.thermostat;
  const airQuality = detailed?.airQuality ?? base.airQuality;
  const deviceType = detailed?.deviceType ?? base.deviceType;
  const hasExtendedProperties = props.length > 0;

  let category: DeviceStatusCategory = 'unknown';
  if (lamp != null) category = 'lamp';
  else if (wallPlug != null) category = 'wallplug';
  else if (doorSensor != null) category = 'doorsensor';
  else if (thermostat != null) category = 'thermostat';
  else if (airQuality != null) category = 'airquality';
  else if (hasExtendedProperties) {
    // Device type numbers are stable in the generated API enum. These
    // fallbacks cover detailed responses whose sub-DTO is omitted.
    if ([1].includes(Number(deviceType)) && getProp('Presence') !== undefined) category = 'motionsensor';
    else if ([2, 13].includes(Number(deviceType)) && getProp('IsOpen') !== undefined) category = 'doorsensor';
    else if ([7, 10, 22].includes(Number(deviceType)) && getProp('TemperatureSetpoint') !== undefined) category = 'thermostat';
    else if ([15].includes(Number(deviceType)) && getProp('CO2') !== undefined) category = 'airquality';
    else if ([6, 11, 14, 16, 19].includes(Number(deviceType)) && getProp('IsOn') !== undefined) category = 'wallplug';
    else if ([8, 9, 12, 20].includes(Number(deviceType)) && getProp('IsOn') !== undefined) category = 'lamp';
  }

  return {
    category,
    isOn: propBoolean('IsOn', category === 'lamp'
      ? Number(lamp?.brightness ?? 0) > 0
      : wallPlug?.isOn === true),
    doorIsOpen: propBoolean('IsOpen', doorSensor?.state === 1),
    lastOpenDurationMinutes: propNumber('LastOpenDurationMinutes', null),
    lastStateChanged: getProp('LastStateChanged') ?? null,
    presence: propBoolean('Presence', false),
    brightness: propNumber('Brightness', lamp?.brightness),
    color: getProp('Color') ?? null,
    powerConsumption: propNumber('Meter_W', null),
    temperature: propNumber('Temperature', thermostat?.temperature ?? airQuality?.temperature),
    temperatureSetpoint: propNumber('TemperatureSetpoint', thermostat?.temperatureSetpoint),
    battery: propNumber('Battery', null),
    luminance: propNumber('Luminance', null),
    humidity: propNumber('RelativeHumidity', airQuality?.relativeHumidity),
    // Keep the existing devices page's GetAll fallback (`co2`) unchanged.
    co2: propNumber('CO2', airQuality?.co2),
    voc: propNumber('VOC', airQuality?.voc ?? airQuality?.volatileOrganicCompounds)
  };
}
