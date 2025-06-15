import { Component, Input, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { LocationService, NamedLocationDto, PersonDto, PersonsService } from '../../../../services/api-client';
import { BingMapComponent } from '../../../../shared/bing-map/bing-map.component';

@Component({
  selector: 'app-persondetails',
  templateUrl: './persondetails.component.html',
  styleUrl: './persondetails.component.css'
})
export class PersondetailsComponent implements OnDestroy {
  @Input() id: number = 0;

  personDetails: PersonDto | null = null;
  lastUpdated: string = "";
  personlatitude: number = 0;
  personlongitude: number = 0;
  refreshInterval: any | null = null;
  zuhause: NamedLocationDto | null = null;
  @ViewChild('map') mapElement: BingMapComponent | null = null;

  constructor(private personsService: PersonsService, private locationService: LocationService) { }

  ngOnInit(): void {
    this.loadPersonDetails();
    this.startLocationRefresh();
  }

  ngOnDestroy(): void {
    this.stopLocationRefresh();
  }

  startLocationRefresh(): void {
    this.refreshInterval = setInterval(() => {
      this.refreshLocation();
    }, 5000); // Refresh every 5 seconds
  }

  stopLocationRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  refreshLocation(): void {
    if (this.personDetails?.gpsTrackerID) {
      this.locationService.apiLocationTrackerIdGet(this.personDetails.gpsTrackerID).subscribe(location => {
        this.personlatitude = location?.longitude ?? 0;
        this.personlongitude = location?.latitude ?? 0;
        this.lastUpdated = new Date().toLocaleString('de-DE');
      });
    }
  }

  centerHomeOnMap(): void {
    if (this.zuhause && this.mapElement) {
      this.mapElement.SetCenter(this.zuhause);
    } else if (!this.mapElement) {
      console.error('No map element found');
    } else {
      console.error('No home location found');
    }
  }

  loadPersonDetails(): void {
    if (this.id) {
      this.personsService.apiPersonsIdGet(this.id).subscribe(person => {
        this.personDetails = person;
        this.lastUpdated = new Date().toLocaleString('de-DE');
      });
      this.locationService.apiLocationWellknownlocationsGet().subscribe(locations => {
        var isarray: boolean = Array.isArray(locations);
        if (isarray && locations.length > 0) {
          this.zuhause = locations[0];
          if (this.zuhause && this.mapElement) {
            this.mapElement?.SetCenter(this.zuhause);
          }
        }
      });
    }
  }
}
