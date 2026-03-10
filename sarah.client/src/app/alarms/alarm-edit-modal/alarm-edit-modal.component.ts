import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../../services/api/rules-service/model/alarmScheduleEntity';
import { SpeechVolumeModel } from '../../services/api/rules-service/model/speechVolume';

export interface AlarmRecurrence {
  freq: 'daily' | 'weekly' | 'monthly' | 'yearly';
  interval: number;
  byweekday?: string[];
  until?: string;
  dtstart: string;
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

  constructor(private fb: FormBuilder, private rulesClient: RulesClient) {
    this.form = this.fb.group({
      text: ['', Validators.required],
      alarmTime: ['', Validators.required],
      volume: [SpeechVolumeModel.NUMBER_1, Validators.required],
      targetSpeaker: [''],
      isActive: [true],
      hasRecurrence: [false],
      recurrence: this.fb.group({
        freq: ['weekly'],
        interval: [1, [Validators.min(1)]],
        byweekday: [[]],
        until: ['']
      })
    });
  }

  get hasRecurrence(): boolean {
    return this.form.get('hasRecurrence')?.value;
  }

  get selectedFreq(): string {
    return this.form.get('recurrence.freq')?.value ?? 'weekly';
  }

  openForCreate(dateStr: string, allDay: boolean): void {
    this.isEditing = false;
    this.currentAlarmId = undefined;

    // Convert selection to datetime-local format (YYYY-MM-DDTHH:mm)
    let alarmTime: string;
    if (allDay) {
      alarmTime = dateStr.substring(0, 10) + 'T08:00';
    } else {
      alarmTime = dateStr.substring(0, 16);
    }

    this.form.reset({
      text: '',
      alarmTime,
      volume: SpeechVolumeModel.NUMBER_1,
      targetSpeaker: '',
      isActive: true,
      hasRecurrence: false,
      recurrence: { freq: 'weekly', interval: 1, byweekday: [], until: '' }
    });

    this.isVisible = true;
  }

  openForEdit(alarm: AlarmScheduleEntityModel): void {
    this.isEditing = true;
    this.currentAlarmId = alarm.id;

    const alarmTime = alarm.alarmTime.substring(0, 16);

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
      text: alarm.text,
      alarmTime,
      volume: alarm.volume,
      targetSpeaker: alarm.targetSpeaker ?? '',
      isActive: alarm.isActive,
      hasRecurrence: alarm.hasRecurrence,
      recurrence
    });

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

  save(): void {
    if (this.form.invalid) return;
    this.isSaving = true;
    const v = this.form.value;

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

    const alarm: AlarmScheduleEntityModel = {
      id: this.currentAlarmId,
      text: v.text,
      alarmTime,
      volume: Number(v.volume) as SpeechVolumeModel,
      targetSpeaker: v.targetSpeaker || null,
      isActive: v.isActive,
      hasRecurrence: v.hasRecurrence,
      serializedRecurrence,
      isNurInFerienNotNull: false,
      isNichtInFerienNotNull: false
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
}
