import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, Observable, of, Subscription } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';

import { DeviceDtoModel as DeviceDto } from '../../services/api/device-service/model/models';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { AlarmScheduleEntityModel } from '../../services/api/rules-service/model/alarmScheduleEntity';
import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { RoomDtoModel as RoomDto } from '../../services/api/room-service/model/models';
import { RoomsClient } from '../../services/api/room-service/api/rooms.service';
import { DialogService } from '../../services/dialog.service';
import { LoggingService } from '../../services/logging.service';
import { AlarmContentTypeModel, AlarmEditModalComponent, AlarmRecurrence } from '../../alarms/alarm-edit-modal/alarm-edit-modal.component';
import { DeviceControlModalComponent } from '../../devices/device-control-modal/device-control-modal.component';
import { DeviceStatus, getDeviceStatus } from '../../devices/device-status';

interface RoomDeviceTile {
  device: DeviceDto;
  detail: DeviceDto | null;
  status: DeviceStatus;
  statusUnavailable: boolean;
}

@Component({
  selector: 'app-room-detail',
  templateUrl: './room-detail.component.html',
  styleUrls: ['./room-detail.component.css']
})
export class RoomDetailComponent implements OnInit, OnDestroy {
  @ViewChild(DeviceControlModalComponent) deviceControlModal!: DeviceControlModalComponent;
  @ViewChild(AlarmEditModalComponent) alarmEditModal!: AlarmEditModalComponent;

  room: RoomDto | null = null;
  roomId: number | null = null;
  devices: RoomDeviceTile[] = [];
  schedules: AlarmScheduleEntityModel[] = [];
  isLoading = true;
  errorMessage = '';
  scheduleError = '';

  private routeSubscription?: Subscription;
  private loadSubscription?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private roomsClient: RoomsClient,
    private devicesClient: DevicesClient,
    private rulesClient: RulesClient,
    private dialogService: DialogService,
    private logger: LoggingService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      this.roomId = Number.isInteger(id) && id > 0 ? id : null;
      this.loadRoomData();
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.loadSubscription?.unsubscribe();
  }

  loadRoomData(): void {
    if (this.roomId === null) {
      this.isLoading = false;
      this.errorMessage = this.translate.instant('roomDetail.errors.invalidRoomId');
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.scheduleError = '';
    this.loadSubscription?.unsubscribe();

    this.loadSubscription = forkJoin({
      room: this.roomsClient.apiRoomsIdGet(this.roomId),
      allDevices: this.devicesClient.devicesGetAllGETApiDevices(),
      alarms: this.rulesClient.apiRulesAlarmsGet()
    }).pipe(
      switchMap(({ room, allDevices, alarms }) => {
        this.room = room;
        const roomDevices = (Array.isArray(allDevices) ? allDevices : [])
          .filter((device: DeviceDto) => Number(device.roomId) === this.roomId);
        const deviceRequests: Observable<{ device: DeviceDto; detail: DeviceDto | null }>[] = roomDevices.map((device: DeviceDto) =>
          this.devicesClient.devicesGetDeviceByIdGETApiDevicesId(device.id as number).pipe(
            map((detail: DeviceDto) => ({ device, detail })),
            catchError(error => {
              this.logger.warn(`Unable to load status for device ${device.id}:`, error);
              return of({ device, detail: null });
            })
          )
        );

        const deviceDetails$ = deviceRequests.length > 0
          ? forkJoin(deviceRequests)
          : of<{ device: DeviceDto; detail: DeviceDto | null }[]>([]);

        return deviceDetails$.pipe(
          map(deviceDetails => ({
            devices: deviceDetails.map(({ device, detail }) => ({
              device,
              detail,
              status: getDeviceStatus(device, detail),
              statusUnavailable: detail === null
            })),
            schedules: (Array.isArray(alarms) ? alarms : [])
              .filter((alarm: AlarmScheduleEntityModel) => this.isScheduleForRoom(alarm, this.roomId as number))
          }))
        );
      })
    ).subscribe({
      next: result => {
        this.devices = result.devices;
        this.schedules = result.schedules;
      },
      error: error => {
        this.logger.error('Error loading room details:', error);
        this.errorMessage = this.translate.instant('roomDetail.errors.loadFailed');
        this.isLoading = false;
      },
      complete: () => this.isLoading = false
    });
  }

  private isScheduleForRoom(alarm: AlarmScheduleEntityModel, roomId: number): boolean {
    if (Number(alarm.contentType) !== AlarmContentTypeModel.TemperatureSchedule || !alarm.contentJson) return false;
    try {
      const content = JSON.parse(alarm.contentJson) as { roomId?: number | string };
      return Number(content.roomId) === roomId;
    } catch {
      return false;
    }
  }

  openDeviceControl(tile: RoomDeviceTile): void {
    this.deviceControlModal.show(tile.device.id as number, tile.device.name ?? '', tile.device.typeName ?? '');
  }

  onDeviceControlClosed(): void {
    this.refreshDeviceStatuses();
  }

  private refreshDeviceStatuses(): void {
    if (this.devices.length === 0) return;

    const refreshedDetails = this.devices.map(tile =>
      this.devicesClient.devicesGetDeviceByIdGETApiDevicesId(tile.device.id as number).pipe(
        map(detail => ({ tile, detail })),
        catchError(error => {
          this.logger.warn(`Unable to refresh status for device ${tile.device.id}:`, error);
          return of({ tile, detail: null });
        })
      )
    );

    forkJoin(refreshedDetails).subscribe(results => {
      results.forEach(({ tile, detail }) => {
        tile.detail = detail;
        tile.status = getDeviceStatus(tile.device, detail);
        tile.statusUnavailable = detail === null;
      });
      this.devices = [...this.devices];
    });
  }

  createSchedule(): void {
    if (this.roomId !== null) this.alarmEditModal.openTemperatureScheduleForRoom(this.roomId);
  }

  editSchedule(schedule: AlarmScheduleEntityModel): void {
    if (this.roomId !== null) this.alarmEditModal.openForEdit(schedule, this.roomId);
  }

  onScheduleSaved(): void {
    this.loadRoomData();
  }

  deleteSchedule(schedule: AlarmScheduleEntityModel): void {
    if (schedule.id == null) return;
    this.dialogService.showConfirmDialog(
      this.translate.instant('roomDetail.schedule.deleteConfirmation'),
      this.translate.instant('roomDetail.schedule.deleteTitle')
    )
      .then(confirmed => {
        if (!confirmed) return;
        this.rulesClient.apiRulesAlarmsIdDelete(schedule.id as number).subscribe({
          next: () => {
            this.schedules = this.schedules.filter(item => item.id !== schedule.id);
            this.scheduleError = '';
          },
          error: error => {
            this.logger.error('Error deleting temperature schedule:', error);
            this.scheduleError = this.translate.instant('roomDetail.errors.deleteScheduleFailed');
          }
        });
      })
      .catch(error => {
        this.logger.error('Error confirming schedule deletion:', error);
        this.scheduleError = this.translate.instant('roomDetail.errors.confirmationFailed');
      });
  }

  scheduleTemperature(schedule: AlarmScheduleEntityModel): number | null {
    try {
      const value = Number(JSON.parse(schedule.contentJson ?? '{}').targetTemperature);
      return Number.isFinite(value) ? value : null;
    } catch {
      return null;
    }
  }

  scheduleRepeatLabel(schedule: AlarmScheduleEntityModel): string {
    const time = schedule.alarmTime ? new Date(schedule.alarmTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
    let recurrence: AlarmRecurrence | null = null;
    try {
      recurrence = schedule.serializedRecurrence ? JSON.parse(schedule.serializedRecurrence) as AlarmRecurrence : null;
    } catch { /* show a generic recurrence label below */ }

    if (!recurrence) {
      const label = this.translate.instant('roomDetail.schedule.recurrence.generic');
      return `${label}${time ? ` · ${time}` : ''}`;
    }
    const frequency: Record<string, string> = {
      daily: 'daily',
      weekly: 'weekly',
      monthly: 'monthly',
      yearly: 'yearly'
    };
    const days: Record<string, string> = {
      mo: 'monday', tu: 'tuesday', we: 'wednesday', th: 'thursday',
      fr: 'friday', sa: 'saturday', su: 'sunday'
    };
    const recurrenceKey = frequency[recurrence.freq];
    const repeatLabel = recurrenceKey
      ? this.translate.instant(`roomDetail.schedule.recurrence.${recurrenceKey}`)
      : this.translate.instant('roomDetail.schedule.recurrence.generic');
    const weekdays = recurrence.byweekday?.map(day => days[day])
      .filter((day): day is string => !!day)
      .map(day => this.translate.instant(`roomDetail.schedule.weekdays.${day}`));
    const repeat = `${repeatLabel}${weekdays?.length ? ` · ${weekdays.join(', ')}` : ''}`;
    return `${repeat}${time ? ` · ${time}` : ''}`;
  }

  goBack(): void {
    this.router.navigate(['/rooms']);
  }
}
