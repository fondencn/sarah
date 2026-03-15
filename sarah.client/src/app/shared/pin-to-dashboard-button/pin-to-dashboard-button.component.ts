import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pin-to-dashboard-button',
  templateUrl: './pin-to-dashboard-button.component.html'
})
export class PinToDashboardButtonComponent {
  @Input() isPinned: boolean = false;
  @Output() pinClicked = new EventEmitter<void>();

  onClicked(): void {
    this.pinClicked.emit();
  }
}
