import { Component } from '@angular/core';
import { DialogContent } from '../../services/dialogcontent';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PersonDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';

@Component({
  selector: 'editPersonModal',
  templateUrl: './edit-person-modal.component.html',
  styleUrl: './edit-person-modal.component.css'
})
export class EditPersonModalComponent extends DialogContent {
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

  // TODO: populate from actual service calls
  public allTrackers: any[] = [];
  public allMobilePhones: string[] = [];

  public get dataContext(): PersonDto | null {
    this._person = this.personForm.valid ? this.personForm.value : null;
    return this._person;
  }

  public set dataContext(person: PersonDto | null) {
    this._person = person;
    if (person) {
      this.personForm.patchValue({
        name: person.name,
        id : person.id,
        mobilePhoneHostname: person.mobilePhoneHostname,
        gpsTrackerId: person.gpsTrackerID
      });
    }
  }


  constructor(private fb: FormBuilder, dialogService: DialogService) {
    super(dialogService);

    this.personForm = this.fb.group({
      name: ['', Validators.required],
      mobilePhoneHostname: [''],
      gpsTrackerId: [0],
      id: [0]
    });
  }


  protected override canOk(): boolean {
    return this.personForm.valid;
  }
}
