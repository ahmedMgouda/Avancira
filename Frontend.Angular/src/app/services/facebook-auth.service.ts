// src/app/services/facebook-auth.service.ts
import { Injectable } from '@angular/core';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';
import { firstValueFrom } from 'rxjs';

import { ConfigService } from './config.service';

@Injectable({ providedIn: 'root' })
export class FacebookAuthService {
  private initPromise: Promise<void> | null = null;

  constructor(
    private fb: FacebookService,
    private config: ConfigService
  ) {}

  /** Ensures the Facebook SDK is initialized */
  ensureInitialized(): Promise<void> {
    if (!this.initPromise) {
      this.initPromise = firstValueFrom(this.config.loadConfig()).then(() => {
        const params: InitParams = {
          appId: this.config.get('facebookAppId'),
          cookie: true,
          xfbml: true,
          version: 'v21.0',
        };
        this.fb.init(params);
      });
    }
    return this.initPromise;
  }

  /** Performs Facebook login and resolves the access token */
  async login(): Promise<string> {
    await this.ensureInitialized();
    const res: LoginResponse = await this.fb.login({ scope: 'email,public_profile' });
    return res.authResponse.accessToken;
  }
}

