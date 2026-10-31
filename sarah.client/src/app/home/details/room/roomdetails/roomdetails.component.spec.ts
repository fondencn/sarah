import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of } from "rxjs";

import { RoomdetailsComponent } from "./roomdetails.component";
import { RoomsClient } from "../../../../services/api/room-service/api/rooms.service";
import { RulesClient } from "../../../../services/api/rules-service/api/rules.service";
import { AlarmContentTypeModel } from "../../../../services/api/rules-service/model/alarmContentType";
import { AlarmScheduleEntityModel } from "../../../../services/api/rules-service/model/alarmScheduleEntity";
import { SpeechVolumeModel } from "../../../../services/api/rules-service/model/speechVolume";

describe("RoomdetailsComponent", () => {
  let component: RoomdetailsComponent;
  let fixture: ComponentFixture<RoomdetailsComponent>;
  let rulesClient: jasmine.SpyObj<RulesClient>;

  beforeEach(async () => {
    rulesClient = jasmine.createSpyObj<RulesClient>("RulesClient", ["apiRulesAlarmsGet"]);
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [RoomdetailsComponent],
      providers: [
        { provide: RoomsClient, useValue: {} },
        { provide: RulesClient, useValue: rulesClient }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RoomdetailsComponent);
    component = fixture.componentInstance;
    component.id = 10;
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("filters schedules to this room and orders by stored clock time without converting timezone offsets", () => {
    const later = schedule(10, "2026-10-12T18:00:00Z", 22);
    const earlier = schedule(10, "2026-10-12T07:15:00+05:00", 20);
    const otherRoom = schedule(11, "2026-10-12T06:00:00", 19);
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([later, otherRoom, earlier]));

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([earlier, later]);
  });

  it("places invalid times last and preserves original order for ties and invalid entries", () => {
    const tiedFirst = schedule(10, "2026-10-12T07:30:00", 20);
    const invalidFirst = schedule(10, "not-a-time", 21);
    const tiedSecond = schedule(10, "2026-10-13T07:30:00", 22);
    const invalidSecond = schedule(10, "2026-10-13T27:30:00", 23);
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([tiedFirst, invalidFirst, tiedSecond, invalidSecond]));

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([tiedFirst, tiedSecond, invalidFirst, invalidSecond]);
  });

  it("keeps the empty state when this room has no temperature schedules", () => {
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([schedule(11, "2026-10-12T07:30:00", 20)]));

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([]);
  });

  function schedule(roomId: number, dtstart: string, targetTemperature: number): AlarmScheduleEntityModel {
    return {
      id: targetTemperature,
      alarmTime: dtstart,
      text: `Temperatur ${targetTemperature}°C`,
      contentType: AlarmContentTypeModel.NUMBER_1,
      contentJson: JSON.stringify({ roomId, targetTemperature }),
      content: { roomId, targetTemperature },
      isActive: true,
      volume: SpeechVolumeModel.NUMBER_1,
      hasRecurrence: true,
      serializedRecurrence: JSON.stringify({ dtstart })
    };
  }
});
