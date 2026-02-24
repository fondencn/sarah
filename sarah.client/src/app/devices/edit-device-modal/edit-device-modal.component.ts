import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DeviceDto, KnownDeviceTypes, NetworkElementDto, RoomsService, RoomDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';
import { DialogContent } from '../../services/dialogcontent';
import { DevicesExtService } from '../../services/devices-ext.service';

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
    const nodeId = v.nodeId !== null && v.nodeId !== undefined ? Number(v.nodeId) : 0;
    const deviceType = v.deviceType !== null && v.deviceType !== undefined ? Number(v.deviceType) : 0;
    const roomId = v.roomId !== null && v.roomId !== undefined ? Number(v.roomId) : null;
    return {
      id: v.id,
      name: v.name,
      nodeId: nodeId,
      deviceType: deviceType,
      roomId: roomId,
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
              private devicesExtService: DevicesExtService, private roomsService: RoomsService) {
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

  private readonly deviceTypeLabels: Record<number, string> = {
    0: 'Unknown',
    1: 'Fibaro Motion Sensor',
    2: 'Aeotec Door Sensor',
    3: 'Aeotec Z-Stick',
    4: 'Fibaro The Button',
    5: 'Popp Wall Controller',
    6: 'Popp Wall Plug',
    7: 'Fibaro Heat Controller',
    8: 'Aeotec LED Bulb',
    9: 'Fibaro RGBW Controller 2',
    10: 'Aeotec Thermostat',
    11: 'Aeotec Smart Switch 7',
    12: 'Aeotec LED Bulb 6 White',
    13: 'Fibaro Door/Window Sensor 2',
    14: 'Fibaro Wall Plug',
    15: 'Eutronic Air Quality Sensor',
    16: 'Fibaro Walli Switch',
    17: 'Fibaro Smoke Sensor',
    18: 'Fibaro Key Fob',
  };

  private loadDeviceTypes(): void {
    this.allDeviceTypes = Object.entries(KnownDeviceTypes)
      .filter(([, v]) => typeof v === 'number')
      .map(([, v]) => ({ enumKey: v as number, enumValue: this.deviceTypeLabels[v as number] ?? `Unknown (${v})` }));
  }

  private loadRooms(): void {
    this.roomsService.apiRoomsGet().subscribe({
      next: (rooms: RoomDto[]) => { this.allRooms = rooms; },
      error: (err) => console.error('Error loading rooms:', err)
    });
  }

  private loadNetworkElements(): void {
    this.devicesExtService.getElements().subscribe({
      next: (elements: NetworkElementDto[]) => { this.allNetworkElements = elements; },
      error: (err) => console.error('Error loading network elements:', err)
    });
  }

  protected override canOk(): boolean {
    return this.deviceForm.valid;
  }

}
