import { Injectable } from '@angular/core';

import { SocialProvider } from '../models/social-provider';
import { ConfigKey } from '../models/config-key';
import { ConfigService } from './config.service';
import { GoogleAuthService } from './google-auth.service';
import { SocialAuthStrategy } from './social-auth-strategy';

@Injectable({ providedIn: 'root' })
export class GoogleAuthStrategy implements SocialAuthStrategy {
  readonly provider = SocialProvider.Google;
  initialized = false;
  private initPromise?: Promise<void>;

  constructor(
    private googleAuth: GoogleAuthService,
    private config: ConfigService
  ) {}

  init(): Promise<void> {
    if (this.initialized) {
      return Promise.resolve();
    }

    if (this.initPromise) {
      return this.initPromise;
    }

    const clientId = this.config.get(ConfigKey.GoogleClientId);
    if (!clientId) {
      return Promise.reject('Google client ID not configured.');
    }

    this.initPromise = this.googleAuth.init(clientId)
      .then(() => {
        this.initialized = true;
      })
      .catch((err) => {
        // reset so that a subsequent call can retry initialization
        this.initPromise = undefined;
        throw err;
      });

    return this.initPromise;
  }

  login(): Promise<string> {
    return this.googleAuth.signIn();
  }
}
