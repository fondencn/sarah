import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';

import { PersondetailsComponent } from './persondetails.component';
import { PersonsClient } from '../../../../services/api/persons-service/api/persons.service';
import { GeofencesClient } from '../../../../services/api/geofences-service/api/geofences.service';
import { DevicesClient } from '../../../../services/api/device-service/api/api';
import { LoggingService } from '../../../../services/logging.service';

describe('PersondetailsComponent', () => {
  let component: PersondetailsComponent;
  let fixture: ComponentFixture<PersondetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PersondetailsComponent],
      providers: [
        { provide: PersonsClient, useValue: {} },
        { provide: GeofencesClient, useValue: {} },
        { provide: DevicesClient, useValue: {} },
        { provide: LoggingService, useValue: { error: () => undefined, warn: () => undefined } }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PersondetailsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
