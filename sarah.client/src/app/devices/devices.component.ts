import { Component, OnInit } from '@angular/core';
import { DevicesService, NetworkElementDto } from '../services/api-client'; // Import the generated client

@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.css']
})
export class DevicesComponent implements OnInit {

  devices: NetworkElementDto[] = []; // Member variable to store the devices list

  constructor(private devicesService: DevicesService) { }

  ngOnInit(): void {
    this.onLoad();
  }

  onLoad(): void {
    // Add your logic here that should be executed after the component is loaded
    console.log('DevicesComponent loaded');

    // Call the API to load the devices list
    this.devicesService.devicesGet().subscribe({
      next: (response: NetworkElementDto[]) => {
        this.devices = response; // Save the devices list in the member variable
      },
      error: (error) => {
        console.error('Error fetching devices:', error);
      }
    });
  }
}
