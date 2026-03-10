import { Component, Input, OnInit } from '@angular/core';
import { DevicesClient } from '../../../../services/api/device-service/api/api';
import { DeviceDtoModel as DeviceDto } from '../../../../services/api/device-service/model/models';
import { ExtendedPropertyDtoModel } from '../../../../services/api/device-service/model/models';

@Component({
  selector: 'app-devicedetails',
  templateUrl: './devicedetails.component.html',
  styleUrls: ['./devicedetails.component.css']
})
export class DevicedetailsComponent implements OnInit {
  @Input() id: number = 0;
  deviceDetails: DeviceDto | null = null;
  lastUpdated : string = "";

  constructor(private deviceService: DevicesClient) {}

  ngOnInit(): void {
    this.loadDeviceDetails();
  }

  loadDeviceDetails(): void {
    this.deviceService.devicesGetDeviceByIdGETApiDevicesId(this.id).subscribe(device => {
      this.deviceDetails = device; // Assign the device details to the property
      this.lastUpdated = device.extendedProperties?.find((item: ExtendedPropertyDtoModel) => item.key === "LastStateChange")?.value || "";
    });
  }
}
