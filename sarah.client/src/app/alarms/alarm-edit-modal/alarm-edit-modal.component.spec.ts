import { FormBuilder } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { RulesClient } from '../../services/api/rules-service/api/rules.service';
import { AlarmScheduleEntityModel } from '../../services/api/rules-service/model/alarmScheduleEntity';
import { AlarmContentTypeModel, AlarmEditModalComponent } from './alarm-edit-modal.component';

describe('AlarmEditModalComponent temperature schedules', () => {
  let component: AlarmEditModalComponent;
  let rulesClient: any;

  beforeEach(() => {
    rulesClient = jasmine.createSpyObj('RulesClient', [
      'apiRulesAlarmsPost',
      'apiRulesAlarmsIdPut',
      'apiRulesAlarmsIdDelete'
    ]);
    rulesClient.apiRulesAlarmsPost.and.returnValue(of({} as AlarmScheduleEntityModel));
    rulesClient.apiRulesAlarmsIdPut.and.returnValue(of({} as AlarmScheduleEntityModel));
    rulesClient.apiRulesAlarmsIdDelete.and.returnValue(of(void 0));

    component = new AlarmEditModalComponent(
      new FormBuilder(),
      rulesClient,
      { apiRoomsGet: () => of([]) } as any
    );
  });

  it('requires a room and accepts inclusive temperatures from 8 to 30 °C', () => {
    component.openTemperatureSchedule();
    expect(component.form.invalid).toBeTrue();

    component.form.get('temperature.roomId')?.setValue('12');
    component.form.get('temperature.targetTemperature')?.setValue(8);
    expect(component.form.valid).toBeTrue();

    component.form.get('temperature.targetTemperature')?.setValue(30);
    expect(component.form.valid).toBeTrue();

    component.form.get('temperature.targetTemperature')?.setValue(7.9);
    expect(component.form.invalid).toBeTrue();
    component.form.get('temperature.targetTemperature')?.setValue(30.1);
    expect(component.form.invalid).toBeTrue();
  });

  it('saves temperature plans as recurring local-time entries without an end date', () => {
    component.openTemperatureSchedule();
    component.form.patchValue({
      alarmTime: '2026-10-12T07:30',
      temperature: { roomId: '12', targetTemperature: 21 },
      recurrence: { freq: 'weekly', interval: 1, byweekday: ['mo'], until: '2027-01-01' }
    });
    component.setTemperatureTime('07:30');

    component.save();

    expect(rulesClient.apiRulesAlarmsPost).toHaveBeenCalled();
    const saved = rulesClient.apiRulesAlarmsPost.calls.mostRecent().args[0] as AlarmScheduleEntityModel;
    expect(Number(saved.contentType)).toBe(Number(AlarmContentTypeModel.TemperatureSchedule));
    expect(saved.hasRecurrence).toBeTrue();
    expect(JSON.parse(saved.contentJson!).targetTemperature).toBe(21);
    expect(JSON.parse(saved.contentJson!).roomId).toBe(12);
    expect(JSON.parse(saved.serializedRecurrence!).dtstart).toBe('2026-10-12T07:30:00');
    expect(saved.alarmTime).toBe(new Date('2026-10-12T07:30').toISOString());
    expect(JSON.parse(saved.serializedRecurrence!).until).toBeUndefined();
    expect(component.temperatureTime).toBe('07:30');
  });

  it('preselects and locks the room when opened from a room detail page', () => {
    component.openTemperatureScheduleForRoom(12);

    expect(component.roomScheduleOnly).toBeTrue();
    expect(component.roomSelectionLocked).toBeTrue();
    expect(component.form.get('temperature.roomId')?.value).toBe('12');
    expect(component.form.get('temperature.roomId')?.disabled).toBeTrue();

    component.form.get('temperature.targetTemperature')?.setValue(7);
    expect(component.form.invalid).toBeTrue();
    component.form.get('temperature.targetTemperature')?.setValue(31);
    expect(component.form.invalid).toBeTrue();
    component.form.get('temperature.targetTemperature')?.setValue(22);
    expect(component.form.valid).toBeTrue();

    component.save();
    const saved = rulesClient.apiRulesAlarmsPost.calls.mostRecent().args[0] as AlarmScheduleEntityModel;
    expect(JSON.parse(saved.contentJson!).roomId).toBe(12);
  });

  it('locks the existing room when editing a plan from a room detail page', () => {
    const alarm = {
      id: 8,
      contentType: AlarmContentTypeModel.TemperatureSchedule,
      contentJson: JSON.stringify({ roomId: 99, targetTemperature: 20 }),
      alarmTime: '2026-10-12T06:30:00Z',
      hasRecurrence: true,
      serializedRecurrence: JSON.stringify({ freq: 'weekly', interval: 1, byweekday: ['mo'] }),
      isActive: true,
      volume: 1,
      text: 'Temperature'
    } as any as AlarmScheduleEntityModel;

    component.openForEdit(alarm, 12);

    expect(component.roomScheduleOnly).toBeTrue();
    expect(component.form.get('temperature.roomId')?.value).toBe('12');
    expect(component.form.get('temperature.roomId')?.disabled).toBeTrue();
  });

  it('shows the server conflict message for room schedule saves', () => {
    rulesClient.apiRulesAlarmsPost.and.returnValue(throwError(() => ({
      status: 409,
      error: { message: 'A schedule already exists at this time.' }
    })));
    component.openTemperatureScheduleForRoom(12);
    component.form.get('temperature.targetTemperature')?.setValue(21);

    component.save();

    expect(component.validationError).toBe('A schedule already exists at this time.');
  });
});
