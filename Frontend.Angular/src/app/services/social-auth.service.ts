import { Injectable } from '@angular/core';
import { from, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import {
  SocialAuthService as LibSocialAuthService,
  GoogleLoginProvider,
  FacebookLoginProvider,
  SocialUser,
} from '@abacritt/angularx-social-login';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { SocialProvider } from '../models/social-provider';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  constructor(
    private config: ConfigService,
    private auth: AuthService,
    private social: LibSocialAuthService
  ) {}

  authenticate(provider: SocialProvider) {
    return this.config.loadConfig().pipe(
      switchMap(() => {
        if (!this.config.isSocialProviderEnabled(provider)) {
          return throwError(() => new Error(`${provider} not enabled`));
        }

        let providerId: string;
        let tokenSelector: (user: SocialUser) => string | undefined;

        switch (provider) {
          case SocialProvider.Google:
            providerId = GoogleLoginProvider.PROVIDER_ID;
            tokenSelector = (u) => u.idToken;
            break;
          case SocialProvider.Facebook:
            providerId = FacebookLoginProvider.PROVIDER_ID;
            tokenSelector = (u) => u.authToken;
            break;
          default:
            return throwError(() => new Error('Unsupported provider'));
        }

        return from(this.social.signIn(providerId)).pipe(
          switchMap((user: SocialUser) => {
            const token = tokenSelector(user);
            if (!token) {
              return throwError(() => new Error('No token'));
            }
            return this.auth.externalLogin(provider, token);
          })
        );
      })
    );
  }
}

