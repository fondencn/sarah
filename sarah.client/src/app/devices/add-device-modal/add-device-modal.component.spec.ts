import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddDeviceModalComponent } from './add-device-modal.component';

describe('AddDeviceModalComponent', () => {
  let component: AddDeviceModalComponent;
  let fixture: ComponentFixture<AddDeviceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AddDeviceModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AddDeviceModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
