import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { RoomDto, RoomsService } from '../../../../services/api-client';

@Component({
  selector: 'app-roomdetails',
  templateUrl: './roomdetails.component.html',
  styleUrls: ['./roomdetails.component.css']
})
export class RoomdetailsComponent implements OnInit {
  @Input() id: number = 0;
  roomDetails: RoomDto | null = null;
  lastUpdated : string = "";

  constructor(private roomsService: RoomsService) {}

  ngOnInit(): void {
    this.loadRoomDetails();
  }


  loadRoomDetails(): void {
    if (this.id) {
      this.roomsService.apiRoomsIdGet(this.id).subscribe(room => {
        this.roomDetails = room; // Assign the room details to the property
        this.lastUpdated = new Date().toLocaleString('de-DE');
      });
    }
  }
}
