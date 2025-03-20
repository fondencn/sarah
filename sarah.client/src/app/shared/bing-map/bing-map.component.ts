import { Component, Input, OnInit, AfterViewInit } from '@angular/core';
import { NamedLocationDto } from '../../services/api-client';
import { BingMapsLoaderService } from '../../services/bing-maps-loader.service';

@Component({
  selector: 'app-bing-map',
  templateUrl: './bing-map.component.html',
  styleUrls: ['./bing-map.component.css']
})
export class BingMapComponent implements OnInit, AfterViewInit {
  @Input() latitude: number = 0;
  @Input() longitude: number = 0;
  private zoom: number = 10;

  private map: Microsoft.Maps.Map | null = null;

  constructor(private bingMapsLoader: BingMapsLoaderService) {}

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.bingMapsLoader.load().then(() => {
      this.loadMap();
    }).catch(error => {
      console.error('Error loading Bing Maps API:', error);
    });
  }

  loadMap(): void {
    this.map = new Microsoft.Maps.Map(document.getElementById('myMap')!, {
      center: new Microsoft.Maps.Location(this.latitude, this.longitude),
      zoom: this.zoom,
      mapTypeId: Microsoft.Maps.MapTypeId.aerial
    });
  }

  public SetCenter(center: NamedLocationDto, zoom: number = 10): void {
    this.latitude = center.latitude ?? 0;
    this.longitude = center.longitude ?? 0;
    this.zoom = zoom;

    if (this.map) {
      this.map.setView({
        center: new Microsoft.Maps.Location(this.latitude, this.longitude),
        zoom: this.zoom
      });

      const centerPoint = this.map.getCenter();

      const pin = new Microsoft.Maps.Pushpin(centerPoint, {
        title: center.name ?? "Center of the map"
      });

      this.map.entities.push(pin);
    }
  }

  public AddPushPin(location: NamedLocationDto, subtitle: string = "", text: string = ""): void {
    if (this.map) {
      const pin = new Microsoft.Maps.Pushpin(new Microsoft.Maps.Location(location.latitude ?? 0, location.longitude ?? 0), {
        title: location.name ?? "Unknown",
        subTitle: subtitle,
        text: text
      });

      this.map.entities.push(pin);
    }
  }

  public DrawPolygon(vertices: NamedLocationDto[], fillColor: string = 'rgba(0, 0, 255, 0.5)', strokeColor: string = 'blue', strokeThickness: number = 2): void {
    if (this.map) {
      const locations = vertices.map(vertex => new Microsoft.Maps.Location(vertex.latitude ?? 0, vertex.longitude ?? 0));
      const polygon = new Microsoft.Maps.Polygon(locations, {
        fillColor: fillColor,
        strokeColor: strokeColor,
        strokeThickness: strokeThickness
      });

      this.map.entities.push(polygon);
    }
  }

  public ClearMap(): void {
    if (this.map) {
      this.map.entities.clear();
    }
  }
}