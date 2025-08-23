import { Injectable } from '@angular/core';

import { SocialProvider } from '../models/social-provider';
import { ConfigService } from './config.service';
import { GoogleAuthService } from './google-auth.service';
import { SocialAuthStrategy } from './social-auth-strategy';

@Injectable({ providedIn: 'root' })
export class GoogleAuthStrategy implements SocialAuthStrategy {
  readonly provider = SocialProvider.Google;

  constructor(
    private googleAuth: GoogleAuthService,
    private config: ConfigService
  ) {}

  init(): Promise<void> {
    const clientId = this.config.get('googleClientId');
    if (!clientId) {
      return Promise.reject('Google client ID not configured.');
    }
    return this.googleAuth.init(clientId);
  }

  login(): Promise<string> {
    return this.googleAuth.signIn();
  }
}
