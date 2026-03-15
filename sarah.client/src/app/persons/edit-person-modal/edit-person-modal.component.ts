import { Component, OnInit } from '@angular/core';
import { DialogContent } from '../../services/dialogcontent';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PersonDto, TrackerDto } from '../../models/api-types';
import { DialogService } from '../../services/dialog.service';
import { DevicesClient } from '../../services/api/device-service/api/devices.service';
import { PersonsExtService } from '../../services/persons-ext.service';
import { LoggingService } from '../../services/logging.service';

@Component({
  selector: 'editPersonModal',
  templateUrl: './edit-person-modal.component.html',
  styleUrl: './edit-person-modal.component.css'
})
export class EditPersonModalComponent extends DialogContent implements OnInit {
  personForm: FormGroup;
  private _person: PersonDto | null = null;
  okButtonText : string = "Save";
  private _isNewPerson: boolean = false;

  public get isNewPerson() : boolean {
    return this._isNewPerson;
  }

  public set isNewPerson(value: boolean) {
    this._isNewPerson = value;
  }

  public allTrackers: TrackerDto[] = [];
  public allMobilePhones: string[] = [];

  public get dataContext(): PersonDto | null {
    if (!this.personForm.valid) return null;
    const v = this.personForm.value;
    return {
      id: v.id != null ? Number(v.id) : 0,
      name: v.name,
      mobilePhoneHostname: v.mobilePhoneHostname,
      gpsTrackerID: v.gpsTrackerID != null ? Number(v.gpsTrackerID) : 0
    } as PersonDto;
  }

  public set dataContext(person: PersonDto | null) {
    this._person = person;
    if (person) {
      this.personForm.patchValue({
        name: person.name,
        id : person.id,
        mobilePhoneHostname: person.mobilePhoneHostname,
        gpsTrackerID: person.gpsTrackerID
      });
    }
  }


  constructor(private fb: FormBuilder, dialogService: DialogService,
              private devicesService: DevicesClient, private personsExtService: PersonsExtService,
              private logger: LoggingService) {
    super(dialogService);

    this.personForm = this.fb.group({
      name: ['', Validators.required],
      mobilePhoneHostname: [''],
      gpsTrackerID: [0],
      id: [0]
    });
  }

  ngOnInit(): void {
    this.loadTrackers();
    this.loadMobilePhones();
  }

  private loadTrackers(): void {
    this.devicesService.devicesGetTrackersGETApiDevicesTrackers().subscribe({
      next: (trackers: TrackerDto[]) => { this.allTrackers = trackers; },
      error: (err) => this.logger.error('Error loading trackers:', err)
    });
  }

  private loadMobilePhones(): void {
    this.personsExtService.getKnownHomeNetworkDevices().subscribe({
      next: (devices: string[]) => { this.allMobilePhones = devices; },
      error: (err) => this.logger.error('Error loading known home network devices:', err)
    });
  }

  protected override canOk(): boolean {
    return this.personForm.valid;
  }
}
