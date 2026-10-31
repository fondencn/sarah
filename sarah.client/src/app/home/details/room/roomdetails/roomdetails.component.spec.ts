import { ComponentFixture, TestBed } from "@angular/core/testing";
import { of, Subject, throwError } from "rxjs";

import { RoomdetailsComponent } from "./roomdetails.component";
import { RoomsClient } from "../../../../services/api/room-service/api/rooms.service";
import { RoomDtoModel as RoomDto } from "../../../../services/api/room-service/model/models";
import { RulesClient } from "../../../../services/api/rules-service/api/rules.service";
import { AlarmContentTypeModel } from "../../../../services/api/rules-service/model/alarmContentType";
import { AlarmScheduleEntityModel } from "../../../../services/api/rules-service/model/alarmScheduleEntity";
import { SpeechVolumeModel } from "../../../../services/api/rules-service/model/speechVolume";

describe("RoomdetailsComponent", () => {
  let component: RoomdetailsComponent;
  let fixture: ComponentFixture<RoomdetailsComponent>;
  let roomsClient: jasmine.SpyObj<RoomsClient>;
  let rulesClient: jasmine.SpyObj<RulesClient>;

  beforeEach(async () => {
    roomsClient = jasmine.createSpyObj<RoomsClient>("RoomsClient", ["apiRoomsIdGet"]);
    rulesClient = jasmine.createSpyObj<RulesClient>("RulesClient", ["apiRulesAlarmsGet"]);
    roomsClient.apiRoomsIdGet.and.returnValue(of({} as RoomDto) as any);
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([]) as any);

    await TestBed.configureTestingModule({
      declarations: [RoomdetailsComponent],
      providers: [
        { provide: RoomsClient, useValue: roomsClient },
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
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([later, otherRoom, earlier]) as any);

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([earlier, later]);
  });

  it("reads mixed-case DTSTART when displaying and sorting schedules", () => {
    const later = schedule(10, "2026-10-12T18:00:00Z", 22, "DTSTART");
    const earlier = schedule(10, "2026-10-12T07:15:00Z", 20, "Dtstart");
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([later, earlier]) as any);

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([earlier, later]);
    expect(component.getScheduleTime(earlier)).toBe("07:15");
  });

  it("places invalid times last and preserves original order for ties and invalid entries", () => {
    const tiedFirst = schedule(10, "2026-10-12T07:30:00", 20);
    const invalidFirst = schedule(10, "not-a-time", 21);
    const tiedSecond = schedule(10, "2026-10-13T07:30:00", 22);
    const invalidSecond = schedule(10, "2026-10-13T27:30:00", 23);
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([tiedFirst, invalidFirst, tiedSecond, invalidSecond]) as any);

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([tiedFirst, tiedSecond, invalidFirst, invalidSecond]);
  });

  it("keeps the empty state when this room has no temperature schedules", () => {
    rulesClient.apiRulesAlarmsGet.and.returnValue(of([schedule(11, "2026-10-12T07:30:00", 20)]) as any);

    component.loadTemperatureSchedules();

    expect(component.temperatureSchedules).toEqual([]);
    expect(component.temperatureSchedulesError).toBeFalse();
    expect(component.temperatureSchedulesLoading).toBeFalse();
  });

  it("reports schedule load failures instead of showing the empty state", () => {
    rulesClient.apiRulesAlarmsGet.and.returnValue(throwError(() => new Error("request failed")) as any);
    component.roomDetails = { name: "Room 10" } as RoomDto;

    component.loadTemperatureSchedules();
    fixture.detectChanges();

    expect(component.temperatureSchedules).toEqual([]);
    expect(component.temperatureSchedulesError).toBeTrue();
    expect(component.temperatureSchedulesLoading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain("Temperatur-Zeitpläne konnten nicht geladen werden.");
    expect(fixture.nativeElement.textContent).not.toContain("Für diesen Raum sind keine Temperatur-Zeitpläne vorhanden.");
  });

  it("reloads room details and schedules for a changed id without applying older responses", () => {
    const firstRoom = new Subject<RoomDto>();
    const secondRoom = new Subject<RoomDto>();
    const firstSchedules = new Subject<AlarmScheduleEntityModel[]>();
    const secondSchedules = new Subject<AlarmScheduleEntityModel[]>();
    roomsClient.apiRoomsIdGet.and.returnValues(firstRoom.asObservable() as any, secondRoom.asObservable() as any);
    rulesClient.apiRulesAlarmsGet.and.returnValues(firstSchedules.asObservable() as any, secondSchedules.asObservable() as any);

    fixture.componentRef.setInput("id", 10);
    fixture.detectChanges();
    fixture.componentRef.setInput("id", 11);
    fixture.detectChanges();

    firstRoom.next({ id: 10, name: "Old room" } as RoomDto);
    firstSchedules.next([schedule(10, "2026-10-12T07:00:00Z", 20)]);
    secondRoom.next({ id: 11, name: "New room" } as RoomDto);
    secondSchedules.next([schedule(11, "2026-10-12T08:00:00Z", 21)]);

    expect(roomsClient.apiRoomsIdGet).toHaveBeenCalledWith(10);
    expect(roomsClient.apiRoomsIdGet).toHaveBeenCalledWith(11);
    expect(component.roomDetails?.name).toBe("New room");
    expect(component.temperatureSchedules).toEqual([schedule(11, "2026-10-12T08:00:00Z", 21)]);
  });

  function schedule(roomId: number, dtstart: string, targetTemperature: number, propertyName = "dtstart"): AlarmScheduleEntityModel {
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
      serializedRecurrence: JSON.stringify({ [propertyName]: dtstart })
    };
  }
});
