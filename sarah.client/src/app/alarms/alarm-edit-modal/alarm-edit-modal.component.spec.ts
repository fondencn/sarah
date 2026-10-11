import { FormBuilder } from '@angular/forms';
import { of } from 'rxjs';
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
});
