import { Component, Input, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { PersonsClient } from '../../../../services/api/persons-service/api/persons.service';
import { GeofencesClient } from '../../../../services/api/geofences-service/api/geofences.service';
import { DevicesClient } from '../../../../services/api/device-service/api/api';
import { NamedLocationDto, PersonDto } from '../../../../models/api-types';
import { OsmMapComponent } from '../../../../shared/osm-map/osm-map.component';
import { LoggingService } from '../../../../services/logging.service';

interface GeoFencePoint {
  latitude?: { value: number };
  longtitude?: { value: number }; // Note: 'longtitude' matches the backend LocatorPosition property name (typo inherited from backend)
}

interface GeoFenceData {
  name?: string;
  points?: GeoFencePoint[];
}

interface TrackerDto {
  id?: number;
  name?: string;
  position?: { latitude?: number; longitude?: number; isValid?: boolean };
  batteryLevel?: number;
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
  homePinAdded: boolean = false;
  @ViewChild('map') mapElement: OsmMapComponent | null = null;

  constructor(
    private personsService: PersonsClient,
    private geofencesService: GeofencesClient,
    private devicesService: DevicesClient,
    private logger: LoggingService
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
      this.pollTrackerPosition();
    }, 60000);
  }

  stopLocationRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  /** Single call to the device service for both position and battery status. */
  private pollTrackerPosition(): void {
    const trackerId = this.personDetails?.gpsTrackerID;
    if (!trackerId) return;

    this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(trackerId).subscribe({
      next: (data: any) => {
        const tracker = data as TrackerDto;
        const pos = tracker?.position;
        if (!pos?.isValid || (pos.latitude === 0 && pos.longitude === 0)) return;

        this.batteryStatus = tracker.batteryLevel != null
          ? `Battery: ${Math.round(tracker.batteryLevel)}%`
          : '';
        this.personlatitude = pos.latitude ?? 0;
        this.personlongitude = pos.longitude ?? 0;
        this.lastUpdated = new Date().toLocaleString('de-DE');

        if (this.mapElement && (pos.latitude || pos.longitude)) {
          this.mapElement.UpdatePersonPin(
            { latitude: pos.latitude, longitude: pos.longitude, name: this.personDetails?.name ?? 'Person' },
            this.batteryStatus
          );
        }
      },
      error: (err) => this.logger.error('Error polling tracker position:', err)
    });
  }

  centerHomeOnMap(): void {
    if (this.zuhause && this.mapElement) {
      this.mapElement.SetCenter(this.zuhause);
    } else if (!this.mapElement) {
      this.logger.error('No map element found');
    } else {
      this.logger.error('No home location found');
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
                this.mapElement?.DrawPolygon(vertices, 'rgba(0, 120, 212, 0.2)', '#0078d4', 2, gf.name ?? '');
              }
            }
          }
          this.geofencesLoaded = true;
        }
      },
      error: (err) => this.logger.error('Error loading geofences:', err)
    });
  }

  private scheduleGeofenceLoading(retries: number = 10, delayMs: number = 200): void {
    if (!this.mapElement) {
      if (retries <= 0) return;
      setTimeout(() => this.scheduleGeofenceLoading(retries - 1, delayMs), delayMs);
      return;
    }
    this.loadGeofencesOnMap();
  }

  private scheduleHomePinPlacement(retries: number = 10, delayMs: number = 200): void {
    if (this.homePinAdded || !this.zuhause) return;
    if (!this.mapElement) {
      if (retries <= 0) return;
      setTimeout(() => this.scheduleHomePinPlacement(retries - 1, delayMs), delayMs);
      return;
    }
    this.mapElement.AddHomePushPin(this.zuhause);
    this.homePinAdded = true;
  }

  loadPersonDetails(): void {
    if (this.id) {
      this.personsService.apiPersonsIdGet(this.id).subscribe(person => {
        this.personDetails = person as PersonDto;
        this.lastUpdated = new Date().toLocaleString('de-DE');
        this.pollTrackerPosition();
        this.scheduleGeofenceLoading();
      });
      this.geofencesService.apiGeofencesWellknownlocationsGet().subscribe((locations: any) => {
        const isarray: boolean = Array.isArray(locations);
        if (isarray && locations.length > 0) {
          this.zuhause = locations[0];
          if (this.zuhause && this.mapElement) {
            this.mapElement.SetCenter(this.zuhause);
          }
          this.scheduleHomePinPlacement();
        }
      });
    }
  }
}

