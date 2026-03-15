import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { RoomsClient } from '../../../../services/api/room-service/api/rooms.service';
import { RoomDtoModel as RoomDto } from '../../../../services/api/room-service/model/models';

@Component({
  selector: 'app-roomdetails',
  templateUrl: './roomdetails.component.html',
  styleUrls: ['./roomdetails.component.css']
})
export class RoomdetailsComponent implements OnInit {
  @Input() id: number = 0;
  roomDetails: RoomDto | null = null;
  lastUpdated : string = "";

  constructor(private roomsService: RoomsClient) {}

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
