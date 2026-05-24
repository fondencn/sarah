import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DevicedetailsComponent } from './devicedetails.component';
import { DevicesClient } from '../../../../services/api/device-service/api/devices.service';

describe('DevicedetailsComponent', () => {
  let component: DevicedetailsComponent;
  let fixture: ComponentFixture<DevicedetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DevicedetailsComponent],
      providers: [{ provide: DevicesClient, useValue: {} }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DevicedetailsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
