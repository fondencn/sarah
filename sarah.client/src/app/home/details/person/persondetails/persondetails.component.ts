import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-persondetails',
  templateUrl: './persondetails.component.html',
  styleUrl: './persondetails.component.css'
})
export class PersondetailsComponent {
  @Input() id: number = 0;
}
