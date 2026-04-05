import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DevicesComponent } from './devices.component';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { DialogService } from '../services/dialog.service';
import { LoggingService } from '../services/logging.service';
import { EventEmitter } from '@angular/core';
import { DashboardItemType } from '../models/api-types';

describe('DevicesComponent', () => {
  let component: DevicesComponent;
  let devicesServiceSpy: jasmine.SpyObj<DevicesClient>;
  let dashboardServiceSpy: jasmine.SpyObj<DashboardRuntimeService>;
  let dialogServiceSpy: jasmine.SpyObj<DialogService>;
  let loggerSpy: jasmine.SpyObj<LoggingService>;

  const mockDevices = [
    { id: 1, name: 'Lamp', isFavourite: false },
    { id: 2, name: 'Thermostat', isFavourite: false },
    { id: 3, name: 'Sensor', isFavourite: false }
  ] as any[];

  const mockDashboardItems = [
    { itemId: 1, itemType: DashboardItemType.NUMBER_0, title: 'Lamp' },
    { itemId: 10, itemType: DashboardItemType.NUMBER_3, title: 'Person A' }
  ] as any[];

  beforeEach(() => {
    devicesServiceSpy = jasmine.createSpyObj('DevicesClient', ['devicesGetAllGETApiDevices']);
    dashboardServiceSpy = jasmine.createSpyObj('DashboardRuntimeService', [
      'apiDashboardGet', 'apiDashboardPost', 'apiDashboardItemIdItemTypeDelete'
    ]);
    dialogServiceSpy = jasmine.createSpyObj('DialogService', ['showDialog', 'showConfirmDialog']);
    (dialogServiceSpy as any).dialogClosed = new EventEmitter();
    loggerSpy = jasmine.createSpyObj('LoggingService', ['debug', 'info', 'warn', 'error']);

    devicesServiceSpy.devicesGetAllGETApiDevices.and.returnValue(of(mockDevices) as any);
    dashboardServiceSpy.apiDashboardGet.and.returnValue(of(mockDashboardItems));

    TestBed.configureTestingModule({
      providers: [
        DevicesComponent,
        { provide: DevicesClient, useValue: devicesServiceSpy },
        { provide: DashboardRuntimeService, useValue: dashboardServiceSpy },
        { provide: DialogService, useValue: dialogServiceSpy },
        { provide: LoggingService, useValue: loggerSpy }
      ]
    });

    component = TestBed.inject(DevicesComponent);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('retrieveDevices()', () => {
    it('marks devices pinned to the dashboard as isFavourite=true', () => {
      component.retrieveDevices();

      expect(component.devices.find(d => d.id === 1)?.isFavourite).toBeTrue();
    });

    it('leaves devices not on the dashboard as isFavourite=false', () => {
      component.retrieveDevices();

      expect(component.devices.find(d => d.id === 2)?.isFavourite).toBeFalse();
      expect(component.devices.find(d => d.id === 3)?.isFavourite).toBeFalse();
    });

    it('ignores dashboard items of other types (persons, rooms, etc.)', () => {
      component.retrieveDevices();

      // itemId=10 is a Person (type NUMBER_3), must not mark any device as favourite
      expect(component.devices.some(d => d.id === 10)).toBeFalse();
    });

    it('sets isLoading to false after loading', () => {
      component.retrieveDevices();
      expect(component.isLoading).toBeFalse();
    });
  });
});
