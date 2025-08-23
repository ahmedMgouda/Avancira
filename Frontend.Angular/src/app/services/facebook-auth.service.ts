// src/app/services/facebook-auth.service.ts
import { Injectable } from '@angular/core';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';

import { ConfigService } from './config.service';
import { ConfigKey } from '../models/config-key';

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
      const appId = this.config.get(ConfigKey.FacebookAppId);
      if (!appId || !appId.trim()) {
        return Promise.reject(new Error('Facebook App ID is required.'));
      }

      const params: InitParams = {
        appId,
        cookie: true,
        xfbml: true,
        version: 'v21.0',
      };
      this.initPromise = (async () => {
        try {
          await this.fb.init(params);
        } catch (error) {
          this.initPromise = null;
          throw error;
        }
      })();
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

