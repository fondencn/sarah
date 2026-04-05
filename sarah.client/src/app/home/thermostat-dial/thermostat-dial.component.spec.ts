import { ComponentFixture, discardPeriodicTasks, fakeAsync, TestBed, tick } from '@angular/core/testing';

import { ThermostatDialComponent } from './thermostat-dial.component';

describe('ThermostatDialComponent', () => {
  let component: ThermostatDialComponent;
  let fixture: ComponentFixture<ThermostatDialComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ThermostatDialComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ThermostatDialComponent);
    component = fixture.componentInstance;
    component.itemId = 1;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not emit temperatureSet immediately on slider input', fakeAsync(() => {
    const emitted: { itemId: number; temperature: number }[] = [];
    component.temperatureSet.subscribe(e => emitted.push(e));

    component.onSliderInput('22');

    tick(component.debounceMs - 1);
    expect(emitted.length).toBe(0);

    discardPeriodicTasks();
  }));

  it('should emit temperatureSet after debounce delay on slider input', fakeAsync(() => {
    const emitted: { itemId: number; temperature: number }[] = [];
    component.temperatureSet.subscribe(e => emitted.push(e));

    component.onSliderInput('22');

    tick(component.debounceMs);
    expect(emitted.length).toBe(1);
    expect(emitted[0].temperature).toBe(22);
  }));

  it('should emit only once when slider is moved multiple times within debounce window', fakeAsync(() => {
    const emitted: { itemId: number; temperature: number }[] = [];
    component.temperatureSet.subscribe(e => emitted.push(e));

    component.onSliderInput('20');
    tick(100);
    component.onSliderInput('21');
    tick(100);
    component.onSliderInput('22');

    tick(component.debounceMs);
    expect(emitted.length).toBe(1);
    expect(emitted[0].temperature).toBe(22);
  }));

  it('should emit only once when +/- buttons are clicked rapidly within debounce window', fakeAsync(() => {
    const emitted: { itemId: number; temperature: number }[] = [];
    component.editableSetpoint = 20;
    component.temperatureSet.subscribe(e => emitted.push(e));

    component.increase();
    tick(100);
    component.increase();
    tick(100);
    component.increase();

    tick(component.debounceMs);
    expect(emitted.length).toBe(1);
    expect(emitted[0].temperature).toBe(21.5);
  }));

  it('should emit immediately when applySetpoint is called', fakeAsync(() => {
    const emitted: { itemId: number; temperature: number }[] = [];
    component.editableSetpoint = 21;
    component.temperatureSet.subscribe(e => emitted.push(e));

    component.applySetpoint();

    tick(0);
    expect(emitted.length).toBe(1);
    expect(emitted[0].temperature).toBe(21);
  }));

  describe('dialColor', () => {
    it('returns blue at minimum temperature', () => {
      component.editableSetpoint = component.minTemperature;
      expect(component.dialColor).toBe('rgb(31, 77, 255)');
    });

    it('returns red at maximum temperature', () => {
      component.editableSetpoint = component.maxTemperature;
      expect(component.dialColor).toBe('rgb(255, 47, 47)');
    });

    it('returns an intermediate color at midpoint temperature', () => {
      component.editableSetpoint = (component.minTemperature + component.maxTemperature) / 2;
      // At t=0.5: r=143, g=62, b=151
      expect(component.dialColor).toBe('rgb(143, 62, 151)');
    });

    it('clamps to blue below minimum', () => {
      component.editableSetpoint = component.minTemperature - 10;
      expect(component.dialColor).toBe('rgb(31, 77, 255)');
    });

    it('clamps to red above maximum', () => {
      component.editableSetpoint = component.maxTemperature + 10;
      expect(component.dialColor).toBe('rgb(255, 47, 47)');
    });
  });
});
