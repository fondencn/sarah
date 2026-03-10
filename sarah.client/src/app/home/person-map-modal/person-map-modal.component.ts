import { Component, Input, OnDestroy, ViewChild } from '@angular/core';
import { LocationRuntimeService } from '../../services/location-runtime.service';
import { GeofencesClient } from '../../services/api/geofences-service/api/geofences.service';
import { DevicesClient } from '../../services/api/device-service/api/api';
import { BingMapComponent } from '../../shared/bing-map/bing-map.component';
import { NamedLocationDto } from '../../models/api-types';

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
  position?: { latitude?: { value: number }; longtitude?: { value: number }; isValid?: boolean }; // Note: 'longtitude' matches the backend API typo
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

  @ViewChild('map') mapElement: BingMapComponent | null = null;

  isVisible: boolean = false;
  batteryStatus: string = '';
  currentGeoFenceName: string = '';
  private bootstrapModal: any = null;
  private refreshInterval: any = null;

  constructor(
    private locationService: LocationRuntimeService,
    private geofencesService: GeofencesClient,
    private devicesService: DevicesClient
  ) {}

  ngOnDestroy(): void {
    this.stopRefresh();
  }

  show(): void {
    this.isVisible = true;
    setTimeout(() => {
      const modalEl = document.getElementById('personMapModal');
      if (modalEl) {
        this.bootstrapModal = new (window as any).bootstrap.Modal(modalEl);
        this.bootstrapModal.show();
        modalEl.addEventListener('hidden.bs.modal', () => {
          this.isVisible = false;
          this.stopRefresh();
          this.bootstrapModal = null;
        }, { once: true });
        setTimeout(() => this.loadMapData(), 300);
      }
    });
  }

  hide(): void {
    if (this.bootstrapModal) {
      this.bootstrapModal.hide();
    }
  }

  private loadMapData(): void {
    if (!this.mapElement) return;

    this.mapElement.ClearMap();

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
        }
        this.loadPersonPosition();
      },
      error: (err) => {
        console.error('Error loading geofences for map:', err);
        this.loadPersonPosition();
      }
    });

    this.startRefresh();
  }

  private loadPersonPosition(): void {
    if (!this.gpsTrackerID) return;

    this.locationService.apiLocationTrackerIdGet(this.gpsTrackerID).subscribe({
      next: (location) => {
        if (!location?.latitude && !location?.longitude) return;

        if (this.gpsTrackerID) {
          this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(this.gpsTrackerID).subscribe({
            next: (trackerData: any) => {
              const tracker = trackerData as GpsTrackerData;
              const battery = tracker?.battery;
              this.batteryStatus = battery
                ? `Battery: ${Math.round(battery.value)}${battery.unit ?? '%'}`
                : '';
              this.placePersonPin(location.latitude ?? 0, location.longitude ?? 0);
            },
            error: () => {
              this.batteryStatus = '';
              this.placePersonPin(location.latitude ?? 0, location.longitude ?? 0);
            }
          });
        } else {
          this.placePersonPin(location.latitude ?? 0, location.longitude ?? 0);
        }
      },
      error: (err) => console.error('Error loading person position:', err)
    });
  }

  private placePersonPin(latitude: number, longitude: number): void {
    if (!this.mapElement) return;

    const personLocation: NamedLocationDto = {
      latitude: latitude,
      longitude: longitude,
      name: this.personName
    };

    this.mapElement.UpdatePersonPin(personLocation, this.batteryStatus, 14);
  }

  private startRefresh(): void {
    // Only refresh the person's position on the interval; geofences are static and loaded once
    this.refreshInterval = setInterval(() => {
      if (this.mapElement && this.isVisible) {
        this.refreshPersonPosition();
      }
    }, 10000);
  }

  private refreshPersonPosition(): void {
    if (!this.gpsTrackerID || !this.mapElement) return;

    this.locationService.apiLocationTrackerIdGet(this.gpsTrackerID).subscribe({
      next: (location) => {
        if (!location?.latitude && !location?.longitude) return;
        this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(this.gpsTrackerID).subscribe({
          next: (trackerData: any) => {
            const tracker = trackerData as GpsTrackerData;
            const battery = tracker?.battery;
            this.batteryStatus = battery
              ? `Battery: ${Math.round(battery.value)}${battery.unit ?? '%'}`
              : '';
            this.placePersonPin(location.latitude ?? 0, location.longitude ?? 0);
          },
          error: () => {
            this.placePersonPin(location.latitude ?? 0, location.longitude ?? 0);
          }
        });
      },
      error: (err) => console.error('Error refreshing person position:', err)
    });
  }

  private stopRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }
}
