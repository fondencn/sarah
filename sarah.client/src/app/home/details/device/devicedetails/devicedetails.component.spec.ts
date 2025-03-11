import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DevicedetailsComponent } from './devicedetails.component';

describe('DevicedetailsComponent', () => {
  let component: DevicedetailsComponent;
  let fixture: ComponentFixture<DevicedetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DevicedetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DevicedetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
