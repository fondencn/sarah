import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../../services/api/rules-service/model/alarmScheduleEntity';
import { SpeechVolumeModel } from '../../services/api/rules-service/model/speechVolume';

export enum AlarmContentTypeModel {
  Text = 0,
  TemperatureSchedule = 1
}

export interface AlarmRecurrence {
  freq: 'daily' | 'weekly' | 'monthly' | 'yearly';
  interval: number;
  byweekday?: string[];
  until?: string;
  dtstart: string;
}

/** Format a Date as the local-timezone value required by a datetime-local input (YYYY-MM-DDTHH:mm). */
function toLocalDatetimeInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

@Component({
  selector: 'alarm-edit-modal',
  templateUrl: './alarm-edit-modal.component.html',
  styleUrls: ['./alarm-edit-modal.component.css']
})
export class AlarmEditModalComponent {
  @Output() alarmSaved = new EventEmitter<void>();

  isVisible = false;
  isEditing = false;
  isSaving = false;
  currentAlarmId: number | undefined;
  readonly contentTypes = AlarmContentTypeModel;

  form: FormGroup;

  readonly weekdays = [
    { key: 'mo', label: 'Mon' },
    { key: 'tu', label: 'Tue' },
    { key: 'we', label: 'Wed' },
    { key: 'th', label: 'Thu' },
    { key: 'fr', label: 'Fri' },
    { key: 'sa', label: 'Sat' },
    { key: 'su', label: 'Sun' }
  ];

  readonly volumeOptions = [
    { value: SpeechVolumeModel.NUMBER_0, label: 'Low' },
    { value: SpeechVolumeModel.NUMBER_1, label: 'Medium' },
    { value: SpeechVolumeModel.NUMBER_2, label: 'High' },
    { value: SpeechVolumeModel.NUMBER_3, label: 'Very High' }
  ];

  readonly frequencyOptions = [
    { value: 'daily', label: 'Daily' },
    { value: 'weekly', label: 'Weekly' },
    { value: 'monthly', label: 'Monthly' },
    { value: 'yearly', label: 'Yearly' }
  ];

  readonly contentTypeOptions = [
    { value: AlarmContentTypeModel.Text, label: 'Textalarm' },
    { value: AlarmContentTypeModel.TemperatureSchedule, label: 'Temperaturplan' }
  ];

  constructor(private fb: FormBuilder, private rulesClient: RulesClient) {
    this.form = this.fb.group({
      contentType: [AlarmContentTypeModel.Text, Validators.required],
      text: ['', Validators.required],
      alarmTime: ['', Validators.required],
      volume: [SpeechVolumeModel.NUMBER_1, Validators.required],
      targetSpeaker: [''],
      isActive: [true],
      isNurInFerien: [false],
      isNichtInFerien: [false],
      hasRecurrence: [false],
      temperature: this.fb.group({
        roomId: [''],
        targetTemperature: [21],
        dayOfWeek: [0],
        suppressDuringSummer: [true]
      }),
      recurrence: this.fb.group({
        freq: ['weekly'],
        interval: [1, [Validators.min(1)]],
        byweekday: [[]],
        until: ['']
      })
    });

    this.form.get('contentType')?.valueChanges.subscribe(() => this.syncValidators());
    this.syncValidators();
  }

  get hasRecurrence(): boolean {
    return this.form.get('hasRecurrence')?.value;
  }

  get isTemperatureContent(): boolean {
    return Number(this.form.get('contentType')?.value) === AlarmContentTypeModel.TemperatureSchedule;
  }

  get selectedFreq(): string {
    return this.form.get('recurrence.freq')?.value ?? 'weekly';
  }

  openForCreate(dateStr: string, allDay: boolean): void {
    this.isEditing = false;
    this.currentAlarmId = undefined;

    // Convert FullCalendar's startStr to local datetime-local format.
    // For all-day slots dateStr is YYYY-MM-DD; default to 08:00 local time.
    // For time slots dateStr may include offset (e.g. 2024-03-14T10:00:00+01:00);
    // parse via Date and reformat to local time.
    let alarmTime: string;
    if (allDay) {
      alarmTime = dateStr.substring(0, 10) + 'T08:00';
    } else {
      alarmTime = toLocalDatetimeInput(new Date(dateStr));
    }

    this.form.reset({
      contentType: AlarmContentTypeModel.Text,
      text: '',
      alarmTime,
      volume: SpeechVolumeModel.NUMBER_1,
      targetSpeaker: '',
      isActive: true,
      isNurInFerien: false,
      isNichtInFerien: false,
      hasRecurrence: false,
      temperature: { roomId: '', targetTemperature: 21, dayOfWeek: 0, suppressDuringSummer: true },
      recurrence: { freq: 'weekly', interval: 1, byweekday: [], until: '' }
    });

    this.syncValidators();

    this.isVisible = true;
  }

  openForEdit(alarm: AlarmScheduleEntityModel): void {
    this.isEditing = true;
    this.currentAlarmId = alarm.id;

    // Convert UTC ISO string from API to local datetime-local format (YYYY-MM-DDTHH:mm).
    const raw = alarm.alarmTime ?? '';
    const alarmTime = raw ? toLocalDatetimeInput(new Date(raw)) : '';
    const contentType = Number(alarm.contentType ?? AlarmContentTypeModel.Text);

    let temperature = { roomId: '', targetTemperature: 21, dayOfWeek: 0, suppressDuringSummer: true };
    if (alarm.contentJson) {
      try {
        const parsed = JSON.parse(alarm.contentJson) as {
          roomId?: number;
          targetTemperature?: number;
          dayOfWeek?: number;
          suppressDuringSummer?: boolean;
        };
        temperature = {
          roomId: parsed.roomId != null ? String(parsed.roomId) : '',
          targetTemperature: parsed.targetTemperature ?? 21,
          dayOfWeek: parsed.dayOfWeek ?? 0,
          suppressDuringSummer: parsed.suppressDuringSummer ?? true
        };
      } catch { /* ignore parse errors */ }
    }

    let recurrence = { freq: 'weekly', interval: 1, byweekday: [] as string[], until: '' };
    if (alarm.hasRecurrence && alarm.serializedRecurrence) {
      try {
        const parsed = JSON.parse(alarm.serializedRecurrence) as AlarmRecurrence;
        recurrence = {
          freq: parsed.freq ?? 'weekly',
          interval: parsed.interval ?? 1,
          byweekday: parsed.byweekday ?? [],
          until: parsed.until ?? ''
        };
      } catch { /* ignore parse errors */ }
    }

    this.form.patchValue({
      contentType,
      text: alarm.text,
      alarmTime,
      volume: alarm.volume,
      targetSpeaker: alarm.targetSpeaker ?? '',
      isActive: alarm.isActive,
      isNurInFerien: alarm.isNurInFerien ?? false,
      isNichtInFerien: alarm.isNichtInFerien ?? false,
      hasRecurrence: alarm.hasRecurrence,
      temperature,
      recurrence
    });

    this.syncValidators();

    this.isVisible = true;
  }

  close(): void {
    this.isVisible = false;
  }

  isWeekdaySelected(day: string): boolean {
    const days: string[] = this.form.get('recurrence.byweekday')?.value ?? [];
    return days.includes(day);
  }

  toggleWeekday(day: string): void {
    const ctrl = this.form.get('recurrence.byweekday');
    if (!ctrl) return;
    const days: string[] = [...(ctrl.value ?? [])];
    const idx = days.indexOf(day);
    if (idx >= 0) {
      days.splice(idx, 1);
    } else {
      days.push(day);
    }
    ctrl.setValue(days);
  }

  freqLabel(freq: string): string {
    switch (freq) {
      case 'daily': return 'day(s)';
      case 'weekly': return 'week(s)';
      case 'monthly': return 'month(s)';
      case 'yearly': return 'year(s)';
      default: return 'occurrence(s)';
    }
  }

  onContentTypeChanged(): void {
    this.syncValidators();
  }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    const v = this.form.value;

    const contentType = Number(v.contentType ?? AlarmContentTypeModel.Text);

    // Build ISO datetime string
    const alarmTime = new Date(v.alarmTime).toISOString();

    // Build serialized recurrence if needed
    let serializedRecurrence: string | null = null;
    if (v.hasRecurrence) {
      const rec: AlarmRecurrence = {
        freq: v.recurrence.freq,
        interval: v.recurrence.interval ?? 1,
        dtstart: alarmTime,
        ...(v.recurrence.byweekday?.length ? { byweekday: v.recurrence.byweekday } : {}),
        ...(v.recurrence.until ? { until: v.recurrence.until } : {})
      };
      serializedRecurrence = JSON.stringify(rec);
    }

    const text = contentType === AlarmContentTypeModel.TemperatureSchedule
      ? (v.text?.trim() || `Temperatur ${Number(v.temperature?.targetTemperature ?? 0).toFixed(1)}°C`)
      : v.text;

    const contentJson = contentType === AlarmContentTypeModel.TemperatureSchedule
      ? JSON.stringify({
          type: 'temperatureSchedule',
          roomId: v.temperature?.roomId === '' ? null : Number(v.temperature?.roomId),
          targetTemperature: Number(v.temperature?.targetTemperature ?? 0),
          dayOfWeek: Number(v.temperature?.dayOfWeek ?? 0),
          suppressDuringSummer: !!v.temperature?.suppressDuringSummer,
          text
        })
      : JSON.stringify({
          type: 'text',
          text,
          targetSpeaker: v.targetSpeaker || null,
          volume: Number(v.volume)
        });

    // Validate volume value against enum before assignment
    const rawVolume = Number(v.volume);
    const validVolumes: number[] = [
      SpeechVolumeModel.NUMBER_0,
      SpeechVolumeModel.NUMBER_1,
      SpeechVolumeModel.NUMBER_2,
      SpeechVolumeModel.NUMBER_3
    ];
    const volume: SpeechVolumeModel = validVolumes.includes(rawVolume)
      ? (rawVolume as SpeechVolumeModel)
      : SpeechVolumeModel.NUMBER_1;

    const alarm: AlarmScheduleEntityModel = {
      id: this.currentAlarmId,
      contentType,
      contentJson,
      text: v.text,
      alarmTime,
      volume,
      targetSpeaker: v.targetSpeaker || null,
      isActive: v.isActive,
      hasRecurrence: v.hasRecurrence,
      serializedRecurrence,
      isNurInFerien: v.isNurInFerien || null,
      isNichtInFerien: v.isNichtInFerien || null,
      isNurInFerienNotNull: !!v.isNurInFerien,
      isNichtInFerienNotNull: !!v.isNichtInFerien
    };

    if (this.isEditing && this.currentAlarmId !== undefined) {
      this.rulesClient.apiRulesAlarmsIdPut(this.currentAlarmId, alarm).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err) => this.onSaveError(err)
      });
    } else {
      this.rulesClient.apiRulesAlarmsPost(alarm).subscribe({
        next: () => this.onSaveSuccess(),
        error: (err) => this.onSaveError(err)
      });
    }
  }

  delete(): void {
    if (this.currentAlarmId === undefined) return;
    this.isSaving = true;
    this.rulesClient.apiRulesAlarmsIdDelete(this.currentAlarmId).subscribe({
      next: () => this.onSaveSuccess(),
      error: (err) => this.onSaveError(err)
    });
  }

  private onSaveSuccess(): void {
    this.isSaving = false;
    this.isVisible = false;
    this.alarmSaved.emit();
  }

  private onSaveError(err: unknown): void {
    console.error('Alarm operation failed:', err);
    this.isSaving = false;
  }

  private syncValidators(): void {
    const textControl = this.form.get('text');
    const targetSpeakerControl = this.form.get('targetSpeaker');
    const volumeControl = this.form.get('volume');
    const temperatureGroup = this.form.get('temperature');
    const roomIdControl = this.form.get('temperature.roomId');
    const targetTemperatureControl = this.form.get('temperature.targetTemperature');

    if (this.isTemperatureContent) {
      textControl?.clearValidators();
      targetSpeakerControl?.clearValidators();
      volumeControl?.clearValidators();
      roomIdControl?.setValidators([Validators.required]);
      targetTemperatureControl?.setValidators([Validators.required]);
    } else {
      textControl?.setValidators([Validators.required]);
      targetSpeakerControl?.clearValidators();
      volumeControl?.setValidators([Validators.required]);
      roomIdControl?.clearValidators();
      targetTemperatureControl?.clearValidators();
    }

    textControl?.updateValueAndValidity({ emitEvent: false });
    targetSpeakerControl?.updateValueAndValidity({ emitEvent: false });
    volumeControl?.updateValueAndValidity({ emitEvent: false });
    roomIdControl?.updateValueAndValidity({ emitEvent: false });
    targetTemperatureControl?.updateValueAndValidity({ emitEvent: false });
    temperatureGroup?.updateValueAndValidity({ emitEvent: false });
  }
}
