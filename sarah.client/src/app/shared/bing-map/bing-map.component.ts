import { Component, Input, OnInit, AfterViewInit } from '@angular/core';
import { NamedLocationDto } from '../../services/api-client';

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


  ngOnInit(): void { }

  ngAfterViewInit(): void {
    this.loadMap();
  }

  loadMap(): void {

    this.map = new Microsoft.Maps.Map(document.getElementById('myMap')!, {
      center: new Microsoft.Maps.Location(this.latitude, this.longitude),
      zoom: this.zoom
    });
  }

  public SetCenter(center: NamedLocationDto, zoom: number): void {
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
        // subTitle: 'Subtitle',
        // text: '1'
      });

      this.map.entities.push(pin);

    }
  }
}