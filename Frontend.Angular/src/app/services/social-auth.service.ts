import { Injectable } from '@angular/core';
import { from, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import {
  SocialAuthService as LibSocialAuthService,
  GoogleLoginProvider,
  SocialUser,
} from '@abacritt/angularx-social-login';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { FacebookAuthService } from './facebook-auth.service';
import { SocialProvider } from '../models/social-provider';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  constructor(
    private config: ConfigService,
    private auth: AuthService,
    private social: LibSocialAuthService,
    private facebook: FacebookAuthService
  ) {}

  authenticate(provider: SocialProvider) {
    return this.config.loadConfig().pipe(
      switchMap(() => {
        if (!this.config.isSocialProviderEnabled(provider)) {
          return throwError(() => new Error(`${provider} not enabled`));
        }

        if (provider === SocialProvider.Google) {
          return from(
            this.social.signIn(GoogleLoginProvider.PROVIDER_ID)
          ).pipe(
            switchMap((user: SocialUser) => {
              const token = user.idToken;
              if (!token) {
                return throwError(() => new Error('No ID token'));
              }
              return this.auth.externalLogin(provider, token);
            })
          );
        }

        if (provider === SocialProvider.Facebook) {
          return from(
            this.facebook
              .ensureInitialized()
              .then(() => this.facebook.login())
          ).pipe(
            switchMap((token) => this.auth.externalLogin(provider, token))
          );
        }

        return throwError(() => new Error('Unsupported provider'));
      })
    );
  }
}

