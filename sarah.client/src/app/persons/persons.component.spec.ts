import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PersonsComponent } from './persons.component';
import { PersonsClient } from '../services/api/persons-service/api/persons.service';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { GeofencesClient } from '../services/api/geofences-service/api/geofences.service';
import { DialogService } from '../services/dialog.service';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { LoggingService } from '../services/logging.service';

describe('PersonsComponent', () => {
  let component: PersonsComponent;
  let fixture: ComponentFixture<PersonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PersonsComponent],
      providers: [
        { provide: PersonsClient, useValue: {} },
        { provide: DevicesClient, useValue: {} },
        { provide: GeofencesClient, useValue: {} },
        { provide: DialogService, useValue: { dialogClosed: { subscribe: () => ({ unsubscribe: () => undefined }) } } },
        { provide: DashboardRuntimeService, useValue: {} },
        { provide: LoggingService, useValue: { error: () => undefined, warn: () => undefined } }
      ],
      imports: [TranslateModule.forRoot()],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PersonsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
