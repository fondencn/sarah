import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { EditPersonModalComponent } from './edit-person-modal.component';
import { DialogService } from '../../services/dialog.service';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { PersonsExtService } from '../../services/persons-ext.service';
import { LoggingService } from '../../services/logging.service';

describe('EditPersonModalComponent', () => {
  let component: EditPersonModalComponent;
  let fixture: ComponentFixture<EditPersonModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [EditPersonModalComponent],
      imports: [ReactiveFormsModule, TranslateModule.forRoot()],
      providers: [
        { provide: DialogService, useValue: {} },
        { provide: DevicesClient, useValue: {} },
        { provide: PersonsExtService, useValue: {} },
        { provide: LoggingService, useValue: { error: () => undefined } }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditPersonModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
