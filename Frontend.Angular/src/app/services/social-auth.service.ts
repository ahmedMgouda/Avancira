import { Injectable } from '@angular/core';
import { of } from 'rxjs';

import { SocialProvider } from '../models/social-provider';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SocialAuthService {
  constructor(private auth: AuthService) {}

  authenticate(provider: SocialProvider) {
    this.auth.startLogin(undefined, provider);
    return of(void 0);
  }
}

