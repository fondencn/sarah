import { Component, Input, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { PersonsClient } from '../../../../services/api/persons-service/api/api';
import { LocationRuntimeService } from '../../../../services/location-runtime.service';
import { GeofencesClient } from '../../../../services/api/geofences-service/api/geofences.service';
import { DevicesClient } from '../../../../services/api/device-service/api/api';
import { NamedLocationDto, PersonDto } from '../../../../models/api-types';
import { BingMapComponent } from '../../../../shared/bing-map/bing-map.component';

interface GeoFencePoint {
  latitude?: { value: number };
  longtitude?: { value: number }; // Note: 'longtitude' matches the backend LocatorPosition property name (typo inherited from backend)
}

interface GeoFenceData {
  name?: string;
  points?: GeoFencePoint[];
}

interface GpsTrackerData {
  battery?: { value: number; unit: string };
}

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
  batteryStatus: string = "";
  refreshInterval: any | null = null;
  zuhause: NamedLocationDto | null = null;
  geofencesLoaded: boolean = false;
  @ViewChild('map') mapElement: BingMapComponent | null = null;

  constructor(
    private personsService: PersonsClient,
    private locationService: LocationRuntimeService,
    private geofencesService: GeofencesClient,
    private devicesService: DevicesClient
  ) { }

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
        this.updatePersonPinOnMap(location?.latitude ?? 0, location?.longitude ?? 0);
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

  private loadGeofencesOnMap(): void {
    if (this.geofencesLoaded || !this.mapElement) return;

    this.geofencesService.apiGeofencesGet().subscribe({
      next: (data: any) => {
        const geofences: GeoFenceData[] = data as GeoFenceData[];
        if (Array.isArray(geofences)) {
          for (const gf of geofences) {
            if (gf.points && gf.points.length > 0) {
              const vertices: NamedLocationDto[] = gf.points
                .filter(p => p.latitude?.value !== undefined && p.longtitude?.value !== undefined)
                .map(p => ({
                  latitude: p.latitude?.value,
                  longitude: p.longtitude?.value,
                  name: gf.name ?? ''
                }));
              if (vertices.length > 0) {
                this.mapElement?.DrawPolygon(vertices, 'rgba(0, 120, 212, 0.2)', '#0078d4', 2);
              }
            }
          }
          this.geofencesLoaded = true;
        }
      },
      error: (err) => console.error('Error loading geofences:', err)
    });
  }

  private updatePersonPinOnMap(latitude: number, longitude: number): void {
    if (!this.mapElement || (!latitude && !longitude)) return;

    const personLocation: NamedLocationDto = {
      latitude: latitude,
      longitude: longitude,
      name: this.personDetails?.name ?? 'Person'
    };
    this.mapElement.UpdatePersonPin(personLocation, this.batteryStatus);
  }

  private loadBatteryStatus(): void {
    const trackerId = this.personDetails?.gpsTrackerID;
    if (!trackerId) return;

    this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(trackerId).subscribe({
      next: (data: any) => {
        const tracker = data as GpsTrackerData;
        const battery = tracker?.battery;
        this.batteryStatus = battery
          ? `Battery: ${Math.round(battery.value)}${battery.unit ?? '%'}`
          : '';
      },
      error: () => { this.batteryStatus = ''; }
    });
  }

  private scheduleGeofenceLoading(retries: number = 10, delayMs: number = 200): void {
    if (!this.mapElement) {
      if (retries <= 0) {
        return;
      }

      setTimeout(() => this.scheduleGeofenceLoading(retries - 1, delayMs), delayMs);
      return;
    }

    this.loadGeofencesOnMap();
  }

  loadPersonDetails(): void {
    if (this.id) {
      this.personsService.apiPersonsIdGet(this.id).subscribe(person => {
        this.personDetails = person as PersonDto;
        this.lastUpdated = new Date().toLocaleString('de-DE');
        this.loadBatteryStatus();
        this.scheduleGeofenceLoading();
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

