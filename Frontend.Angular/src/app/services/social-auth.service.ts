import { Injectable } from '@angular/core';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';
import { firstValueFrom } from 'rxjs';

import { GoogleAuthService } from './google-auth.service';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root',
})
export class SocialAuthService {
  private initialized = false;

  constructor(
    private fb: FacebookService,
    private googleAuthService: GoogleAuthService,
    private configService: ConfigService
  ) {}

  /** Initialize Facebook and Google SDKs using values from ConfigService */
  init(): void {
    if (this.initialized) {
      return;
    }

    this.configService.loadConfig().subscribe({
      next: async () => {
        const initParams: InitParams = {
          appId: this.configService.get('facebookAppId'),
          cookie: true,
          xfbml: true,
          version: 'v21.0',
        };
        this.fb.init(initParams);

        await this.googleAuthService.init(this.configService.get('googleClientId'));
        this.initialized = true;
      },
      error: err => {
        console.error('Failed to load configuration:', err.message);
      },
    });
  }

  /** Trigger Facebook login and return the OAuth token */
  loginWithFacebook(): Promise<string> {
    return this.fb
      .login({ scope: 'email,public_profile' })
      .then((response: LoginResponse) => response.authResponse.accessToken);
  }

  /** Trigger Google login and return the OAuth token */
  async loginWithGoogle(): Promise<string> {
    await this.googleAuthService.init(this.configService.get('googleClientId'));
    const auth2 = this.googleAuthService.getAuthInstance();
    if (!auth2) {
      throw new Error('Google Auth instance not initialized');
    }

    const googleUser = await auth2.signIn();
    return googleUser.getAuthResponse().id_token;
  }
}
