import { Injectable } from '@angular/core';
import { Observable, of, switchMap } from 'rxjs';

import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class GoogleMapsService {
  private scriptLoaded = false;

  constructor(
    private configService: ConfigService
  ) { }

  loadGoogleMaps(): Observable<void> {
    if (this.scriptLoaded) {
      return of(undefined); // Return an observable that emits void immediately
    }

    return this.configService.loadConfig().pipe(
      switchMap(() => new Observable<void>((observer) => {
        const script = document.createElement('script');
        script.src = `https://maps.googleapis.com/maps/api/js?key=${this.configService.get('googleMapsApiKey')}&libraries=places`;
        script.async = true;
        script.defer = true;

        script.onload = () => {
          this.scriptLoaded = true;
          observer.next();
          observer.complete();
        };

        script.onerror = () => {
          observer.error('Google Maps API could not be loaded.');
        };

        document.body.appendChild(script);
      }))
    );
  }
}
