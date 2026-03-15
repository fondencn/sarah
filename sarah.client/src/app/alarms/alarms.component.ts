import { Component, OnInit, ViewChild } from '@angular/core';
import { CalendarOptions, EventInput, DateSelectArg, EventClickArg, EventDropArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import rrulePlugin from '@fullcalendar/rrule';
import bootstrap5Plugin from '@fullcalendar/bootstrap5';
import deLocale from '@fullcalendar/core/locales/de';

import { RulesClient } from '../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../services/api/rules-service/model/alarmScheduleEntity';
import { AlarmEditModalComponent, AlarmRecurrence } from './alarm-edit-modal/alarm-edit-modal.component';

@Component({
  selector: 'app-alarms',
  templateUrl: './alarms.component.html',
  styleUrls: ['./alarms.component.css']
})
export class AlarmsComponent implements OnInit {
  @ViewChild(AlarmEditModalComponent) editModal!: AlarmEditModalComponent;

  isLoading = false;

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
    select: this.handleDateSelect.bind(this),
    eventClick: this.handleEventClick.bind(this),
    eventDrop: this.handleEventDrop.bind(this),
    events: []
  };

  constructor(private rulesClient: RulesClient) {}

  ngOnInit(): void {
    this.loadAlarms();
  }

  loadAlarms(): void {
    this.isLoading = true;
    this.rulesClient.apiRulesAlarmsGet().subscribe({
      next: (alarms: AlarmScheduleEntityModel[]) => {
        const events = alarms.map(a => this.alarmToEvent(a));
        this.calendarOptions = { ...this.calendarOptions, events };
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading alarms:', err);
        this.isLoading = false;
      }
    });
  }

  private alarmToEvent(alarm: AlarmScheduleEntityModel): EventInput {
    const color = alarm.isActive ? '#0d6efd' : '#6c757d';
    const base: EventInput = {
      id: alarm.id?.toString(),
      title: alarm.text,
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
        rec.dtstart = newStartUtc;
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

  onAlarmSaved(): void {
    this.loadAlarms();
  }
}
