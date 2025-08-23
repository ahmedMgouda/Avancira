import { Injectable } from '@angular/core';
import { Observable, from } from 'rxjs';
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
    if (provider === GOOGLE) {
      return this.config.loadConfig().pipe(
        take(1),
        switchMap(() => from(this.google.init(this.config.get('googleClientId')))),
        switchMap(() => from(this.google.signIn())),
        switchMap((token) => this.auth.externalLogin(GOOGLE, token))
      );
    }

    return from(this.facebook.login()).pipe(
      switchMap((token) => this.auth.externalLogin(FACEBOOK, token))
    );
  }
}
