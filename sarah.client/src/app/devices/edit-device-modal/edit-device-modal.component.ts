import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DeviceDto, DevicesService, KnownDeviceTypes, NetworkElementDto, RoomsService, RoomDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';
import { DialogContent } from '../../services/dialogcontent';

@Component({
  selector: 'editDeviceModal',
  templateUrl: './edit-device-modal.component.html',
  styleUrls: ['./edit-device-modal.component.css']
})

export class EditDeviceModalComponent extends DialogContent implements OnInit {
  
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

  public allDeviceTypes: { enumKey: number, enumValue: string }[] = [];
  public allRooms: RoomDto[] = [];
  public allNetworkElements: NetworkElementDto[] = [];

  public get dataContext(): DeviceDto | null {
    if (!this.deviceForm.valid) return null;
    const v = this.deviceForm.value;
    return {
      id: v.id,
      name: v.name,
      nodeId: v.nodeId,
      deviceType: v.deviceType,
      roomId: v.roomId,
      isReadonly: v.isReadonly,
      isFavourite: this._device?.isFavourite ?? false
    } as DeviceDto;
  }

  public set dataContext(device: DeviceDto | null) {
    this._device = device;
    if (device) {
      this.deviceForm.patchValue({
        nodeId: device.nodeId,
        name: device.name,
        deviceType: device.deviceType,
        roomId: device.roomId,
        isReadonly: device.isReadonly,
        id : device.id
      });
    }
  }

  constructor(private fb: FormBuilder, dialogService: DialogService,
              private devicesService: DevicesService, private roomsService: RoomsService) {
    super(dialogService);

    this.deviceForm = this.fb.group({
      nodeId: [0, Validators.required],
      name: ['', Validators.required],
      deviceType: [0, Validators.required], 
      roomId: [null], 
      isReadonly: [false], 
      id: [0]
    });
  }

  ngOnInit(): void {
    this.loadDeviceTypes();
    this.loadRooms();
    this.loadNetworkElements();
  }

  private loadDeviceTypes(): void {
    this.allDeviceTypes = Object.entries(KnownDeviceTypes)
      .filter(([, v]) => typeof v === 'number')
      .map(([k, v]) => ({ enumKey: v as number, enumValue: k }));
  }

  private loadRooms(): void {
    this.roomsService.apiRoomsGet().subscribe({
      next: (rooms: RoomDto[]) => { this.allRooms = rooms; },
      error: (err) => console.error('Error loading rooms:', err)
    });
  }

  private loadNetworkElements(): void {
    this.devicesService.devicesElementsGet().subscribe({
      next: (elements: NetworkElementDto[]) => { this.allNetworkElements = elements; },
      error: (err) => console.error('Error loading network elements:', err)
    });
  }

  protected override canOk(): boolean {
    return this.deviceForm.valid;
  }

}
