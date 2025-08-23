import { Injectable } from '@angular/core';

import { SocialProvider } from '../models/social-provider';
import { FacebookAuthService } from './facebook-auth.service';
import { SocialAuthStrategy } from './social-auth-strategy';

@Injectable({ providedIn: 'root' })
export class FacebookAuthStrategy implements SocialAuthStrategy {
  readonly provider = SocialProvider.Facebook;
  initialized = false;
  private initPromise?: Promise<void>;

  constructor(private facebookAuth: FacebookAuthService) {}

  init(): Promise<void> {
    if (this.initialized) {
      return Promise.resolve();
    }

    if (this.initPromise) {
      return this.initPromise;
    }

    this.initPromise = this.facebookAuth
      .ensureInitialized()
      .then(() => {
        this.initialized = true;
      })
      .catch((err) => {
        this.initPromise = undefined;
        throw err;
      });

    return this.initPromise;
  }

  login(): Promise<string> {
    return this.facebookAuth.login();
  }
}
