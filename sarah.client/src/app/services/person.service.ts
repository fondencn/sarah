import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PersonsControllerService } from './api/persons-service';

/**
 * Wrapper service for Persons Service API
 * Provides a simplified interface to the generated Persons Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class PersonService {

  constructor(private personsClient: PersonsControllerService) { }

  /**
   * Get all persons in the system
   */
  getAllPersons(): Observable<any> {
    return this.personsClient.getAllPersons();
  }

  /**
   * Get a specific person by ID
   * @param id Person ID
   */
  getPersonById(id: string): Observable<any> {
    return this.personsClient.getPersonById(id);
  }

  /**
   * Get persons service status
   */
  getStatus(): Observable<any> {
    return this.personsClient.getStatus();
  }
}
