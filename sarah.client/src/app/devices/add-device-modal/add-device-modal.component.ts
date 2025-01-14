import { Component} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NetworkElementDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';
import { DialogContent } from '../../services/dialogcontent';

@Component({
  selector: 'app-add-device-modal',
  templateUrl: './add-device-modal.component.html',
  styleUrls: ['./add-device-modal.component.css']
})

export class AddDeviceModalComponent extends DialogContent {

  protected override canOk(): boolean {
    return this.deviceForm.valid;
  }

  deviceForm: FormGroup;

  public get newElement(): NetworkElementDto | null {
    return this.deviceForm.valid ? this.deviceForm.value : null;
  }

  constructor(private fb: FormBuilder, dialogService: DialogService) {
    super(dialogService);
    this.deviceForm = this.fb.group({
      id: ['', Validators.required],
      name: ['', Validators.required],
      typeName: ['', Validators.required],
      info: ['']
    });
  }

}
