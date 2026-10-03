import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { EditDeviceModalComponent } from './edit-device-modal.component';
import { DialogService } from '../../services/dialog.service';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { RoomsClient } from '../../services/api/room-service/api/rooms.service';
import { LoggingService } from '../../services/logging.service';

describe('AddDeviceModalComponent', () => {
  let component: EditDeviceModalComponent;
  let fixture: ComponentFixture<EditDeviceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [EditDeviceModalComponent],
      imports: [ReactiveFormsModule, TranslateModule.forRoot()],
      providers: [
        { provide: DialogService, useValue: {} },
        { provide: DevicesClient, useValue: {} },
        { provide: RoomsClient, useValue: {} },
        { provide: LoggingService, useValue: { error: () => undefined } }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditDeviceModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
