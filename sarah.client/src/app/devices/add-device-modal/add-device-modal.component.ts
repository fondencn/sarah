import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NetworkElementDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';

@Component({
  selector: 'app-add-device-modal',
  templateUrl: './add-device-modal.component.html',
  styleUrls: ['./add-device-modal.component.css']
})

export class AddDeviceModalComponent {

  @Output() closed = new EventEmitter<boolean>();
  deviceForm: FormGroup;

  public get newElement(): NetworkElementDto | null {
    return this.deviceForm.valid ? this.deviceForm.value : null;
  }

  constructor(private fb: FormBuilder, private dialogService : DialogService) {
    this.deviceForm = this.fb.group({
      id: ['', Validators.required],
      name: ['', Validators.required],
      typeName: ['', Validators.required],
      info: ['']
    });
  }

  public onCancel() {
    this.dialogService.hideDialog();
    this.closed.emit(false);
  }


  public onOk() {
    if (this.deviceForm.valid) {
      this.dialogService.hideDialog();
      this.closed.emit(true);
    }
  }
}
