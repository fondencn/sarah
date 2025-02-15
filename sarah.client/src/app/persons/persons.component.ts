import { Component, OnInit } from '@angular/core';
import { PersonDto, PersonsService } from '../services/api-client';

@Component({
  selector: 'app-persons',
  templateUrl: './persons.component.html',
  styleUrl: './persons.component.css'
})
export class PersonsComponent implements OnInit {

  ngOnInit():  void {
    this.retrievePersons();
  }

  persons: PersonDto[] = [];
  isLoading: boolean = false;


  constructor(private personsService: PersonsService) { }


  public retrievePersons(): void {
    this.isLoading = true; // Set the loading state to true
    this.personsService.apiPersonsGet().subscribe({
      next: (response: PersonDto[]) => {
        this.persons = response; // Save the devices list in the member variable
      },
      error: (error) => {
        console.error('Error fetching persons:', error);
      },
      complete: () => {
        this.isLoading = false; // Set the loading state to false
      }
    });
  }


  public setFavourite(room: PersonDto,isFavourite: boolean) {
    throw new Error('Method not implemented.');
  }
  public deletePerson(room: PersonDto) {
    throw new Error('Method not implemented.');
  }
  public editPerson(room: PersonDto) {
    throw new Error('Method not implemented.');
  }
  public addPerson() {
    throw new Error('Method not implemented.');
  }
}
