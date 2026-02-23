import { Component } from '@angular/core';
import { DialogContent } from '../../services/dialogcontent';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RoomDto } from '../../services/api-client';
import { DialogService } from '../../services/dialog.service';


@Component({
  selector: 'editRoomModal',
  templateUrl: './edit-room-modal.component.html',
  styleUrl: './edit-room-modal.component.css'
})
export class EditRoomModalComponent extends DialogContent {
  roomForm: FormGroup;
  private _room: RoomDto | null = null;
  okButtonText : string = "Save";
  private _isNewRoom: boolean = false;

  public get isNewRoom(): boolean {
    return this._isNewRoom;
  }

  public set isNewRoom(value: boolean) {
    this._isNewRoom = value;
  }


  public get dataContext(): RoomDto | null {
    this._room = this.roomForm.valid ? this.roomForm.value : null;
    return this._room;
  }

  public set dataContext(room: RoomDto | null) {
    this._room = room;
    if (room) {
      this.roomForm.patchValue({
        name: room.name,
        id : room.id
      });
    }
  }

  

  constructor(private fb: FormBuilder, dialogService: DialogService) {
    super(dialogService);

    this.roomForm = this.fb.group({
      name: ['', Validators.required],
      id: [0]
    });
  }


  protected override canOk(): boolean {
    return this.roomForm.valid;
  }
}
