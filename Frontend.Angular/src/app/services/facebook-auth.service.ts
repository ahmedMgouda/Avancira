// src/app/services/facebook-auth.service.ts
import { Injectable } from '@angular/core';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';

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
      const params: InitParams = {
        appId: this.config.get('facebookAppId'),
        cookie: true,
        xfbml: true,
        version: 'v21.0',
      };
      this.fb.init(params);
      this.initPromise = Promise.resolve();
    }
    return this.initPromise;
  }

  /** Performs Facebook login and resolves the access token */
  async login(): Promise<string> {
    const res: LoginResponse = await this.fb.login({ scope: 'email,public_profile' });
    const token = res.authResponse?.accessToken;
    if (res.status !== 'connected' || !token) {
      throw new Error('Facebook login failed or access token missing');
    }
    return token;
  }
}

