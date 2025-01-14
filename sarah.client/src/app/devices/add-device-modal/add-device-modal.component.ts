import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { NetworkElementDto } from '../../services/api-client';

@Component({
  selector: 'app-add-device-modal',
  templateUrl: './add-device-modal.component.html',
  styleUrls: ['./add-device-modal.component.css']
})

export class AddDeviceModalComponent {

  @Output() deviceAdded = new EventEmitter<NetworkElementDto>();
  deviceForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.deviceForm = this.fb.group({
      id: ['', Validators.required],
      name: ['', Validators.required],
      typeName: ['', Validators.required],
      info: ['']
    });
  }

  public onCancel() {
    this.closeDialog();
  }


  public onOk() {
    if (this.deviceForm.valid) {
      this.deviceAdded.emit(this.deviceForm.value);
      this.closeDialog();
    }
  }

  public closeDialog() {
    // Ensure the modal element exists before trying to show it
    const modalElement = document.getElementById('addDeviceModal');
    if (modalElement) {
      // Assuming you are using Bootstrap, you can use the Bootstrap modal method
      const bootstrapModal = new (window as any).bootstrap.Modal(modalElement);
      bootstrapModal.hide();
    }
  }
}
