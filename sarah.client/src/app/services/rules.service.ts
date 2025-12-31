import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { RulesControllerService } from './api/rules-service';

/**
 * Wrapper service for Rules Service API
 * Provides a simplified interface to the generated Rules Service API client
 */
@Injectable({
  providedIn: 'root'
})
export class RulesService {

  constructor(private rulesClient: RulesControllerService) { }

  /**
   * Get rules service status
   */
  getStatus(): Observable<any> {
    return this.rulesClient.getStatus();
  }
}
