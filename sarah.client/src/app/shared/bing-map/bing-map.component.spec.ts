import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BingMapComponent } from './bing-map.component';

describe('BingMapComponent', () => {
  let component: BingMapComponent;
  let fixture: ComponentFixture<BingMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [BingMapComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BingMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
