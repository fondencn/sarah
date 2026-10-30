import { DeviceDtoModel as DeviceDto } from '../services/api/device-service/model/models';
import { KnownDeviceTypesModel } from '../services/api/device-service/model/knownDeviceTypes';
import { AirQualityStateDtoModel } from '../services/api/device-service/model/airQualityStateDto';

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
  const props = detail?.extendedProperties ?? device.extendedProperties ?? [];
  const getProp = (key: string): string | undefined =>
    props.find(property => property.key === key)?.value ?? undefined;
  const parseNumber = (value: unknown): number | null => {
    if (value === undefined || value === null || value === '') return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  };
  const propNumber = (key: string, fallback: unknown): number | null =>
    getProp(key) !== undefined ? parseNumber(getProp(key)) : parseNumber(fallback);
  const propBoolean = (key: string, fallback: boolean): boolean => {
    const value = getProp(key);
    return value !== undefined ? value.toLowerCase() === 'true' : fallback;
  };

  const lamp = detail?.lamp ?? device.lamp;
  const wallPlug = detail?.wallPlug ?? device.wallPlug;
  const doorSensor = detail?.doorSensor ?? device.doorSensor;
  const thermostat = detail?.thermostat ?? device.thermostat;
  const airQuality = detail?.airQuality ?? device.airQuality;
  const deviceType = detail?.deviceType ?? device.deviceType;
  const hasExtendedProperties = props.length > 0;
  const legacyCo2 = (state: AirQualityStateDtoModel | null | undefined): unknown => {
    if (!state) return undefined;
    return 'co2' in state ? state.co2 : state.cO2;
  };
  const legacyTemperature = (state: unknown): number | undefined => {
    if (typeof state !== 'object' || state === null || !('temperature' in state)) return undefined;
    const temperature = state.temperature;
    return typeof temperature === 'number' && Number.isFinite(temperature) ? temperature : undefined;
  };

  let category: DeviceStatusCategory = 'unknown';
  if (lamp != null) category = 'lamp';
  else if (wallPlug != null) category = 'wallplug';
  else if (doorSensor != null) category = 'doorsensor';
  else if (thermostat != null) category = 'thermostat';
  else if (airQuality != null) category = 'airquality';
  else if (hasExtendedProperties) {
    // Some detailed responses omit the sub-DTO, so fall back to the API's
    // generated device type enum and known ExtendedProperty keys.
    switch (deviceType) {
      case KnownDeviceTypesModel.NUMBER_1:
        if (getProp('Presence') !== undefined) category = 'motionsensor';
        break;
      case KnownDeviceTypesModel.NUMBER_2:
      case KnownDeviceTypesModel.NUMBER_13:
        if (getProp('IsOpen') !== undefined) category = 'doorsensor';
        break;
      case KnownDeviceTypesModel.NUMBER_7:
      case KnownDeviceTypesModel.NUMBER_10:
      case KnownDeviceTypesModel.NUMBER_22:
        if (getProp('TemperatureSetpoint') !== undefined) category = 'thermostat';
        break;
      case KnownDeviceTypesModel.NUMBER_15:
        if (getProp('CO2') !== undefined) category = 'airquality';
        break;
      case KnownDeviceTypesModel.NUMBER_6:
      case KnownDeviceTypesModel.NUMBER_11:
      case KnownDeviceTypesModel.NUMBER_14:
      case KnownDeviceTypesModel.NUMBER_16:
      case KnownDeviceTypesModel.NUMBER_19:
        if (getProp('IsOn') !== undefined) category = 'wallplug';
        break;
      case KnownDeviceTypesModel.NUMBER_8:
      case KnownDeviceTypesModel.NUMBER_9:
      case KnownDeviceTypesModel.NUMBER_12:
      case KnownDeviceTypesModel.NUMBER_20:
        if (getProp('IsOn') !== undefined) category = 'lamp';
        break;
    }
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
    temperature: propNumber('Temperature', legacyTemperature(thermostat) ?? legacyTemperature(airQuality)),
    temperatureSetpoint: propNumber('TemperatureSetpoint', thermostat?.temperatureSetpoint),
    battery: propNumber('Battery', null),
    luminance: propNumber('Luminance', null),
    humidity: propNumber('RelativeHumidity', airQuality?.relativeHumidity),
    // Keep the existing devices page's GetAll fallback (`co2`) unchanged.
    co2: propNumber('CO2', legacyCo2(airQuality) ?? airQuality?.cO2),
    voc: propNumber('VOC', airQuality?.volatileOrganicCompounds)
  };
}
