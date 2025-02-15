import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditPersonModalComponent } from './edit-person-modal.component';

describe('EditPersonModalComponent', () => {
  let component: EditPersonModalComponent;
  let fixture: ComponentFixture<EditPersonModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [EditPersonModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EditPersonModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
