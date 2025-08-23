import { Injectable } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';

import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import { FacebookAuthStrategy } from './facebook-auth.strategy';
import { GoogleAuthStrategy } from './google-auth.strategy';
import { SocialAuthStrategy } from './social-auth-strategy';
import { SocialProvider } from '../models/social-provider';
import { UserProfile } from '../models/UserProfile';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  private strategies = new Map<SocialProvider, SocialAuthStrategy>();

  constructor(
    google: GoogleAuthStrategy,
    facebook: FacebookAuthStrategy,
    private config: ConfigService,
    private auth: AuthService
  ) {
    this.strategies.set(SocialProvider.Google, google);
    this.strategies.set(SocialProvider.Facebook, facebook);
  }

  authenticate(provider: SocialProvider): Observable<UserProfile> {
    return this.config.loadConfig().pipe(
      take(1),
      switchMap(() => {
        const strategy = this.strategies.get(provider);
        if (!strategy) {
          return throwError(() => new Error(`Unsupported provider: ${provider}`));
        }

        return from(strategy.init()).pipe(
          switchMap(() => from(strategy.login())),
          switchMap((token) => this.auth.externalLogin(provider, token))
        );
      })
    );
  }
}
