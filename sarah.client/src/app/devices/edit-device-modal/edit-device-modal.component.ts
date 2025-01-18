import { Component} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DeviceDto, EnumDto, NetworkElementDto, RoomDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';
import { DialogContent } from '../../services/dialogcontent';
import { CacheService } from '../../services/cache.service';

@Component({
  selector: 'editDeviceModal',
  templateUrl: './edit-device-modal.component.html',
  styleUrls: ['./edit-device-modal.component.css']
})

export class EditDeviceModalComponent extends DialogContent {
  
  deviceForm: FormGroup;
  private _device: DeviceDto | null = null;
  okButtonText : string = "Save";
  private _isNewDevice: boolean = false;

  public get isNewDevice(): boolean {
    return this._isNewDevice;
  }

  public set isNewDevice(value: boolean) {
    this._isNewDevice = value;
  }

  public get allDeviceTypes(): EnumDto[] {
    return this.cacheService.get<EnumDto[]>(CacheService.DEVICE_TYPES_KEY) ?? [];
  }
  
  public get allRooms(): RoomDto[] {
    return this.cacheService.get<RoomDto[]>(CacheService.ROOMS_KEY) ?? [];
  }

  public get allNetworkElements(): NetworkElementDto[] {
    return this.cacheService.get<NetworkElementDto[]>(CacheService.NETWORK_ELEMENTS_KEY) ?? [];
  }

  public get dataContext(): DeviceDto | null {
    this._device = this.deviceForm.valid ? this.deviceForm.value : null;
    return this._device;
  }

  public set dataContext(device: DeviceDto | null) {
    this._device = device;
    if (device) {
      this.deviceForm.patchValue({
        nodeID: device.nodeId,
        name: device.name,
        deviceType: device.deviceType,
        roomId: device.roomId,
        isReadonly: device.isReadonly,
        id : device.id
      });
    }
  }

  

  constructor(private fb: FormBuilder, dialogService: DialogService, public cacheService: CacheService) {
    super(dialogService);

    this.deviceForm = this.fb.group({
      nodeId: [0, Validators.required],
      name: ['', Validators.required],
      deviceType: [0, Validators.required], 
      roomId: [0, Validators.required], 
      isReadonly: [false], 
      id: [0]
    });
  }


  protected override canOk(): boolean {
    return this.deviceForm.valid;
  }

}
