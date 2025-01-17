import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { DeviceDto, DevicesService, NetworkElementDto } from '../services/api-client'; // Import the generated client
import { DialogService } from '../services/dialog.service';
import { EditDeviceModalComponent } from './edit-device-modal/edit-device-modal.component';
import { Subscription } from 'rxjs';


@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.css']
})
export class DevicesComponent implements OnInit, OnDestroy {



  devices: DeviceDto[] = []; // Member variable to store the devices list
  isLoading: boolean = false; // Member variable to store the loading state
  @ViewChild(EditDeviceModalComponent) editDeviceModal!: EditDeviceModalComponent;
  private dialogClosedSubscription: Subscription | null = null;

  constructor(private devicesService: DevicesService, private dialogService : DialogService) { }

  ngOnInit(): void {
    this.onLoad();
  }


  ngOnDestroy(): void {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
    }
  }

  /**
   * Called when the component is loaded
   */
  onLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('DevicesComponent loaded');

    // Call the API to load the devices list
    this.retrieveDevices();
  }

  /**
   * Retrieves the devices list
   */
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

  /**
   * Adds a device
   */
  public addDevice() {
    this.dialogService.showDialog('editDeviceModal');
    this.editDeviceModal.dataContext = {} as DeviceDto;
    this.editDeviceModal.okButtonText = 'Add device';
    this.dialogClosedSubscription = this.dialogService.dialogClosed.subscribe((success: boolean) => {
      this.onDeviceAdded(success);
    });
  }

  /**
   * Adds a device
   * 
   * @param success is true if the dialog was closed with OK, false if it was closed with Cancel
   */
  public onDeviceAdded(success: boolean) {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
    }
    if (success) {
      var addedDevice : DeviceDto = this.editDeviceModal.dataContext as DeviceDto;
      this.devicesService.devicesPut(addedDevice).subscribe({
        next: (response: DeviceDto) => {
          this.devices.push(response);
        },
        error: (error) => {
          console.error('Error adding device:', error);
        }
      });
    }
  }



  /**
   * Opens the edit device dialog
   * 
   * @param device The device to edit
   */
  public editDevice(device: DeviceDto) {
    this.dialogService.showDialog('editDeviceModal');
    this.editDeviceModal.dataContext = device;
    this.editDeviceModal.okButtonText = 'Save changes';
    this.dialogClosedSubscription = this.dialogService.dialogClosed.subscribe((success: boolean) => {
      this.onDeviceEdited(success);
    });
  }

  /**
   * Edits the device 
   * 
   * @param success is true if the dialog was closed with OK, false if it was closed with Cancel
   */
  public onDeviceEdited(success: boolean) {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
    }
    if (success) {
      this.devicesService.devicesPut(this.editDeviceModal.dataContext as DeviceDto).subscribe({
        next: (response: DeviceDto) => {
          const index = this.devices.findIndex(d => d.nodeID === response.nodeID);
          this.devices[index] = response;
        },
        error: (error) => {
          console.error('Error editing device:', error);
        }
      });
    }
  }

  /**
   * Deletes a device
   * 
   * @param device The device to delete
   */
  public deleteDevice(device: DeviceDto) {
    this.dialogService.showConfirmDialog('Are you sure you want to delete this device?', 'Confirm Deletion')
      .then((result: boolean) => {
        if (result) {
          this.devicesService.devicesIdDelete(device.id as number).subscribe({
            next: () => {
              this.devices = this.devices.filter(d => d.nodeID !== device.nodeID);
            },
            error: (error) => {
              console.error('Error deleting device:', error);
            }
          });
        }
      }).catch((error) => {
        console.error('Error showing confirm dialog:', error);
      });
  }

}
