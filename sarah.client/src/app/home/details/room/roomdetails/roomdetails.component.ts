import { Component, Input, OnInit } from '@angular/core';
import { RoomsClient } from '../../../../services/api/room-service/api/rooms.service';
import { RoomDtoModel as RoomDto } from '../../../../services/api/room-service/model/models';
import { RulesClient } from '../../../../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../../../../services/api/rules-service/model/alarmScheduleEntity';

@Component({
  selector: 'app-roomdetails',
  templateUrl: './roomdetails.component.html',
  styleUrls: ['./roomdetails.component.css']
})
export class RoomdetailsComponent implements OnInit {
  @Input() id: number = 0;
  roomDetails: RoomDto | null = null;
  lastUpdated: string = "";
  temperatureSchedules: AlarmScheduleEntityModel[] = [];

  constructor(private roomsService: RoomsClient, private rulesClient: RulesClient) {}

  ngOnInit(): void {
    this.loadRoomDetails();
    this.loadTemperatureSchedules();
  }

  loadRoomDetails(): void {
    if (this.id) {
      this.roomsService.apiRoomsIdGet(this.id).subscribe(room => {
        this.roomDetails = room;
        this.lastUpdated = new Date().toLocaleString('de-DE');
      });
    }
  }

  loadTemperatureSchedules(): void {
    if (!this.id) {
      this.temperatureSchedules = [];
      return;
    }

    this.rulesClient.apiRulesAlarmsGet().subscribe({
      next: schedules => {
        this.temperatureSchedules = schedules
          .map((schedule, originalIndex) => ({
            schedule,
            originalIndex,
            roomId: this.getRoomId(schedule),
            clockTime: this.getStoredClockMilliseconds(schedule)
          }))
          .filter(item => Number(item.schedule.contentType) === 1 && item.roomId === this.id)
          .sort((a, b) => {
            if (a.clockTime === null) return b.clockTime === null ? a.originalIndex - b.originalIndex : 1;
            if (b.clockTime === null) return -1;
            return a.clockTime - b.clockTime || a.originalIndex - b.originalIndex;
          })
          .map(item => item.schedule);
      },
      error: () => this.temperatureSchedules = []
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
      const recurrence = JSON.parse(schedule.serializedRecurrence) as { dtstart?: unknown };
      return typeof recurrence.dtstart === 'string' ? recurrence.dtstart : null;
    } catch {
      return null;
    }
  }
}
