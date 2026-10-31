import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { Subscription } from 'rxjs';
import { RoomsClient } from '../../../../services/api/room-service/api/rooms.service';
import { RoomDtoModel as RoomDto } from '../../../../services/api/room-service/model/models';
import { RulesClient } from '../../../../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../../../../services/api/rules-service/model/alarmScheduleEntity';

@Component({
  selector: 'app-roomdetails',
  templateUrl: './roomdetails.component.html',
  styleUrls: ['./roomdetails.component.css']
})
export class RoomdetailsComponent implements OnChanges {
  @Input() id: number = 0;
  roomDetails: RoomDto | null = null;
  lastUpdated: string = "";
  temperatureSchedules: AlarmScheduleEntityModel[] = [];
  temperatureSchedulesLoading = false;
  temperatureSchedulesError = false;

  constructor(private roomsService: RoomsClient, private rulesClient: RulesClient) {}

  private roomDetailsRequest?: Subscription;
  private temperatureSchedulesRequest?: Subscription;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['id']) return;

    this.roomDetailsRequest?.unsubscribe();
    this.temperatureSchedulesRequest?.unsubscribe();
    this.roomDetails = null;
    this.lastUpdated = "";
    this.temperatureSchedules = [];
    this.temperatureSchedulesLoading = false;
    this.temperatureSchedulesError = false;
    this.loadRoomDetails();
    this.loadTemperatureSchedules();
  }

  loadRoomDetails(): void {
    const roomId = this.id;
    if (roomId) {
      this.roomDetailsRequest?.unsubscribe();
      this.roomDetailsRequest = this.roomsService.apiRoomsIdGet(roomId).subscribe(room => {
        if (this.id !== roomId) return;
        this.roomDetails = room;
        this.lastUpdated = new Date().toLocaleString('de-DE');
      });
    }
  }

  loadTemperatureSchedules(): void {
    const roomId = this.id;
    if (!roomId) {
      this.temperatureSchedules = [];
      return;
    }

    this.temperatureSchedulesRequest?.unsubscribe();
    this.temperatureSchedulesLoading = true;
    this.temperatureSchedulesError = false;
    this.temperatureSchedulesRequest = this.rulesClient.apiRulesAlarmsGet().subscribe({
      next: schedules => {
        if (this.id !== roomId) return;
        this.temperatureSchedules = schedules
          .map((schedule, originalIndex) => ({
            schedule,
            originalIndex,
            roomId: this.getRoomId(schedule),
            clockTime: this.getStoredClockMilliseconds(schedule)
          }))
          .filter(item => Number(item.schedule.contentType) === 1 && item.roomId === roomId)
          .sort((a, b) => {
            if (a.clockTime === null) return b.clockTime === null ? a.originalIndex - b.originalIndex : 1;
            if (b.clockTime === null) return -1;
            return a.clockTime - b.clockTime || a.originalIndex - b.originalIndex;
          })
          .map(item => item.schedule);
        this.temperatureSchedulesLoading = false;
      },
      error: () => {
        if (this.id !== roomId) return;
        this.temperatureSchedules = [];
        this.temperatureSchedulesLoading = false;
        this.temperatureSchedulesError = true;
      }
    });
  }

  getScheduleTime(schedule: AlarmScheduleEntityModel): string {
    const value = this.getDtstart(schedule);
    const match = value?.match(/^\d{4}-\d{2}-\d{2}T(\d{2}):(\d{2})/);
    return this.getStoredClockMilliseconds(schedule) !== null && match
      ? `${match[1]}:${match[2]}`
      : "Ungültige Uhrzeit";
  }

  private getRoomId(schedule: AlarmScheduleEntityModel): number | null {
    if (schedule.contentJson) {
      try {
        const content = JSON.parse(schedule.contentJson) as { roomId?: number | null };
        if (content.roomId != null) return Number(content.roomId);
      } catch {
        // Fall back to the structured content returned by the API.
      }
    }

    return schedule.content?.roomId == null ? null : Number(schedule.content.roomId);
  }

  private getStoredClockMilliseconds(schedule: AlarmScheduleEntityModel): number | null {
    const value = this.getDtstart(schedule);
    const match = value?.match(/^\d{4}-\d{2}-\d{2}T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d+))?)?(?:Z|[+-]\d{2}:?\d{2})?$/);
    if (!match) return null;

    const hours = Number(match[1]);
    const minutes = Number(match[2]);
    const seconds = Number(match[3] ?? 0);
    if (hours > 23 || minutes > 59 || seconds > 59) return null;

    const fraction = match[4] ? Number(`0.${match[4]}`) * 1000 : 0;
    return hours * 3600000 + minutes * 60000 + seconds * 1000 + fraction;
  }

  private getDtstart(schedule: AlarmScheduleEntityModel): string | null {
    if (!schedule.serializedRecurrence) return null;
    try {
      const recurrence = JSON.parse(schedule.serializedRecurrence) as Record<string, unknown>;
      let dtstart: unknown;
      for (const [name, value] of Object.entries(recurrence)) {
        if (name.toLowerCase() === 'dtstart') dtstart = value;
      }
      return typeof dtstart === 'string' ? dtstart : null;
    } catch {
      return null;
    }
  }
}
