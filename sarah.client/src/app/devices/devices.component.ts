import { Component, OnInit, ViewChild } from '@angular/core';
import { DeviceDto, DevicesService, NetworkElementDto } from '../services/api-client'; // Import the generated client
import { DialogService } from '../services/dialog.service';
import { EditDeviceModalComponent } from './edit-device-modal/edit-device-modal.component';


@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.css']
})
export class DevicesComponent implements OnInit {



  devices: DeviceDto[] = []; // Member variable to store the devices list
  isLoading: boolean = false; // Member variable to store the loading state
  @ViewChild(EditDeviceModalComponent) editDeviceModal!: EditDeviceModalComponent;


  constructor(private devicesService: DevicesService, private dialogService : DialogService) { }

  ngOnInit(): void {
    this.onLoad();
  }

  onLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('DevicesComponent loaded');

    // Call the API to load the devices list
    this.retrieveDevices();
  }

  public retrieveDevices() {
    this.isLoading = true; // Set the loading state to true
    this.devicesService.devicesGet().subscribe({
      next: (response: DeviceDto[]) => {
        this.devices = response; // Save the devices list in the member variable
      },
      error: (error) => {
        console.error('Error fetching devices:', error);
      },
      complete: () => {
        this.isLoading = false; // Set the loading state to false
      }
    });
  }

  public addDevice() {
    this.dialogService.showDialog('addDeviceModal');
    this.dialogService.closed.subscribe(this.onDeviceAdded);
  }

  public onDeviceAdded(success: boolean) {
    this.dialogService.closed.unsubscribe();
    if (success) {

      this.devices.push(this.editDeviceModal.newElement as DeviceDto);
    }
  }



  public editDevice(device: DeviceDto) {
    throw new Error('Method not implemented.');
  }

  public deleteDevice(device: DeviceDto) {
    throw new Error('Method not implemented.');
  }
}
