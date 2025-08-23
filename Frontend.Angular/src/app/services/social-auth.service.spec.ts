import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { SocialAuthService } from './social-auth.service';
import { GoogleAuthStrategy } from './google-auth.strategy';
import { FacebookAuthStrategy } from './facebook-auth.strategy';
import { ConfigService } from './config.service';
import { AuthService } from './auth.service';
import { SocialProvider } from '../models/social-provider';

describe('SocialAuthService', () => {
  let service: SocialAuthService;
  let config: jasmine.SpyObj<ConfigService>;
  let auth: jasmine.SpyObj<AuthService>;
  let google: jasmine.SpyObj<GoogleAuthStrategy>;
  let facebook: jasmine.SpyObj<FacebookAuthStrategy>;

  beforeEach(() => {
    google = jasmine.createSpyObj('GoogleAuthStrategy', ['init', 'login'], { provider: SocialProvider.Google });
    facebook = jasmine.createSpyObj('FacebookAuthStrategy', ['init', 'login'], { provider: SocialProvider.Facebook });
    config = jasmine.createSpyObj('ConfigService', ['loadConfig', 'isSocialProviderEnabled']);
    auth = jasmine.createSpyObj('AuthService', ['externalLogin']);

    TestBed.configureTestingModule({
      providers: [
        SocialAuthService,
        { provide: GoogleAuthStrategy, useValue: google },
        { provide: FacebookAuthStrategy, useValue: facebook },
        { provide: ConfigService, useValue: config },
        { provide: AuthService, useValue: auth },
      ],
    });

    service = TestBed.inject(SocialAuthService);
  });

  it('should error when provider is disabled', (done) => {
    config.loadConfig.and.returnValue(of({} as any));
    config.isSocialProviderEnabled.and.returnValue(false);

    service.authenticate(SocialProvider.Google).subscribe({
      next: () => done.fail('expected error'),
      error: () => {
        expect(config.isSocialProviderEnabled).toHaveBeenCalledWith(SocialProvider.Google);
        expect(google.init).not.toHaveBeenCalled();
        expect(auth.externalLogin).not.toHaveBeenCalled();
        done();
      },
    });
  });
});
