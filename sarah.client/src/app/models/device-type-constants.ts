import { KnownDeviceTypes } from '../services/api-client';

export const DEVICE_TYPE_UNKNOWN                      = KnownDeviceTypes.NUMBER_0;  // 0
export const DEVICE_TYPE_FIBARO_MOTION_SENSOR         = KnownDeviceTypes.NUMBER_1;  // 1
export const DEVICE_TYPE_AEOTEC_DOOR_SENSOR           = KnownDeviceTypes.NUMBER_2;  // 2
export const DEVICE_TYPE_AEOTEC_Z_STICK               = KnownDeviceTypes.NUMBER_3;  // 3
export const DEVICE_TYPE_FIBARO_THE_BUTTON            = KnownDeviceTypes.NUMBER_4;  // 4
export const DEVICE_TYPE_POPP_WALL_CONTROLLER         = KnownDeviceTypes.NUMBER_5;  // 5
export const DEVICE_TYPE_POPP_WALL_PLUG               = KnownDeviceTypes.NUMBER_6;  // 6
export const DEVICE_TYPE_FIBARO_HEAT_CONTROLLER       = KnownDeviceTypes.NUMBER_7;  // 7
export const DEVICE_TYPE_AEOTEC_LED_BULB              = KnownDeviceTypes.NUMBER_8;  // 8
export const DEVICE_TYPE_FIBARO_RGBW_CONTROLLER_2     = KnownDeviceTypes.NUMBER_9;  // 9
export const DEVICE_TYPE_AEOTEC_THERMOSTAT            = KnownDeviceTypes.NUMBER_10; // 10
export const DEVICE_TYPE_AEOTEC_SMART_SWITCH_7        = KnownDeviceTypes.NUMBER_11; // 11
export const DEVICE_TYPE_AEOTEC_LED_BULB_6_WHITE      = KnownDeviceTypes.NUMBER_12; // 12
export const DEVICE_TYPE_FIBARO_DOOR_WINDOW_SENSOR_2  = KnownDeviceTypes.NUMBER_13; // 13
export const DEVICE_TYPE_FIBARO_WALL_PLUG             = KnownDeviceTypes.NUMBER_14; // 14
export const DEVICE_TYPE_EUTRONIC_AIR_QUALITY_SENSOR  = KnownDeviceTypes.NUMBER_15; // 15
export const DEVICE_TYPE_FIBARO_WALLI_SWITCH          = KnownDeviceTypes.NUMBER_16; // 16
export const DEVICE_TYPE_FIBARO_SMOKE_SENSOR          = KnownDeviceTypes.NUMBER_17; // 17
export const DEVICE_TYPE_FIBARO_KEY_FOB               = KnownDeviceTypes.NUMBER_18; // 18
export const DEVICE_TYPE_WIFI_WALL_PLUG               = KnownDeviceTypes.NUMBER_19; // 19
export const DEVICE_TYPE_SHELLY_WIFI_LAMP             = KnownDeviceTypes.NUMBER_20; // 20
export const DEVICE_TYPE_LORA_WAN_GPS_TRACKER         = KnownDeviceTypes.NUMBER_21; // 21

export const DEVICE_TYPE_LABELS: Record<number, string> = {
  [DEVICE_TYPE_UNKNOWN as number]:                     'Unknown',
  [DEVICE_TYPE_FIBARO_MOTION_SENSOR as number]:        'Fibaro Motion Sensor',
  [DEVICE_TYPE_AEOTEC_DOOR_SENSOR as number]:          'Aeotec Door Sensor',
  [DEVICE_TYPE_AEOTEC_Z_STICK as number]:              'Aeotec Z-Stick',
  [DEVICE_TYPE_FIBARO_THE_BUTTON as number]:           'Fibaro The Button',
  [DEVICE_TYPE_POPP_WALL_CONTROLLER as number]:        'Popp Wall Controller',
  [DEVICE_TYPE_POPP_WALL_PLUG as number]:              'Popp Wall Plug',
  [DEVICE_TYPE_FIBARO_HEAT_CONTROLLER as number]:      'Fibaro Heat Controller',
  [DEVICE_TYPE_AEOTEC_LED_BULB as number]:             'Aeotec LED Bulb',
  [DEVICE_TYPE_FIBARO_RGBW_CONTROLLER_2 as number]:    'Fibaro RGBW Controller 2',
  [DEVICE_TYPE_AEOTEC_THERMOSTAT as number]:           'Aeotec Thermostat',
  [DEVICE_TYPE_AEOTEC_SMART_SWITCH_7 as number]:       'Aeotec Smart Switch 7',
  [DEVICE_TYPE_AEOTEC_LED_BULB_6_WHITE as number]:     'Aeotec LED Bulb 6 White',
  [DEVICE_TYPE_FIBARO_DOOR_WINDOW_SENSOR_2 as number]: 'Fibaro Door/Window Sensor 2',
  [DEVICE_TYPE_FIBARO_WALL_PLUG as number]:            'Fibaro Wall Plug',
  [DEVICE_TYPE_EUTRONIC_AIR_QUALITY_SENSOR as number]: 'Eutronic Air Quality Sensor',
  [DEVICE_TYPE_FIBARO_WALLI_SWITCH as number]:         'Fibaro Walli Switch',
  [DEVICE_TYPE_FIBARO_SMOKE_SENSOR as number]:         'Fibaro Smoke Sensor',
  [DEVICE_TYPE_FIBARO_KEY_FOB as number]:              'Fibaro Key Fob',
  [DEVICE_TYPE_WIFI_WALL_PLUG as number]:              'WiFi Wall Plug',
  [DEVICE_TYPE_SHELLY_WIFI_LAMP as number]:            'Shelly WiFi Lamp',
  [DEVICE_TYPE_LORA_WAN_GPS_TRACKER as number]:        'LoRaWAN GPS Tracker',
};
