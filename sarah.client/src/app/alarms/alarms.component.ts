import { AfterViewInit, Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions, EventInput, DateSelectArg, EventClickArg, EventDropArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import rrulePlugin from '@fullcalendar/rrule';
import bootstrap5Plugin from '@fullcalendar/bootstrap5';
import deLocale from '@fullcalendar/core/locales/de';

import { RulesClient } from '../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../services/api/rules-service/model/alarmScheduleEntity';
import { AlarmContentTypeModel, AlarmEditModalComponent, AlarmRecurrence } from './alarm-edit-modal/alarm-edit-modal.component';
import { RoomsClient } from '../services/api/room-service/api/rooms.service';

@Component({
  selector: 'app-alarms',
  templateUrl: './alarms.component.html',
  styleUrls: ['./alarms.component.css']
})
export class AlarmsComponent implements OnInit, AfterViewInit {
  @ViewChild(AlarmEditModalComponent) editModal!: AlarmEditModalComponent;
  @ViewChild(FullCalendarComponent) fullCalendar?: FullCalendarComponent;

  isLoading = false;
  private readonly roomNames = new Map<number, string>();

  calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin, rrulePlugin, bootstrap5Plugin],
    themeSystem: 'bootstrap5',
    locale: deLocale,
    buttonText: { prev: '‹', next: '›' },
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    initialView: 'dayGridMonth',
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    weekends: true,
    editable: true,
    handleWindowResize: true,
    height: 'auto',
    select: this.handleDateSelect.bind(this),
    eventClick: this.handleEventClick.bind(this),
    eventDrop: this.handleEventDrop.bind(this),
    events: []
  };

  constructor(private rulesClient: RulesClient, private roomsClient: RoomsClient) {}

  ngOnInit(): void {
    this.roomsClient.apiRoomsGet().subscribe({
      next: rooms => {
        rooms.forEach(room => {
          if (room.id != null) this.roomNames.set(Number(room.id), room.name ?? 'Raum');
        });
        this.loadAlarms();
      },
      error: () => this.loadAlarms()
    });
  }

  ngAfterViewInit(): void {
    this.scheduleCalendarResize();
  }

  loadAlarms(): void {
    this.isLoading = true;
    this.rulesClient.apiRulesAlarmsGet().subscribe({
      next: (alarms: AlarmScheduleEntityModel[]) => {
        const events = alarms.map(a => this.alarmToEvent(a));
        this.calendarOptions = { ...this.calendarOptions, events };
        this.isLoading = false;
        this.scheduleCalendarResize();
      },
      error: (err) => {
        console.error('Error loading alarms:', err);
        this.isLoading = false;
        this.scheduleCalendarResize();
      }
    });
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    this.scheduleCalendarResize();
  }

  private alarmToEvent(alarm: AlarmScheduleEntityModel): EventInput {
    const isTemperature = Number(alarm.contentType) === AlarmContentTypeModel.TemperatureSchedule;
    const color = !alarm.isActive ? '#6c757d' : (isTemperature ? '#198754' : '#0d6efd');
    let title = alarm.text;
    if (isTemperature && alarm.contentJson) {
      try {
        const content = JSON.parse(alarm.contentJson) as { roomId?: number; targetTemperature?: number };
        const roomName = content.roomId != null ? this.roomNames.get(content.roomId) : null;
        title = `${roomName ?? 'Raum'} · ${content.targetTemperature ?? ''} °C`;
      } catch { /* show the server-provided display text */ }
    }
    const base: EventInput = {
      id: alarm.id?.toString(),
      title,
      backgroundColor: color,
      borderColor: color,
      extendedProps: { alarm }
    };

    if (alarm.hasRecurrence && alarm.serializedRecurrence) {
      try {
        const rruleConfig = JSON.parse(alarm.serializedRecurrence);
        return { ...base, rrule: rruleConfig, duration: '01:00' };
      } catch {
        // fall through to one-time event
      }
    }

    return { ...base, start: alarm.alarmTime, allDay: false };
  }

  handleDateSelect(selectInfo: DateSelectArg): void {
    this.editModal.openForCreate(selectInfo.startStr, selectInfo.allDay);
    selectInfo.view.calendar.unselect();
  }

  handleEventClick(clickInfo: EventClickArg): void {
    const alarm = clickInfo.event.extendedProps['alarm'] as AlarmScheduleEntityModel;
    this.editModal.openForEdit(alarm);
  }

  handleEventDrop(dropInfo: EventDropArg): void {
    const alarm = dropInfo.event.extendedProps['alarm'] as AlarmScheduleEntityModel;
    // Normalize to UTC ISO string for consistent backend storage
    const newStartUtc = new Date(dropInfo.event.startStr).toISOString();
    const updatedAlarm: AlarmScheduleEntityModel = { ...alarm, alarmTime: newStartUtc };

    // For recurring alarms also update dtstart in serializedRecurrence so
    // the rrule stays in sync and the drag is reflected after reload.
    if (alarm.hasRecurrence && alarm.serializedRecurrence) {
      try {
        const rec = JSON.parse(alarm.serializedRecurrence) as AlarmRecurrence;
        const start = dropInfo.event.start!;
        const pad = (value: number) => String(value).padStart(2, '0');
        rec.dtstart = `${start.getFullYear()}-${pad(start.getMonth() + 1)}-${pad(start.getDate())}T${pad(start.getHours())}:${pad(start.getMinutes())}:00`;
        updatedAlarm.serializedRecurrence = JSON.stringify(rec);
      } catch { /* ignore malformed recurrence */ }
    }

    this.rulesClient.apiRulesAlarmsIdPut(alarm.id!, updatedAlarm).subscribe({
      next: () => this.loadAlarms(),
      error: (err) => {
        console.error('Error updating alarm after drag:', err);
        dropInfo.revert();
      }
    });
  }

  createTemperatureSchedule(): void {
    this.editModal.openTemperatureSchedule();
  }

  onAlarmSaved(): void {
    this.loadAlarms();
  }

  private scheduleCalendarResize(): void {
    setTimeout(() => {
      const api = this.fullCalendar?.getApi();
      if (!api) {
        return;
      }

      api.render();
      api.updateSize();
    }, 0);
  }
}
