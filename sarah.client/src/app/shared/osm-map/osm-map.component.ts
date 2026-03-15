import { Component, Input, OnDestroy, AfterViewInit, ElementRef, ViewChild, ViewEncapsulation } from '@angular/core';
import * as L from 'leaflet';
import { NamedLocationDto } from '../../models/api-types';
import { LoggingService } from '../../services/logging.service';

// Fix Leaflet's broken default icon path when bundled with webpack/Angular
const iconRetinaUrl = 'assets/leaflet/marker-icon-2x.png';
const iconUrl = 'assets/leaflet/marker-icon.png';
const shadowUrl = 'assets/leaflet/marker-shadow.png';
const DefaultIcon = L.icon({ iconRetinaUrl, iconUrl, shadowUrl, iconSize: [25, 41], iconAnchor: [12, 41] });
L.Marker.mergeOptions({ icon: DefaultIcon });

@Component({
  selector: 'app-osm-map',
  templateUrl: './osm-map.component.html',
  styleUrls: ['./osm-map.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class OsmMapComponent implements AfterViewInit, OnDestroy {
  @Input() latitude: number = 0;
  @Input() longitude: number = 0;
  @ViewChild('mapContainer') mapContainer!: ElementRef;

  private map: L.Map | null = null;
  private personMarker: L.Marker | null = null;

  constructor(private logger: LoggingService) {}

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  private initMap(): void {
    this.map = L.map(this.mapContainer.nativeElement, {
      center: [this.latitude || 51.5, this.longitude || 10],
      zoom: 6,
      zoomControl: true
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      maxZoom: 19
    }).addTo(this.map);

    this.logger.debug('OSM map initialised');
  }

  /** Places a permanent home marker in green. Call once on initialisation. */
  public AddHomePushPin(location: NamedLocationDto): void {
    if (!this.map) return;

    const homeIcon = L.divIcon({
      className: '',
      html: `
        <div class="osm-home-pin">
          <div class="osm-home-pin__head"></div>
          <div class="osm-home-pin__needle"></div>
        </div>`,
      iconSize: [28, 42],
      iconAnchor: [14, 42],
      popupAnchor: [0, -44]
    });

    L.marker([location.latitude ?? 0, location.longitude ?? 0], { icon: homeIcon })
      .addTo(this.map)
      .bindPopup(`<strong>${location.name ?? 'Zuhause'}</strong>`);
  }

  public invalidateSize(): void {
    this.map?.invalidateSize();
  }

  public isMapReady(): boolean {
    return this.map !== null;
  }

  public SetCenter(center: NamedLocationDto, zoom: number = 10): void {
    const lat = center.latitude ?? 0;
    const lng = center.longitude ?? 0;
    this.latitude = lat;
    this.longitude = lng;

    if (this.map) {
      this.map.setView([lat, lng], zoom);
      L.marker([lat, lng], { title: center.name ?? 'Center of the map' })
        .addTo(this.map)
        .bindPopup(center.name ?? 'Center of the map');
    }
  }

  public AddPushPin(location: NamedLocationDto, subtitle: string = '', text: string = ''): void {
    if (!this.map) return;

    const lat = location.latitude ?? 0;
    const lng = location.longitude ?? 0;
    const label = [location.name, subtitle, text].filter(Boolean).join(' – ');

    L.marker([lat, lng])
      .addTo(this.map)
      .bindPopup(label || 'Unknown');
  }

  /** Places (or replaces) the tracked person marker with a pin-needle style. Centres the map on the new position. */
  public UpdatePersonPin(location: NamedLocationDto, subtitle: string = '', zoom: number = 14): void {
    if (!this.map) return;

    const lat = location.latitude ?? 0;
    const lng = location.longitude ?? 0;

    if (this.personMarker) {
      this.personMarker.remove();
      this.personMarker = null;
    }

    this.map.setView([lat, lng], zoom);

    const pinIcon = L.divIcon({
      className: '',
      html: `
        <div class="osm-person-pin">
          <div class="osm-person-pin__head"></div>
          <div class="osm-person-pin__needle"></div>
        </div>`,
      iconSize: [28, 42],
      iconAnchor: [14, 42],
      popupAnchor: [0, -44]
    });

    const popupContent = subtitle
      ? `<strong>${location.name ?? 'Person'}</strong><br>${subtitle}`
      : `<strong>${location.name ?? 'Person'}</strong>`;

    this.personMarker = L.marker([lat, lng], { icon: pinIcon })
      .addTo(this.map)
      .bindPopup(popupContent);
  }

  public DrawPolygon(
    vertices: NamedLocationDto[],
    fillColor: string = 'rgba(0, 120, 212, 0.2)',
    strokeColor: string = '#0078d4',
    strokeThickness: number = 2,
    labelText: string = ''
  ): void {
    if (!this.map || vertices.length < 2) return;

    const latlngs: L.LatLngTuple[] = vertices.map(v => [v.latitude ?? 0, v.longitude ?? 0]);

    const polygon = L.polygon(latlngs, {
      color: strokeColor,
      weight: strokeThickness,
      fillColor: fillColor,
      fillOpacity: 0.3
    }).addTo(this.map);

    if (labelText.trim().length > 0) {
      polygon.bindTooltip(labelText, {
        permanent: true,
        direction: 'center',
        className: 'osm-geofence-label'
      });
    }
  }

  public ClearMap(): void {
    if (!this.map) return;
    this.map.eachLayer(layer => {
      if (!(layer instanceof L.TileLayer)) {
        layer.remove();
      }
    });
    this.personMarker = null;
  }
}
