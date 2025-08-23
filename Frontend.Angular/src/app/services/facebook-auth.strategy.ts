import { Injectable } from '@angular/core';

import { SocialProvider } from '../models/social-provider';
import { FacebookAuthService } from './facebook-auth.service';
import { SocialAuthStrategy } from './social-auth-strategy';

@Injectable({ providedIn: 'root' })
export class FacebookAuthStrategy implements SocialAuthStrategy {
  readonly provider = SocialProvider.Facebook;

  constructor(private facebookAuth: FacebookAuthService) {}

  init(): Promise<void> {
    return this.facebookAuth.ensureInitialized();
  }

  login(): Promise<string> {
    return this.facebookAuth.login();
  }
}
