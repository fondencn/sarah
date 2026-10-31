import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { RoomsClient } from '../services/api/room-service/api/rooms.service';
import { RoomDtoModel as RoomDto } from '../services/api/room-service/model/models';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';
import { forkJoin, Subscription } from 'rxjs';
import { EditRoomModalComponent } from './edit-room-modal/edit-room-modal.component';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { CreateDashboardItemDto, DashboardItemTypeDto } from '../models/api-types';
import { LoggingService } from '../services/logging.service';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { RoomOrderingService } from './room-ordering.service';

@Component({
  selector: 'app-rooms',
  templateUrl: './rooms.component.html',
  styleUrls: ['./rooms.component.css']
})
export class RoomsComponent implements OnInit,OnDestroy {

  constructor(private roomsService: RoomsClient, private dialogService: DialogService, private dashboardService: DashboardRuntimeService, private logger: LoggingService, private roomOrderingService: RoomOrderingService) { }

  readonly ITEM_TYPE_ROOM: DashboardItemTypeDto = DashboardItemTypeDto.NUMBER_2;


  rooms: RoomDto[] = [];
  isLoading: boolean = false;
  isSavingOrder = false;
  orderSaveError: string | null = null;
  private lastSavedRoomIds: number[] = [];
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
    this.isLoading = true;
    forkJoin({
      rooms: this.roomsService.apiRoomsGet(),
      dashboardItems: this.dashboardService.apiDashboardGet()
    }).subscribe({
      next: ({ rooms, dashboardItems }) => {
        this.rooms = rooms;
        this.lastSavedRoomIds = this.rooms.map(room => room.id as number);
        this.orderSaveError = null;
        this.pinnedRoomIds = new Set(
          dashboardItems
            .filter(item => item.itemType === DashboardItemTypeDto.NUMBER_2)
            .map(item => item.itemId as number)
        );
      },
      error: (error) => {
        this.logger.error('Error fetching rooms:', error);
        this.isLoading = false;
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }


  public onRoomDrop(event: CdkDragDrop<RoomDto[]>): void {
    if (this.rooms.length < 2 || this.isSavingOrder || event.previousIndex === event.currentIndex) {
      return;
    }

    moveItemInArray(this.rooms, event.previousIndex, event.currentIndex);
    this.orderSaveError = null;
    this.isSavingOrder = true;
    const requestedOrder = this.rooms.map(room => room.id as number);

    this.roomOrderingService.saveOrder(requestedOrder).subscribe({
      next: () => {
        this.lastSavedRoomIds = requestedOrder;
      },
      error: (error) => {
        const savedIds = new Set(this.lastSavedRoomIds);
        const roomsById = new Map(this.rooms.map(room => [room.id as number, room]));
        const restoredRooms = this.lastSavedRoomIds
          .map(id => roomsById.get(id))
          .filter((room): room is RoomDto => room !== undefined);
        const roomsAddedAfterSnapshot = this.rooms.filter(room => !savedIds.has(room.id as number));
        this.rooms = [...restoredRooms, ...roomsAddedAfterSnapshot];
        this.orderSaveError = "Room order could not be saved. Your previous order has been restored.";
        this.isSavingOrder = false;
        this.logger.error(this.orderSaveError, error);
      },
      complete: () => {
        this.isSavingOrder = false;
      }
    });
  }

  public trackByRoomId(index: number, room: RoomDto): number {
    return room.id ?? index;
  }

  public deleteRoom(room: RoomDto) {
    this.dialogService.showConfirmDialog('Are you sure you want to delete this room?', 'Confirm Deletion')
    .then((result: boolean) => {
      if (result) {
        this.roomsService.apiRoomsIdDelete(room.id as number).subscribe({
          next: () => {
            this.rooms = this.rooms.filter(d => d.id !== room.id);
            this.lastSavedRoomIds = this.rooms.map(currentRoom => currentRoom.id as number);
          },
          error: (error) => {
            this.logger.error('Error deleting room:', error);
          }
        });
      }
    }).catch((error) => {
      this.logger.error('Error showing confirm dialog:', error);
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
          this.logger.error('Error editing room:', error);
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
          this.logger.error('Error adding room:', error);
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
        this.logger.error('Error setting favourite state:', error);
      }
    });
  }

  pinnedRoomIds: Set<number> = new Set<number>();

  public pinToDashboard(room: RoomDto) {
    if (this.pinnedRoomIds.has(room.id as number)) {
      this.dashboardService.apiDashboardItemIdItemTypeDelete(room.id as number, this.ITEM_TYPE_ROOM).subscribe({
        next: () => {
          this.pinnedRoomIds.delete(room.id as number);
        },
        error: (error) => {
          this.logger.error('Error unpinning room from dashboard:', error);
        }
      });
    } else {
      const dto: CreateDashboardItemDto = {
        itemId: room.id as number,
        itemType: this.ITEM_TYPE_ROOM,
        title: room.name,
        description: room.description,
        subtype: 'Room'
      };
      this.dashboardService.apiDashboardPost(dto).subscribe({
        next: () => {
          this.pinnedRoomIds.add(room.id as number);
        },
        error: (error) => {
          this.logger.error('Error pinning room to dashboard:', error);
        }
      });
    }
  }
}

