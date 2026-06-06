import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DeviceControlModalComponent } from './device-control-modal.component';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { LoggingService } from '../../services/logging.service';

describe('DeviceControlModalComponent', () => {
  let component: DeviceControlModalComponent;
  let devicesServiceSpy: jasmine.SpyObj<DevicesClient>;
  let loggerSpy: jasmine.SpyObj<LoggingService>;

  beforeEach(() => {
    devicesServiceSpy = jasmine.createSpyObj('DevicesClient', [
      'devicesGetDeviceByIdGETApiDevicesId',
      'devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness',
      'devicesSetLampColorPOSTApiDevicesLampIdColorColor',
      'devicesSetLampWarmWhitePOSTApiDevicesLampIdWarmwhite',
      'devicesSetLampColdWhitePOSTApiDevicesLampIdColdwhite',
      'devicesSetWallplugStateByDeviceIdPOSTApiDevicesWallplugIdStateIsOn',
      'devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature'
    ]);
    loggerSpy = jasmine.createSpyObj('LoggingService', ['debug', 'info', 'warn', 'error']);

    TestBed.configureTestingModule({
      providers: [
        DeviceControlModalComponent,
        { provide: DevicesClient, useValue: devicesServiceSpy },
        { provide: LoggingService, useValue: loggerSpy }
      ]
    });

    component = TestBed.inject(DeviceControlModalComponent);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('updateStateFromDevice (via loadState path)', () => {
    it('parses lamp state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'IsOn', value: 'True' },
          { key: 'Color', value: '#ff0000' },
          { key: 'Brightness', value: '80' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'Lampe';
      component.deviceId = 1;
      (component as any).loadState();

      expect(component.isOn).toBeTrue();
      expect(component.lampColor).toBe('#ff0000');
      expect(component.lampBrightness).toBe(80);
    });

    it('parses wallplug state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'IsOn', value: 'True' },
          { key: 'Meter_W', value: '42.5' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'Steckdose';
      component.deviceId = 2;
      (component as any).loadState();

      expect(component.isOn).toBeTrue();
      expect(component.powerConsumption).toBe(42.5);
    });

    it('parses thermostat state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'Temperature', value: '21.5' },
          { key: 'TemperatureSetpoint', value: '22.0' },
          { key: 'Battery', value: '85' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'Heizung';
      component.deviceId = 3;
      (component as any).loadState();

      expect(component.thermostatTemperature).toBe(21.5);
      expect(component.thermostatSetpoint).toBe(22.0);
      expect(component.thermostatBattery).toBe(85);
    });

    it('parses door sensor state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'IsOpen', value: 'True' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'DoorSensor';
      component.deviceId = 4;
      (component as any).loadState();

      expect(component.doorIsOpen).toBeTrue();
    });

    it('parses air quality sensor state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'Temperature', value: '23.1' },
          { key: 'RelativeHumidity', value: '55.2' },
          { key: 'CO2', value: '800' },
          { key: 'VOC', value: '150' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'EutronicAirQualitySensor';
      component.deviceId = 5;
      (component as any).loadState();

      expect(component.airTemperature).toBe(23.1);
      expect(component.airHumidity).toBe(55.2);
      expect(component.airCO2).toBe(800);
      expect(component.airVOC).toBe(150);
    });

    it('parses motion sensor state from extendedProperties', () => {
      const dev = {
        extendedProperties: [
          { key: 'Presence', value: 'True' },
          { key: 'Luminance', value: '37.5' },
          { key: 'Battery', value: '78' },
          { key: 'Temperature', value: '22.4' }
        ]
      };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'MotionSensor';
      component.deviceId = 8;
      (component as any).loadState();

      expect(component.motionPresence).toBeTrue();
      expect(component.motionLuminance).toBe(37.5);
      expect(component.motionBattery).toBe(78);
      expect(component.airTemperature).toBe(22.4);
    });

    it('returns null for missing extendedProperties values', () => {
      const dev = { extendedProperties: [] };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'Heizung';
      component.deviceId = 6;
      (component as any).loadState();

      expect(component.thermostatTemperature).toBeNull();
      expect(component.thermostatSetpoint).toBeNull();
      expect(component.thermostatBattery).toBeNull();
    });

    it('handles missing motion sensor values', () => {
      const dev = { extendedProperties: [] };
      devicesServiceSpy.devicesGetDeviceByIdGETApiDevicesId.and.returnValue(of(dev) as any);

      component.typeName = 'MotionSensor';
      component.deviceId = 9;
      (component as any).loadState();

      expect(component.motionPresence).toBeFalse();
      expect(component.motionLuminance).toBeNull();
      expect(component.motionBattery).toBeNull();
    });
  });

  describe('setThermostatTemperatureValue()', () => {
    it('calls devicesService with the correct temperature', () => {
      devicesServiceSpy.devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature.and.returnValue(of(null) as any);
      component.setThermostatTemperatureValue(7, 21.5);
      expect(devicesServiceSpy.devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature).toHaveBeenCalledWith(7, 21.5);
    });

    it('does nothing when itemId is undefined', () => {
      component.setThermostatTemperatureValue(undefined, 21.5);
      expect(devicesServiceSpy.devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature).not.toHaveBeenCalled();
    });
  });

  describe('toggleLamp()', () => {
    it('sends full brightness when turned on', () => {
      devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness.and.returnValue(of(null) as any);
      component.deviceId = 11;

      component.toggleLamp({ checked: true } as HTMLInputElement);

      expect(devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness)
        .toHaveBeenCalledWith(11, 255);
      expect(component.isOn).toBeTrue();
      expect(component.lampBrightness).toBe(255);
    });

    it('sends zero brightness when turned off', () => {
      devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness.and.returnValue(of(null) as any);
      component.deviceId = 11;

      component.toggleLamp({ checked: false } as HTMLInputElement);

      expect(devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness)
        .toHaveBeenCalledWith(11, 0);
      expect(component.isOn).toBeFalse();
      expect(component.lampBrightness).toBe(0);
    });
  });

  describe('ngOnDestroy()', () => {
    it('clears the refresh interval on destroy', () => {
      (component as any).refreshInterval = setInterval(() => {}, 99999);
      component.ngOnDestroy();
      expect((component as any).refreshInterval).toBeNull();
    });
  });
});
