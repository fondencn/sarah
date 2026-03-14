import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { RoomsClient } from '../services/api/room-service/api/rooms.service';
import { RoomDtoModel as RoomDto } from '../services/api/room-service/model/models';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';
import { Subscription } from 'rxjs';
import { EditRoomModalComponent } from './edit-room-modal/edit-room-modal.component';

@Component({
  selector: 'app-rooms',
  templateUrl: './rooms.component.html',
  styleUrls: ['./rooms.component.css']
})
export class RoomsComponent implements OnInit,OnDestroy {

  constructor(private roomsService: RoomsClient, private dialogService: DialogService) { }


  rooms: RoomDto[] = [];
  isLoading: boolean = false;
  @ViewChild(EditRoomModalComponent) editRoomModal!: EditRoomModalComponent;
  private dialogClosedSubscription: Subscription | null = null;


  ngOnInit(): void {
    this.retrieveRooms();

    this.dialogClosedSubscription = this.dialogService.dialogClosed.subscribe((e: DialogClosedEventArgs) => {
      this.onDialogClosed(e);
    });
  }
  ngOnDestroy(): void {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
      this.dialogClosedSubscription = null;
    }
  }






  private onDialogClosed(e: DialogClosedEventArgs) {
    if (e.success && e.dialogId === 'editRoomModal') {
      if (this.editRoomModal.isNewRoom) {
        this.onRoomAdded(e.success);
      } else {
        this.onRoomEdited(e.success);
      }
    }
  }



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


  public deleteRoom(room: RoomDto) {
    this.dialogService.showConfirmDialog('Are you sure you want to delete this room?', 'Confirm Deletion')
    .then((result: boolean) => {
      if (result) {
        this.roomsService.apiRoomsIdDelete(room.id as number).subscribe({
          next: () => {
            this.rooms = this.rooms.filter(d => d.id !== room.id);
          },
          error: (error) => {
            console.error('Error deleting room:', error);
          }
        });
      }
    }).catch((error) => {
      console.error('Error showing confirm dialog:', error);
    });
  }

  public editRoom(room: RoomDto) {
    this.editRoomModal.dataContext = room;
    this.editRoomModal.isNewRoom = false;
    this.editRoomModal.okButtonText = 'Save changes';
    this.dialogService.showDialog('editRoomModal');
  }

  public onRoomEdited(success: boolean) {
    if (success) {
      var roomDto = this.editRoomModal.dataContext as RoomDto;
      this.roomsService.apiRoomsIdPut(roomDto.id as number, roomDto).subscribe({
        next: (response: RoomDto) => {
          const index = this.rooms.findIndex(d => d.id === response.id);
          this.rooms[index] = response;
        },
        error: (error) => {
          console.error('Error editing device:', error);
        }
      });
    }
  }


  public addRoom() {
    var newRoom = {} as RoomDto;
    newRoom.name = '';
    newRoom.id = 0;

    this.editRoomModal.dataContext = newRoom;
    this.editRoomModal.isNewRoom = true;
    this.editRoomModal.okButtonText = 'Add room';

    this.dialogService.showDialog('editRoomModal');
  }

  public onRoomAdded(success: boolean) {
    if (success) {
      let addedRoom: RoomDto = this.editRoomModal.dataContext as RoomDto;
      this.roomsService.apiRoomsPost(addedRoom).subscribe({
        next: (response: RoomDto) => {
          this.rooms.push(response);
        },
        error: (error) => {
          console.error('Error adding room:', error);
        }
      });
    }
  }



  public setFavourite(room: RoomDto, isFavourite: boolean) {
    this.roomsService.apiRoomsIdFavouriteIsFavouritePut((room.id as number), isFavourite).subscribe({
      next: () => {
        room.isFavourite = isFavourite;
      },
      error: (error) => {
        console.error('Error setting favourite state:', error);
      }
    });
  }
}

