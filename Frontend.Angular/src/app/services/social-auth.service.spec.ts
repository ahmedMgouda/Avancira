import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { SocialAuthService } from './social-auth.service';
import { AuthService } from './auth.service';
import { ConfigService } from './config.service';
import {
  SocialAuthService as LibSocialAuthService,
  GoogleLoginProvider,
  FacebookLoginProvider,
  SocialUser,
} from '@abacritt/angularx-social-login';
import { SocialProvider } from '../models/social-provider';

describe('SocialAuthService', () => {
  let service: SocialAuthService;
  let config: jasmine.SpyObj<ConfigService>;
  let auth: jasmine.SpyObj<AuthService>;
  let lib: jasmine.SpyObj<LibSocialAuthService>;

  beforeEach(() => {
    config = jasmine.createSpyObj('ConfigService', [
      'loadConfig',
      'isSocialProviderEnabled',
    ]);
    auth = jasmine.createSpyObj('AuthService', ['externalLogin']);
    lib = jasmine.createSpyObj('LibSocialAuthService', ['signIn']);

    TestBed.configureTestingModule({
      providers: [
        SocialAuthService,
        { provide: ConfigService, useValue: config },
        { provide: AuthService, useValue: auth },
        { provide: LibSocialAuthService, useValue: lib },
      ],
    });

    service = TestBed.inject(SocialAuthService);
  });

  it('should error when provider is disabled', (done) => {
    config.loadConfig.and.returnValue(of({} as any));
    config.isSocialProviderEnabled.and.returnValue(false);

    service.authenticate(SocialProvider.Google).subscribe({
      next: () => done.fail('expected error'),
      error: () => done(),
    });
  });

  it('should sign in with Google and forward the token', (done) => {
    config.loadConfig.and.returnValue(of({} as any));
    config.isSocialProviderEnabled.and.returnValue(true);
    lib.signIn.and.returnValue(
      Promise.resolve({ idToken: 'token' } as SocialUser)
    );
    auth.externalLogin.and.returnValue(of({} as any));

    service.authenticate(SocialProvider.Google).subscribe({
      next: () => {
        expect(lib.signIn).toHaveBeenCalledWith(
          GoogleLoginProvider.PROVIDER_ID
        );
        expect(auth.externalLogin).toHaveBeenCalledWith(
          SocialProvider.Google,
          'token'
        );
        done();
      },
      error: done.fail,
    });
  });

  it('should sign in with Facebook and forward the auth token', (done) => {
    config.loadConfig.and.returnValue(of({} as any));
    config.isSocialProviderEnabled.and.returnValue(true);
    lib.signIn.and.returnValue(
      Promise.resolve({ authToken: 'fb-token' } as SocialUser)
    );
    auth.externalLogin.and.returnValue(of({} as any));

    service.authenticate(SocialProvider.Facebook).subscribe({
      next: () => {
        expect(lib.signIn).toHaveBeenCalledWith(
          FacebookLoginProvider.PROVIDER_ID
        );
        expect(auth.externalLogin).toHaveBeenCalledWith(
          SocialProvider.Facebook,
          'fb-token'
        );
        done();
      },
      error: done.fail,
    });
  });
});

