import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { DeviceDtoModel as DeviceDto, KnownDeviceTypesModel as KnownDeviceTypes } from '../services/api/device-service/model/models';
import { CreateDashboardItemDto, DashboardItemType, NetworkElementDto } from '../models/api-types';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';
import { EditDeviceModalComponent } from './edit-device-modal/edit-device-modal.component';
import { Subscription } from 'rxjs';
import { DEVICE_TYPE_UNKNOWN } from '../models/device-type-constants';


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

  constructor(private devicesService: DevicesClient, private dialogService: DialogService, private dashboardService: DashboardRuntimeService) { }

  ngOnInit(): void {
    this.onLoad();
  }


  ngOnDestroy(): void {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
      this.dialogClosedSubscription = null;
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


    this.dialogClosedSubscription = this.dialogService.dialogClosed.subscribe((e : DialogClosedEventArgs) => {
      this.onDialogClosed(e);
    });
  }


  private onDialogClosed(e : DialogClosedEventArgs) {
    if (e.success && e.dialogId === 'editDeviceModal') {
      if (this.editDeviceModal.isNewDevice) {
        this.onDeviceAdded(e.success);
      } else {
        this.onDeviceEdited(e.success);
      }
    }
  }


  /**
   * Retrieves the devices list
   */
  public retrieveDevices() {
    this.isLoading = true; // Set the loading state to true
    this.devicesService.devicesGetAllGETApiDevices().subscribe({
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
    var newDevice = {} as DeviceDto;
    newDevice.nodeId = 0;
    newDevice.name = '';
    newDevice.info = '';
    newDevice.deviceType = DEVICE_TYPE_UNKNOWN;
    newDevice.isReadonly = false;
    newDevice.id = 0;

    this.editDeviceModal.dataContext = newDevice;
    this.editDeviceModal.isNewDevice = true;
    this.editDeviceModal.okButtonText = 'Add device';

    this.dialogService.showDialog('editDeviceModal');
  }

  /**
   * Adds a device
   * 
   * @param success is true if the dialog was closed with OK, false if it was closed with Cancel
   */
  public onDeviceAdded(success: boolean) {
    if (success) {
      let addedDevice: DeviceDto = this.editDeviceModal.dataContext as DeviceDto;
      this.devicesService.devicesCreatePUTApiDevices(addedDevice).subscribe({
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
    this.editDeviceModal.dataContext = device;
    this.editDeviceModal.isNewDevice = false;
    this.editDeviceModal.okButtonText = 'Save changes';
    this.dialogService.showDialog('editDeviceModal');
  }

  /**
   * Edits the device 
   * 
   * @param success is true if the dialog was closed with OK, false if it was closed with Cancel
   */
  public onDeviceEdited(success: boolean) {
    if (success) {
      this.devicesService.devicesUpdatePOSTApiDevices(this.editDeviceModal.dataContext as DeviceDto).subscribe({
        next: (response: DeviceDto) => {
          const index = this.devices.findIndex(d => d.nodeId === response.nodeId);
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
          this.devicesService.devicesDeleteDELETEApiDevicesId(device.id as number).subscribe({
            next: () => {
              this.devices = this.devices.filter(d => d.nodeId !== device.nodeId);
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

  /**
   * Sets the favourite state of a device
   * @param device the device to set the favourite state for
   * @param isFavourite true if the device should be marked as favourite, false if not
   */
  public setFavourite(device: DeviceDto, isFavourite: boolean) {
    if (isFavourite) {
      const createDto: CreateDashboardItemDto = {
        itemId: device.id,
        itemType: DashboardItemType.NUMBER_0,
        title: device.name,
        description: device.info,
        subtype: device.typeName
      };
      this.dashboardService.apiDashboardPost(createDto).subscribe({
        next: () => {
          device.isFavourite = true;
        },
        error: (err) => {
          console.error('Error adding device to dashboard:', err);
        }
      });
    } else {
      this.dashboardService.apiDashboardItemIdItemTypeDelete((device.id as number), DashboardItemType.NUMBER_0).subscribe({
        next: () => {
          device.isFavourite = false;
        },
        error: (err) => {
          console.error('Error removing device from dashboard:', err);
        }
      });
    }
  }

}
