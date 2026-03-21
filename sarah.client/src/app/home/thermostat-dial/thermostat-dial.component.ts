import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-thermostat-dial',
  templateUrl: './thermostat-dial.component.html',
  styleUrls: ['./thermostat-dial.component.css']
})
export class ThermostatDialComponent implements OnChanges {
  @Input() itemId: number | undefined;
  @Input() temperature: number | null = null;
  @Input() setpoint: number | null = null;
  @Input() battery: number | null = null;

  @Input() minTemperature = 4;
  @Input() maxTemperature = 30;
  @Input() step = 0.5;

  @Output() temperatureSet = new EventEmitter<{ itemId: number; temperature: number }>();

  editableSetpoint = 20;

  readonly cx = 110;
  readonly cy = 110;
  readonly radius = 82;
  readonly arcStartDeg = 135;
  readonly arcSweepDeg = 270;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['setpoint']) {
      const incoming = this.setpoint ?? this.editableSetpoint;
      this.editableSetpoint = this.clampAndRound(incoming);
    }
  }

  get displayTemperature(): number {
    return this.temperature ?? this.editableSetpoint;
  }

  get normalizedValue(): number {
    return (this.editableSetpoint - this.minTemperature) / (this.maxTemperature - this.minTemperature);
  }

  get circumference(): number {
    return 2 * Math.PI * this.radius;
  }

  get arcLength(): number {
    return (this.arcSweepDeg / 360) * this.circumference;
  }

  get trackDasharray(): string {
    return `${this.arcLength} ${this.circumference}`;
  }

  get progressDasharray(): string {
    return `${this.normalizedValue * this.arcLength} ${this.circumference}`;
  }

  get thumbX(): number {
    const angle = this.arcStartDeg + this.normalizedValue * this.arcSweepDeg;
    const radians = (angle * Math.PI) / 180;
    return this.cx + this.radius * Math.cos(radians);
  }

  get thumbY(): number {
    const angle = this.arcStartDeg + this.normalizedValue * this.arcSweepDeg;
    const radians = (angle * Math.PI) / 180;
    return this.cy + this.radius * Math.sin(radians);
  }

  onSliderInput(value: string): void {
    this.editableSetpoint = this.clampAndRound(Number(value));
    this.emitTemperatureSet();
  }

  decrease(): void {
    this.editableSetpoint = this.clampAndRound(this.editableSetpoint - this.step);
    this.emitTemperatureSet();
  }

  increase(): void {
    this.editableSetpoint = this.clampAndRound(this.editableSetpoint + this.step);
    this.emitTemperatureSet();
  }

  applySetpoint(): void {
    this.emitTemperatureSet();
  }

  private emitTemperatureSet(): void {
    if (this.itemId === undefined) {
      return;
    }

    this.temperatureSet.emit({
      itemId: this.itemId,
      temperature: this.editableSetpoint
    });
  }

  private clampAndRound(value: number): number {
    const min = this.minTemperature;
    const max = this.maxTemperature;
    const rounded = Math.round(value / this.step) * this.step;
    return Math.min(max, Math.max(min, rounded));
  }
}
