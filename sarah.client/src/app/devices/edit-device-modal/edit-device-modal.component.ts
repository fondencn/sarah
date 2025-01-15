import { Component} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DeviceDto, EnumDto, NetworkElementDto, RoomDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';
import { DialogContent } from '../../services/dialogcontent';
import { CacheService } from '../../services/cache.service';

@Component({
  selector: 'edit-device-modal',
  templateUrl: './edit-device-modal.component.html',
  styleUrls: ['./edit-device-modal.component.css']
})

export class EditDeviceModalComponent extends DialogContent {
  
  deviceForm: FormGroup;

  public get allDeviceTypes(): EnumDto[] {
    return this.cacheService.get<EnumDto[]>(CacheService.DEVICE_TYPES_KEY) ?? [];
  }
  
  public get allRooms(): RoomDto[] {
    return this.cacheService.get<RoomDto[]>(CacheService.ROOMS_KEY) ?? [];
  }

  public get allNetworkElements(): NetworkElementDto[] {
    return this.cacheService.get<NetworkElementDto[]>(CacheService.NETWORK_ELEMENTS_KEY) ?? [];
  }

  public get newElement(): DeviceDto | null {
    return this.deviceForm.valid ? this.deviceForm.value : null;
  }

  

  constructor(private fb: FormBuilder, dialogService: DialogService, public cacheService: CacheService) {
    super(dialogService);
    this.deviceForm = this.fb.group({
      nodeID: ['', Validators.required],
      name: ['', Validators.required],
      deviceType: ['', Validators.required], 
      roomId: ['', Validators.required], 
    });
  }


  protected override canOk(): boolean {
    return this.deviceForm.valid;
  }

}
