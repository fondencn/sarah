import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomedetailsComponent } from './homedetails.component';

describe('HomedetailsComponent', () => {
  let component: HomedetailsComponent;
  let fixture: ComponentFixture<HomedetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [HomedetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HomedetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
