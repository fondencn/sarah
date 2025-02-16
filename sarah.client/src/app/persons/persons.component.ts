import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { PersonDto, PersonsService } from '../services/api-client';
import { Subscription } from 'rxjs';
import { EditPersonModalComponent } from './edit-person-modal/edit-person-modal.component';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';

@Component({
  selector: 'app-persons',
  templateUrl: './persons.component.html',
  styleUrl: './persons.component.css'
})
export class PersonsComponent implements OnInit,OnDestroy {

  ngOnInit(): void {
    this.retrievePersons();
    this.dialogClosedSubscription = this.dialogService.dialogClosed.subscribe((e: DialogClosedEventArgs) => {
      this.onDialogClosed(e);
    });
  }
  ngOnDestroy(): void {
    if (this.dialogClosedSubscription) {
      this.dialogClosedSubscription.unsubscribe();
      this.dialogClosedSubscription = null;
    }
  }



  private onDialogClosed(e: DialogClosedEventArgs) {
    if (e.success && e.dialogId === 'editPersonModal') {
      if (this.editPersonModal.isNewPerson) {
        this.onPersonAdded(e.success);
      } else {
        this.onPersonEdited(e.success);
      }
    }
  }

  persons: PersonDto[] = [];
  isLoading: boolean = false;
  @ViewChild(EditPersonModalComponent) editPersonModal!: EditPersonModalComponent;
  private dialogClosedSubscription: Subscription | null = null;


  constructor(private personsService: PersonsService, private dialogService: DialogService) { }


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

  public deletePerson(person: PersonDto) {
    this.dialogService.showConfirmDialog('Are you sure you want to delete this person?', 'Confirm Deletion')
    .then((result: boolean) => {
      if (result) {
        this.personsService.apiPersonsIdDelete(person.id as number).subscribe({
          next: () => {
            this.persons = this.persons.filter(d => d.id !== person.id);
          },
          error: (error) => {
            console.error('Error deleting person:', error);
          }
        });
      }
    }).catch((error) => {
      console.error('Error showing confirm dialog:', error);
    });
  }

  public editPerson(person: PersonDto) {
    this.editPersonModal.dataContext = person;
    this.editPersonModal.isNewPerson = false;
    this.editPersonModal.okButtonText = 'Save changes';
    this.dialogService.showDialog('editPersonModal');
  }


  public onPersonEdited(success: boolean) {
    if (success) {
      var personDto = this.editPersonModal.dataContext as PersonDto;
      this.personsService.apiPersonsIdPut(personDto.id as number, personDto).subscribe({
        next: (response: PersonDto) => {
          const index = this.persons.findIndex(d => d.id === response.id);
          this.persons[index] = response;
        },
        error: (error) => {
          console.error('Error editing person:', error);
        }
      });
    }
  }


  public addPerson() {
      var newPerson = {} as PersonDto;
      newPerson.name = '';
      newPerson.id = 0;
  
      this.editPersonModal.dataContext = newPerson;
      this.editPersonModal.isNewPerson = true;
      this.editPersonModal.okButtonText = 'Add person';
  
      this.dialogService.showDialog('editPersonModal');
  }


    public onPersonAdded(success: boolean) {
      if (success) {
        let addedPerson: PersonDto = this.editPersonModal.dataContext as PersonDto;
        this.personsService.apiPersonsPost(addedPerson).subscribe({
          next: (response: PersonDto) => {
            this.persons.push(response);
          },
          error: (error) => {
            console.error('Error adding room:', error);
          }
        });
      }
    }
  


  public setFavourite(person: PersonDto, isFavourite: boolean) {
    this.personsService.apiPersonsIdFavouriteIsFavouritePut((person.id as number), isFavourite).subscribe({
      next: () => {
        person.isFavourite = isFavourite;
      },
      error: (error) => {
        console.error('Error setting favourite state:', error);
      }
    });
  }
}
