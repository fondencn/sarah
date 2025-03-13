import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-roomdetails',
  templateUrl: './roomdetails.component.html',
  styleUrl: './roomdetails.component.css'
})
export class RoomdetailsComponent {
  @Input() id: number = 0;
}
