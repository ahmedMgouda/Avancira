import { Injectable } from '@angular/core';
import { loadGapiInsideDOM } from 'gapi-script';

// gapi is loaded dynamically by gapi-script
declare const gapi: any;

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {
  private initialized = false;

  async init(clientId: string): Promise<void> {
    if (this.initialized) return;

    await loadGapiInsideDOM();

    return new Promise((resolve, reject) => {
      gapi.load('auth2', () => {
        try {
          gapi.auth2.init({
            client_id: clientId,
            scope: 'profile email'
          });
          this.initialized = true;
          resolve();
        } catch (err) {
          reject(err);
        }
      });
    });
  }

  getAuthInstance(): any | null {
    if (!this.initialized) {
      return null;
    }
    return gapi.auth2.getAuthInstance();
  }
}
