import { Component, Input, OnDestroy, ViewChild } from '@angular/core';
import { Modal } from 'bootstrap';
import { GeofencesClient } from '../../services/api/geofences-service/api/geofences.service';
import { DevicesClient } from '../../services/api/device-service/api/api';
import { PersonsClient } from '../../services/api/persons-service/api/persons.service';
import { PersonResponseDtoModel } from '../../services/api/persons-service/model/personResponseDto';
import { OsmMapComponent } from '../../shared/osm-map/osm-map.component';
import { NamedLocationDto } from '../../models/api-types';
import { LoggingService } from '../../services/logging.service';

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
  selector: 'app-person-map-modal',
  templateUrl: './person-map-modal.component.html',
  styleUrl: './person-map-modal.component.css'
})
export class PersonMapModalComponent implements OnDestroy {
  @Input() personId: number = 0;
  @Input() personName: string = '';
  @Input() gpsTrackerID: number = 0;

  @ViewChild('map') mapElement: OsmMapComponent | null = null;

  isVisible: boolean = false;
  batteryStatus: string = '';
  currentGeoFenceName: string = '';
  personLatitude: number = 0;
  personLongitude: number = 0;
  lastRefresh: Date | null = null;
  private bootstrapModal: any = null;
  private refreshInterval: any = null;
  private readonly MAP_INIT_MAX_RETRIES: number = 20;

  constructor(
    private personsService: PersonsClient,
    private geofencesService: GeofencesClient,
    private devicesService: DevicesClient,
    private logger: LoggingService
  ) {}

  ngOnDestroy(): void {
    this.stopRefresh();
  }

  show(): void {
    this.isVisible = true;
    this.loadPersonContext();
    setTimeout(() => {
      const modalEl = document.getElementById('personMapModal');
      if (modalEl) {
        this.bootstrapModal = new Modal(modalEl);
        this.bootstrapModal.show();
        modalEl.addEventListener('shown.bs.modal', () => {
          this.mapElement?.invalidateSize();
        }, { once: true });
        modalEl.addEventListener('hidden.bs.modal', () => {
          this.isVisible = false;
          this.stopRefresh();
          this.bootstrapModal = null;
        }, { once: true });
        this.scheduleLoadMapData();
      }
    });
  }

  hide(): void {
    if (this.bootstrapModal) {
      this.bootstrapModal.hide();
    }
  }

  private scheduleLoadMapData(retry: number = 0): void {
    if (!this.isVisible) return;

    const mapIsReady =
      this.mapElement &&
      (typeof (this.mapElement as any).isMapReady === 'function'
        ? (this.mapElement as any).isMapReady()
        : true);

    if (mapIsReady) {
      this.loadMapData();
      return;
    }

    if (retry >= this.MAP_INIT_MAX_RETRIES) {
      this.logger.warn('Map did not report ready state in time; loading data anyway.');
      this.loadMapData();
      return;
    }

    setTimeout(() => this.scheduleLoadMapData(retry + 1), 100);
  }

  private loadMapData(): void {
    if (!this.mapElement) return;

    this.mapElement.ClearMap();

    // Load geofence polygons + Zuhause home pin once from the geofences service
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
                this.mapElement?.DrawPolygon(vertices, 'rgba(0, 120, 212, 0.68)', '#0078d4', 2, gf.name ?? '');
              }
            }
          }
        }
        this.loadHomePinAndTrackerPosition();
      },
      error: (err) => {
        this.logger.error('Error loading geofences for map:', err);
        this.loadHomePinAndTrackerPosition();
      }
    });

    this.startRefresh();
  }

  private loadPersonContext(): void {
    if (!this.personId) {
      return;
    }

    this.personsService.apiPersonsIdGet(this.personId).subscribe({
      next: (person: PersonResponseDtoModel) => {
        this.currentGeoFenceName = person.currentGeoFence?.trim() ?? 'Not at known location';
        if (person.name?.trim()) {
          this.personName = person.name;
        }
        if (person.gpsTrackerID) {
          this.gpsTrackerID = person.gpsTrackerID;
        }
      },
      error: (err) => this.logger.error('Error loading person details for map modal:', err)
    });
  }

  private loadHomePinAndTrackerPosition(): void {
    // Place Zuhause home pin once
    this.geofencesService.apiGeofencesWellknownlocationsGet().subscribe({
      next: (locations: any) => {
        if (Array.isArray(locations) && locations.length > 0) {
          this.mapElement?.AddHomePushPin(locations[0]);
        }
      },
      error: (err) => this.logger.error('Error loading well-known locations:', err)
    });
    this.pollTrackerPosition();
  }

  private pollTrackerPosition(): void {
    this.lastRefresh = new Date();

    if (!this.gpsTrackerID) return;

    this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(this.gpsTrackerID).subscribe({
      next: (data: any) => {
        const tracker = data as TrackerDto;
        const pos = tracker?.position;
        if (!pos?.isValid || (pos.latitude === 0 && pos.longitude === 0)) return;

        this.batteryStatus = tracker.batteryLevel != null
          ? `Battery: ${Math.round(tracker.batteryLevel)}%`
          : '';
        this.personLatitude = pos.latitude ?? 0;
        this.personLongitude = pos.longitude ?? 0;
        

        this.mapElement?.UpdatePersonPin(
          { latitude: pos.latitude, longitude: pos.longitude, name: this.personName },
          this.batteryStatus,
          14
        );
      },
      error: (err) => this.logger.error('Error loading person position:', err)
    });
  }

  private startRefresh(): void {
    if (this.refreshInterval) return;

    // Only the tracker position is polled; geofences are static and loaded once
    this.refreshInterval = setInterval(() => {
      if (this.mapElement && this.isVisible) {
        this.pollTrackerPosition();
      }
    }, 60000);
  }

  private stopRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }
}
