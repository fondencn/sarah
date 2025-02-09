import { Component, OnInit } from '@angular/core';
import { RoomDto, RoomsService } from '../services/api-client';

@Component({
  selector: 'app-rooms',
  templateUrl: './rooms.component.html',
  styleUrls: ['./rooms.component.css']
})
export class RoomsComponent implements OnInit {

  ngOnInit(): void {
    this.retrieveRooms();
  }

  rooms: RoomDto[] = [];
  isLoading: boolean = false;


  constructor(private roomsService: RoomsService) { }


  public retrieveRooms(): void {
    this.isLoading = true; // Set the loading state to true
    this.roomsService.apiRoomsGet().subscribe({
      next: (response: RoomDto[]) => {
        this.rooms = response; // Save the devices list in the member variable
      },
      error: (error) => {
        console.error('Error fetching rooms:', error);
      },
      complete: () => {
        this.isLoading = false; // Set the loading state to false
      }
    });
  }


  public setFavourite(room: RoomDto,isFavourite: boolean) {
    throw new Error('Method not implemented.');
  }
  public deleteRoom(room: RoomDto) {
    throw new Error('Method not implemented.');
  }
  public editRoom(room: RoomDto) {
    throw new Error('Method not implemented.');
  }
  public addRoom() {
    throw new Error('Method not implemented.');
  }
}

