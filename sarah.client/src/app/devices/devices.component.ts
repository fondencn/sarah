import { Component, OnInit } from '@angular/core';
import { DevicesService, NetworkElementDto } from '../services/api-client'; // Import the generated client
import { DialogService } from '../services/dialog.service';


@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.css']
})
export class DevicesComponent implements OnInit {



  devices: NetworkElementDto[] = []; // Member variable to store the devices list
  isLoading: boolean = false; // Member variable to store the loading state

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
      next: (response: NetworkElementDto[]) => {
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
  }

  public onDeviceAdded(newDevice: NetworkElementDto) {
    this.devices.push(newDevice);
  }



  public editDevice(device: NetworkElementDto) {
    throw new Error('Method not implemented.');
  }

  public deleteDevice(device: NetworkElementDto) {
    throw new Error('Method not implemented.');
  }
}
