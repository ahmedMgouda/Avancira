import { Injectable } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { GoogleAuthService } from './google-auth.service';
import { FacebookAuthService } from './facebook-auth.service';
import { FACEBOOK, GOOGLE, SocialProvider } from '../models/social-provider';
import { UserProfile } from '../models/UserProfile';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  constructor(
    private google: GoogleAuthService,
    private facebook: FacebookAuthService,
    private config: ConfigService,
    private auth: AuthService
  ) {}

  authenticate(provider: SocialProvider): Observable<UserProfile> {
    return this.config.loadConfig().pipe(
      take(1),
      switchMap(() => {
        if (provider === GOOGLE) {
          return from(this.google.init(this.config.get('googleClientId'))).pipe(
            switchMap(() => from(this.google.signIn())),
            switchMap((token) => this.auth.externalLogin(GOOGLE, token))
          );
        }

        if (provider === FACEBOOK) {
          return from(this.facebook.ensureInitialized()).pipe(
            switchMap(() => from(this.facebook.login())),
            switchMap((token) => this.auth.externalLogin(FACEBOOK, token))
          );
        }

        return throwError(() => new Error(`Unsupported provider: ${provider}`));
      })
    );
  }
}
