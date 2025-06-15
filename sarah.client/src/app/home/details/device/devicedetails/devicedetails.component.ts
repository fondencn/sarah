import { Component, Input, OnInit } from '@angular/core';
import { DeviceDto, DevicesService } from '../../../../services/api-client';

@Component({
  selector: 'app-devicedetails',
  templateUrl: './devicedetails.component.html',
  styleUrls: ['./devicedetails.component.css']
})
export class DevicedetailsComponent implements OnInit {
  @Input() id: number = 0;
  deviceDetails: DeviceDto | null = null;
  lastUpdated : string = "";

  constructor(private deviceService: DevicesService) {}

  ngOnInit(): void {
    this.loadDeviceDetails();
  }

  loadDeviceDetails(): void {
    this.deviceService.devicesIdGet(this.id).subscribe(device => {
      this.deviceDetails = device; // Assign the device details to the property
      this.lastUpdated = device.extendedProperties?.find(item => item.key === "LastStateChange")?.value || "";
    });
  }
}
