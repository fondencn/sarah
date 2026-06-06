/// <reference types="jasmine" />

import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardItemViewModel, HomeComponent } from './home.component';
import { AuthService } from '../services/auth.service';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { PersonsClient } from '../services/api/persons-service/api/persons.service';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { StatusRuntimeService } from '../services/status-runtime.service';
import { LoggingService } from '../services/logging.service';
import { DashboardItemTypeDto } from '../models/api-types';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let devicesServiceSpy: jasmine.SpyObj<DevicesClient>;

  beforeEach(() => {
    devicesServiceSpy = jasmine.createSpyObj('DevicesClient', [
      'devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness',
      'devicesSetWallplugStateByDeviceIdPOSTApiDevicesWallplugIdStateIsOn',
      'devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature',
      'devicesSetLampColorPOSTApiDevicesLampIdColorColor',
      'devicesSetLampWarmWhitePOSTApiDevicesLampIdWarmwhite',
      'devicesSetLampColdWhitePOSTApiDevicesLampIdColdwhite'
    ]);

    devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness.and.returnValue(of({}) as any);
    devicesServiceSpy.devicesSetWallplugStateByDeviceIdPOSTApiDevicesWallplugIdStateIsOn.and.returnValue(of({}) as any);
    devicesServiceSpy.devicesSetThermostatTemperaturePOSTApiDevicesThermostatIdTemperatureTemperature.and.returnValue(of({}) as any);
    devicesServiceSpy.devicesSetLampColorPOSTApiDevicesLampIdColorColor.and.returnValue(of({}) as any);
    devicesServiceSpy.devicesSetLampWarmWhitePOSTApiDevicesLampIdWarmwhite.and.returnValue(of({}) as any);
    devicesServiceSpy.devicesSetLampColdWhitePOSTApiDevicesLampIdColdwhite.and.returnValue(of({}) as any);

    TestBed.configureTestingModule({
      providers: [
        HomeComponent,
        { provide: AuthService, useValue: jasmine.createSpyObj('AuthService', ['login', 'isLoggedIn'], { currentUserName: 'chris', currentUserDisplayName: 'Chris' }) },
        { provide: StatusRuntimeService, useValue: jasmine.createSpyObj('StatusRuntimeService', ['statusGet']) },
        { provide: DashboardRuntimeService, useValue: jasmine.createSpyObj('DashboardRuntimeService', ['apiDashboardGet']) },
        { provide: PersonsClient, useValue: jasmine.createSpyObj('PersonsClient', ['apiPersonsGet']) },
        { provide: DevicesClient, useValue: devicesServiceSpy },
        { provide: LoggingService, useValue: jasmine.createSpyObj('LoggingService', ['debug', 'info', 'warn', 'error']) }
      ]
    });

    component = TestBed.inject(HomeComponent);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('switchLampOn sends full brightness', () => {
    component.switchLampOn(42);

    expect(devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness)
      .toHaveBeenCalledWith(42, 255);
  });

  it('toggleLamp sends full brightness when checked', () => {
    const target = { checked: true } as HTMLInputElement;

    component.toggleLamp(7, target);

    expect(devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness)
      .toHaveBeenCalledWith(7, 255);
  });

  it('toggleLamp sends zero brightness when unchecked', () => {
    const target = { checked: false } as HTMLInputElement;

    component.toggleLamp(7, target);

    expect(devicesServiceSpy.devicesSetLampBrightnessPOSTApiDevicesLampIdBrightnessBrightness)
      .toHaveBeenCalledWith(7, 0);
  });

  it('DashboardItemViewModel reads motion sensor properties', () => {
    const vm = new DashboardItemViewModel({
      itemId: 11,
      itemType: DashboardItemTypeDto.NUMBER_0,
      title: 'Motion',
      subtype: 'MotionSensor',
      extendedProperties: [
        { key: 'Presence', value: 'True' },
        { key: 'Luminance', value: '43.5' },
        { key: 'Battery', value: '88' }
      ]
    });

    expect(vm.motionPresence).toBeTrue();
    expect(vm.motionLuminance).toBe(43.5);
    expect(vm.motionBattery).toBe(88);
  });

  it('DashboardItemViewModel motion getters handle missing/invalid values', () => {
    const vm = new DashboardItemViewModel({
      itemId: 12,
      itemType: DashboardItemTypeDto.NUMBER_0,
      title: 'Motion',
      subtype: 'MotionSensor',
      extendedProperties: [
        { key: 'Presence', value: 'False' },
        { key: 'Luminance', value: 'not-a-number' }
      ]
    });

    expect(vm.motionPresence).toBeFalse();
    expect(vm.motionLuminance).toBeNull();
    expect(vm.motionBattery).toBeNull();
  });
});
