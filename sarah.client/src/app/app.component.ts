import { Component, OnDestroy, OnInit } from '@angular/core';
import { UiService } from './services/ui.service';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { CacheService } from './services/cache.service';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  constructor(
    public ui: UiService, 
    public router: Router, 
    private authService: AuthService, 
    private cacheService: CacheService  
  ) 
  {
  }

  currentYear = new Date().getFullYear();

  ngOnInit() {
    this.cacheService.update().subscribe({
      next: () => {
        console.log('Base data updated');
      },
      error: (error) => {
        console.error('Error updating base data:', error);
      }
    });
  }


  ngOnDestroy() {
  }


}
