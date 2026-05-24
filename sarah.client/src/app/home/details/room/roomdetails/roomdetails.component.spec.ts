import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RoomdetailsComponent } from './roomdetails.component';
import { RoomsClient } from '../../../../services/api/room-service/api/rooms.service';

describe('RoomdetailsComponent', () => {
  let component: RoomdetailsComponent;
  let fixture: ComponentFixture<RoomdetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [RoomdetailsComponent],
      providers: [{ provide: RoomsClient, useValue: {} }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RoomdetailsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
