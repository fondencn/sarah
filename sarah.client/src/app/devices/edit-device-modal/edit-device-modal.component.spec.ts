import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditDeviceModalComponent } from './edit-device-modal.component';

describe('AddDeviceModalComponent', () => {
  let component: EditDeviceModalComponent;
  let fixture: ComponentFixture<EditDeviceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [EditDeviceModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditDeviceModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
