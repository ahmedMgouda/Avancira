import { Injectable } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { GoogleAuthService } from './google-auth.service';
import { FacebookAuthService } from './facebook-auth.service';
import { SocialProvider } from '../models/social-provider';
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
        if (provider === SocialProvider.Google) {
          return from(this.google.init(this.config.get('googleClientId'))).pipe(
            switchMap(() => from(this.google.signIn())),
            switchMap((token) => this.auth.externalLogin(SocialProvider.Google, token))
          );
        }

        if (provider === SocialProvider.Facebook) {
          return from(this.facebook.ensureInitialized()).pipe(
            switchMap(() => from(this.facebook.login())),
            switchMap((token) => this.auth.externalLogin(SocialProvider.Facebook, token))
          );
        }

        return throwError(() => new Error(`Unsupported provider: ${provider}`));
      })
    );
  }
}
