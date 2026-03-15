import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { PersonsClient } from '../services/api/persons-service/api/persons.service';
import { PersonResponseDtoModel, PersonDtoModel } from '../services/api/persons-service/model/models';
import { CreateDashboardItemDto, DashboardItemType, PersonDto } from '../models/api-types';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { Subscription } from 'rxjs';
import { EditPersonModalComponent } from './edit-person-modal/edit-person-modal.component';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';
import { LoggingService } from '../services/logging.service';

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

  persons: PersonResponseDtoModel[] = [];
  isLoading: boolean = false;
  @ViewChild(EditPersonModalComponent) editPersonModal!: EditPersonModalComponent;
  private dialogClosedSubscription: Subscription | null = null;


  constructor(private personsService: PersonsClient, private dialogService: DialogService, private dashboardService: DashboardRuntimeService, private logger: LoggingService) { }


  public retrievePersons(): void {
    this.isLoading = true;
    this.personsService.apiPersonsGet().subscribe({
      next: (response: PersonResponseDtoModel[]) => {
        this.persons = response;
      },
      error: (error) => {
        this.logger.error('Error fetching persons:', error);
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  public deletePerson(person: PersonResponseDtoModel) {
    this.dialogService.showConfirmDialog('Are you sure you want to delete this person?', 'Confirm Deletion')
    .then((result: boolean) => {
      if (result) {
        this.personsService.apiPersonsIdDelete(person.id as number).subscribe({
          next: () => {
            this.persons = this.persons.filter(d => d.id !== person.id);
          },
          error: (error) => {
            this.logger.error('Error deleting person:', error);
          }
        });
      }
    }).catch((error) => {
      this.logger.error('Error showing confirm dialog:', error);
    });
  }

  public editPerson(person: PersonResponseDtoModel) {
    this.editPersonModal.dataContext = this.toPersonDto(person);
    this.editPersonModal.isNewPerson = false;
    this.editPersonModal.okButtonText = 'Save changes';
    this.dialogService.showDialog('editPersonModal');
  }


  public onPersonEdited(success: boolean) {
    if (success) {
      const personDto = this.editPersonModal.dataContext as PersonDto;
      const requestModel: PersonDtoModel = {
        id: personDto.id,
        name: personDto.name,
        mobilePhoneHostname: personDto.mobilePhoneHostname,
        gpsTrackerID: personDto.gpsTrackerID
      };
      this.personsService.apiPersonsIdPut(personDto.id as number, requestModel).subscribe({
        next: (response: PersonResponseDtoModel) => {
          const index = this.persons.findIndex(d => d.id === response.id);
          this.persons[index] = response;
        },
        error: (error) => {
          this.logger.error('Error editing person:', error);
        }
      });
    }
  }


  public addPerson() {
      const newPerson: PersonDto = { name: '', id: 0 };
  
      this.editPersonModal.dataContext = newPerson;
      this.editPersonModal.isNewPerson = true;
      this.editPersonModal.okButtonText = 'Add person';
  
      this.dialogService.showDialog('editPersonModal');
  }


    public onPersonAdded(success: boolean) {
      if (success) {
        const addedPerson = this.editPersonModal.dataContext as PersonDto;
        const requestModel: PersonDtoModel = {
          name: addedPerson.name,
          mobilePhoneHostname: addedPerson.mobilePhoneHostname,
          gpsTrackerID: addedPerson.gpsTrackerID
        };
        this.personsService.apiPersonsPost(requestModel).subscribe({
          next: (response: PersonResponseDtoModel) => {
            this.persons.push(response);
          },
          error: (error) => {
            this.logger.error('Error adding person:', error);
          }
        });
      }
    }
  


  public setFavourite(person: PersonResponseDtoModel, isFavourite: boolean) {
    this.personsService.apiPersonsIdFavouriteIsFavouritePut((person.id as number), isFavourite).subscribe({
      next: () => {
        person.isFavourite = isFavourite;
        if (isFavourite) {
          const createDto: CreateDashboardItemDto = {
            itemId: person.id,
            itemType: DashboardItemType.NUMBER_3,
            title: person.name,
            description: person.isAtHome ? 'At home' : 'Away',
            subtype: 'Person'
          };
          this.dashboardService.apiDashboardPost(createDto).subscribe({
            error: (err) => this.logger.error('Error adding person to dashboard:', err)
          });
        } else {
          this.dashboardService.apiDashboardItemIdItemTypeDelete((person.id as number), DashboardItemType.NUMBER_3).subscribe({
            error: (err) => this.logger.error('Error removing person from dashboard:', err)
          });
        }
      },
      error: (error) => {
        this.logger.error('Error setting favourite state:', error);
      }
    });
  }

  private toPersonDto(person: PersonResponseDtoModel): PersonDto {
    return {
      id: person.id,
      name: person.name,
      mobilePhoneHostname: person.mobilePhoneHostname,
      gpsTrackerID: person.gpsTrackerID,
      gpsTrackerName: person.gpsTrackerName,
      currentGeoFence: person.currentGeoFence,
      currentPosition: person.currentPosition,
      isAtHome: person.isAtHome,
      isFavourite: person.isFavourite
    };
  }
}
