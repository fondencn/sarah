import { Component, Input } from '@angular/core';
import { PersonDto, PersonsService } from '../../../../services/api-client';

@Component({
  selector: 'app-persondetails',
  templateUrl: './persondetails.component.html',
  styleUrl: './persondetails.component.css'
})
export class PersondetailsComponent {
  @Input() id: number = 0;

  personDetails: PersonDto | null = null;
    lastUpdated : string = "";
  
    constructor(private personsService: PersonsService) {}
  
    ngOnInit(): void {
      this.loadPersonDetails();
    }
  
  
    loadPersonDetails(): void {
      if (this.id) {
        this.personsService.apiPersonsIdGet(this.id).subscribe(person => {
          this.personDetails = person; // Assign the room details to the property
          this.lastUpdated = new Date().toLocaleString('de-DE');
        });
      }
    }
}
