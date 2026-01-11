import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EventsControllerService } from './api/eventprocessing-service';

/**
 * Wrapper service for Event Processing Service API
 * Provides a simplified interface to the generated Event Processing Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class EventProcessingService {

  constructor(private eventsClient: EventsControllerService) { }

  /**
   * Publish a text-to-speech event
   * @param request Say event request
   */
  publishSayEvent(request: any): Observable<any> {
    return this.eventsClient.publishSayEvent(request);
  }

  /**
   * Get event processing service status
   */
  getStatus(): Observable<any> {
    return this.eventsClient.getStatus();
  }
}
