import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { EditRoomModalComponent } from './edit-room-modal.component';
import { DialogService } from '../../services/dialog.service';

describe('EditRoomModalComponent', () => {
  let component: EditRoomModalComponent;
  let fixture: ComponentFixture<EditRoomModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [EditRoomModalComponent],
      imports: [ReactiveFormsModule, TranslateModule.forRoot()],
      providers: [{ provide: DialogService, useValue: {} }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditRoomModalComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
