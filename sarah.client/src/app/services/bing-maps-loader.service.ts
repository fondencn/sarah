import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BingMapsLoaderService {
  private apiKey: string = environment.bingMapKey;
  private scriptLoaded: boolean = false;

  load(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (this.scriptLoaded) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.type = 'text/javascript';
      script.src = `https://www.bing.com/api/maps/mapcontrol?callback=bingMapsCallback&key=${this.apiKey}`;
      script.async = true;
      script.defer = true;

      if (this.apiKey === 'BING_MAPS_KEY_PLACEHOLDER') {
        reject('Bing Maps API key not set');
        return;
      }

      (window as any).bingMapsCallback = () => {
        this.scriptLoaded = true;
        resolve();
      };

      script.onerror = (error: any) => {
        reject(error);
      };

      document.body.appendChild(script);
    });
  }
}