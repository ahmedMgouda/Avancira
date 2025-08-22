import { Injectable } from '@angular/core';
import { FacebookService, InitParams } from 'ngx-facebook';
import { Observable, from } from 'rxjs';
import { switchMap, take, tap } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { GoogleAuthService } from './google-auth.service';
import { UserProfile } from '../models/UserProfile';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  private facebookInitialized = false;

  constructor(
    private google: GoogleAuthService,
    private facebook: FacebookService,
    private config: ConfigService,
    private auth: AuthService
  ) {}

  authenticate(provider: 'google' | 'facebook'): Observable<UserProfile> {
    if (provider === 'google') {
      return this.config.loadConfig().pipe(
        take(1),
        switchMap(() => from(this.google.init(this.config.get('googleClientId')))),
        switchMap(() => from(this.google.signIn())),
        switchMap((token) => this.auth.externalLogin('google', token))
      );
    }

    return this.config.loadConfig().pipe(
      take(1),
      tap(() => {
        if (!this.facebookInitialized) {
          const params: InitParams = {
            appId: this.config.get('facebookAppId'),
            cookie: true,
            xfbml: true,
            version: 'v21.0',
          };
          this.facebook.init(params);
          this.facebookInitialized = true;
        }
      }),
      switchMap(() => from(this.facebook.login({ scope: 'email,public_profile' }))),
      switchMap((res) =>
        this.auth.externalLogin('facebook', res.authResponse.accessToken)
      )
    );
  }
}
