import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { PersonsClient } from '../services/api/persons-service/api/persons.service';
import { PersonResponseDtoModel, PersonDtoModel } from '../services/api/persons-service/model/models';
import { DevicesClient } from '../services/api/device-service/api/devices.service';
import { PositionDtoModel } from '../services/api/device-service/model/positionDto';
import { GeofencesClient } from '../services/api/geofences-service/api/geofences.service';
import { CreateDashboardItemDto, DashboardItemType, PersonDto } from '../models/api-types';
import { DashboardRuntimeService } from '../services/dashboard-runtime.service';
import { forkJoin, Observable, of, Subscription } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { EditPersonModalComponent } from './edit-person-modal/edit-person-modal.component';
import { DialogClosedEventArgs, DialogService } from '../services/dialog.service';
import { LoggingService } from '../services/logging.service';

interface TrackerStatusDto {
  id?: number;
  name?: string | null;
  position?: PositionDtoModel | null;
  batteryLevel?: number | null;
}

interface CurrentGeofenceDto {
  name?: string | null;
}

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


  constructor(
    private personsService: PersonsClient,
    private devicesService: DevicesClient,
    private geofencesService: GeofencesClient,
    private dialogService: DialogService,
    private dashboardService: DashboardRuntimeService,
    private logger: LoggingService
  ) { }


  public retrievePersons(): void {
    this.isLoading = true;
    this.personsService.apiPersonsGet().pipe(
      switchMap((response: PersonResponseDtoModel[]) => {
        if (response.length === 0) {
          return of([] as PersonResponseDtoModel[]);
        }

        return forkJoin(response.map(person => this.enrichPerson(person)));
      })
    ).subscribe({
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

  private enrichPerson(person: PersonResponseDtoModel): Observable<PersonResponseDtoModel> {
    const trackerId = person.gpsTrackerID;
    if (!trackerId) {
      return of(this.withFallbackValues(person));
    }

    return this.devicesService.devicesGetGpsTrackerByNodeIdGETApiDevicesGpstrackerNodeId(trackerId).pipe(
      switchMap((tracker: TrackerStatusDto | null | undefined) => {
        const position = tracker?.position ?? undefined;
        const enrichedPerson: PersonResponseDtoModel = {
          ...person,
          gpsTrackerName: tracker?.name ?? person.gpsTrackerName ?? `#${trackerId}`,
          currentPosition: this.formatPosition(position)
        };

        if (!this.hasValidPosition(position)) {
          return of({
            ...enrichedPerson,
            currentGeoFence: person.currentGeoFence ?? '-'
          });
        }

        return this.geofencesService.apiGeofencesCurrentGet(position.latitude, position.longitude).pipe(
          map((geofence: CurrentGeofenceDto | null | undefined) => ({
            ...enrichedPerson,
            currentGeoFence: geofence?.name ?? '-'
          })),
          catchError((error) => {
            this.logger.warn('Error loading current geofence for person:', error);
            return of({
              ...enrichedPerson,
              currentGeoFence: person.currentGeoFence ?? '-'
            });
          })
        );
      }),
      catchError((error) => {
        this.logger.warn('Error loading GPS tracker status for person:', error);
        return of(this.withFallbackValues(person));
      })
    );
  }

  private withFallbackValues(person: PersonResponseDtoModel): PersonResponseDtoModel {
    return {
      ...person,
      gpsTrackerName: person.gpsTrackerName ?? '-',
      currentGeoFence: person.currentGeoFence ?? '-',
      currentPosition: person.currentPosition ?? '-'
    };
  }

  private hasValidPosition(position?: PositionDtoModel | null): position is PositionDtoModel {
    return !!position?.isValid
      && position.latitude != null
      && position.longitude != null
      && !(position.latitude === 0 && position.longitude === 0);
  }

  private formatPosition(position?: PositionDtoModel | null): string {
    if (!this.hasValidPosition(position)) {
      return '-';
    }

    const latitude = position.latitude;
    const longitude = position.longitude;

    if (latitude == null || longitude == null) {
      return '-';
    }

    return `${latitude.toFixed(5)}, ${longitude.toFixed(5)}`;
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
          this.enrichPerson(response).subscribe({
            next: (enrichedPerson: PersonResponseDtoModel) => {
              const index = this.persons.findIndex(d => d.id === enrichedPerson.id);
              this.persons[index] = enrichedPerson;
            },
            error: (error) => {
              this.logger.error('Error enriching edited person:', error);
            }
          });
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
            this.enrichPerson(response).subscribe({
              next: (enrichedPerson: PersonResponseDtoModel) => {
                this.persons.push(enrichedPerson);
              },
              error: (error) => {
                this.logger.error('Error enriching added person:', error);
              }
            });
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
            description: '',
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
