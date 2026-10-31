import { convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RoomDetailComponent } from './room-detail.component';
import { AlarmContentTypeModel } from '../../alarms/alarm-edit-modal/alarm-edit-modal.component';

describe('RoomDetailComponent', () => {
  let component: RoomDetailComponent;
  let roomsClient: any;
  let devicesClient: any;
  let rulesClient: any;
  let dialogService: any;
  let logger: any;
  let translate: any;
  let modal: any;

  const room = { id: 7, name: 'Kitchen' } as any;
  const devices = [
    { id: 10, roomId: 7, name: 'Heater', typeName: 'Heizung', deviceType: 10, thermostat: {} },
    { id: 11, roomId: 7, name: 'Lamp', typeName: 'Lampe', deviceType: 8, lamp: {} },
    { id: 12, roomId: 8, name: 'Other room device', deviceType: 8, lamp: {} }
  ] as any[];
  const alarms = [
    {
      id: 1,
      contentType: AlarmContentTypeModel.TemperatureSchedule,
      contentJson: JSON.stringify({ roomId: 7, targetTemperature: 21 }),
      alarmTime: '2026-10-30T06:30:00Z',
      hasRecurrence: true,
      serializedRecurrence: JSON.stringify({ freq: 'weekly', byweekday: ['mo', 'fr'] }),
      isActive: true
    },
    { id: 2, contentType: AlarmContentTypeModel.TemperatureSchedule, contentJson: JSON.stringify({ roomId: 8 }) },
    { id: 3, contentType: AlarmContentTypeModel.Text, contentJson: JSON.stringify({ roomId: 7 }) },
    { id: 4, contentType: AlarmContentTypeModel.TemperatureSchedule, contentJson: '{bad json' }
  ] as any[];

  beforeEach(() => {
    roomsClient = jasmine.createSpyObj('RoomsClient', ['apiRoomsIdGet']);
    devicesClient = jasmine.createSpyObj('DevicesClient', [
      'devicesGetAllGETApiDevices',
      'devicesGetDeviceByIdGETApiDevicesId'
    ]);
    rulesClient = jasmine.createSpyObj('RulesClient', ['apiRulesAlarmsGet', 'apiRulesAlarmsIdDelete']);
    dialogService = jasmine.createSpyObj('DialogService', ['showConfirmDialog']);
    logger = jasmine.createSpyObj('LoggingService', ['debug', 'info', 'warn', 'error']);
    translate = {
      instant: jasmine.createSpy('instant').and.callFake((key: string) => ({
        'roomDetail.schedule.recurrence.weekly': 'Wöchentlich',
        'roomDetail.schedule.weekdays.monday': 'Mo',
        'roomDetail.schedule.weekdays.friday': 'Fr',
        'roomDetail.schedule.recurrence.generic': 'Wiederkehrend'
      }[key] ?? key))
    };
    modal = { show: jasmine.createSpy('show') };

    roomsClient.apiRoomsIdGet.and.returnValue(of(room));
    devicesClient.devicesGetAllGETApiDevices.and.returnValue(of(devices));
    devicesClient.devicesGetDeviceByIdGETApiDevicesId.and.callFake((id: number) => {
      if (id === 11) return throwError(() => new Error('device detail unavailable'));
      return of({
        extendedProperties: [
          { key: 'Temperature', value: '19.5' },
          { key: 'TemperatureSetpoint', value: '21' },
          { key: 'Battery', value: '88' }
        ]
      } as any);
    });
    rulesClient.apiRulesAlarmsGet.and.returnValue(of(alarms));
    rulesClient.apiRulesAlarmsIdDelete.and.returnValue(of(undefined));

    const route = { paramMap: of(convertToParamMap({ id: '7' })) } as any;
    component = new RoomDetailComponent(
      route,
      { navigate: jasmine.createSpy('navigate') } as any,
      roomsClient,
      devicesClient,
      rulesClient,
      dialogService,
      logger,
      translate as any
    );
    component.deviceControlModal = modal;
    component.alarmEditModal = {
      openTemperatureScheduleForRoom: jasmine.createSpy('openTemperatureScheduleForRoom'),
      openForEdit: jasmine.createSpy('openForEdit')
    } as any;
  });

  it('loads the route room title and only devices and temperature schedules for that room', () => {
    component.ngOnInit();

    expect(component.room?.name).toBe('Kitchen');
    expect(roomsClient.apiRoomsIdGet).toHaveBeenCalledWith(7);
    expect(devicesClient.devicesGetDeviceByIdGETApiDevicesId.calls.allArgs().map((args: any[]) => args[0])).toEqual([10, 11]);
    expect(component.devices.length).toBe(2);
    expect(component.devices[0].status.temperature).toBe(19.5);
    expect(component.devices[0].status.temperatureSetpoint).toBe(21);
    expect(component.devices[1].statusUnavailable).toBeTrue();
    expect(component.schedules.map(schedule => schedule.id)).toEqual([1]);
    expect(component.isLoading).toBeFalse();
  });

  it('opens the shared control modal for the selected device', () => {
    component.ngOnInit();
    component.openDeviceControl(component.devices[0]);
    expect(modal.show).toHaveBeenCalledWith(10, 'Heater', 'Heizung');
  });

  it('refreshes only existing device detail requests after control closes, preserving per-device errors', () => {
    component.ngOnInit();
    const tileRef = component.devices[0];
    roomsClient.apiRoomsIdGet.calls.reset();
    devicesClient.devicesGetAllGETApiDevices.calls.reset();
    rulesClient.apiRulesAlarmsGet.calls.reset();
    devicesClient.devicesGetDeviceByIdGETApiDevicesId.calls.reset();
    devicesClient.devicesGetDeviceByIdGETApiDevicesId.and.callFake((id: number) => {
      if (id === 11) return throwError(() => new Error('device unavailable'));
      return of({
        extendedProperties: [
          { key: 'Temperature', value: '20.5' },
          { key: 'TemperatureSetpoint', value: '22' }
        ]
      } as any);
    });

    component.onDeviceControlClosed();

    expect(roomsClient.apiRoomsIdGet).not.toHaveBeenCalled();
    expect(devicesClient.devicesGetAllGETApiDevices).not.toHaveBeenCalled();
    expect(rulesClient.apiRulesAlarmsGet).not.toHaveBeenCalled();
    expect(devicesClient.devicesGetDeviceByIdGETApiDevicesId.calls.allArgs().map((args: any[]) => args[0])).toEqual([10, 11]);
    expect(component.devices[0]).toBe(tileRef);
    expect(tileRef.status.temperature).toBe(20.5);
    expect(component.devices[1].statusUnavailable).toBeTrue();
  });

  it('performs the full page reload after a schedule is saved', () => {
    component.ngOnInit();
    roomsClient.apiRoomsIdGet.calls.reset();
    devicesClient.devicesGetAllGETApiDevices.calls.reset();
    rulesClient.apiRulesAlarmsGet.calls.reset();

    component.onScheduleSaved();

    expect(roomsClient.apiRoomsIdGet).toHaveBeenCalledWith(7);
    expect(devicesClient.devicesGetAllGETApiDevices).toHaveBeenCalled();
    expect(rulesClient.apiRulesAlarmsGet).toHaveBeenCalled();
  });

  it('opens create and edit schedule forms with the room locked to this room', () => {
    component.ngOnInit();
    component.createSchedule();
    expect(component.alarmEditModal.openTemperatureScheduleForRoom).toHaveBeenCalledWith(7);
    component.editSchedule(alarms[0]);
    expect(component.alarmEditModal.openForEdit).toHaveBeenCalledWith(alarms[0], 7);
  });

  it('supports empty device and schedule states', () => {
    devicesClient.devicesGetAllGETApiDevices.and.returnValue(of([{ ...devices[2] }]));
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([]));
    component.ngOnInit();

    expect(component.devices).toEqual([]);
    expect(component.schedules).toEqual([]);
    expect(component.isLoading).toBeFalse();
  });

  it('confirms before deleting a schedule and leaves it unchanged when cancelled', async () => {
    component.ngOnInit();
    dialogService.showConfirmDialog.and.returnValue(Promise.resolve(false));
    component.deleteSchedule(alarms[0]);
    await Promise.resolve();
    expect(rulesClient.apiRulesAlarmsIdDelete).not.toHaveBeenCalled();
    expect(component.schedules.length).toBe(1);
  });

  it('removes a schedule immediately after confirmed deletion', async () => {
    component.ngOnInit();
    dialogService.showConfirmDialog.and.returnValue(Promise.resolve(true));
    component.deleteSchedule(alarms[0]);
    await Promise.resolve();
    expect(rulesClient.apiRulesAlarmsIdDelete).toHaveBeenCalledWith(1);
    expect(component.schedules).toEqual([]);
  });

  it('shows a page-level error when room data fails to load', () => {
    roomsClient.apiRoomsIdGet.and.returnValue(throwError(() => new Error('not found')));
    component.ngOnInit();
    expect(component.errorMessage).toBe('roomDetail.errors.loadFailed');
    expect(component.isLoading).toBeFalse();
  });

  it('formats a schedule temperature and weekday recurrence for the list', () => {
    expect(component.scheduleTemperature(alarms[0])).toBe(21);
    expect(component.scheduleRepeatLabel(alarms[0])).toContain('Wöchentlich · Mo, Fr');
  });
});
