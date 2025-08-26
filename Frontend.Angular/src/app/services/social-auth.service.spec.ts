import { TestBed } from '@angular/core/testing';

import { SocialAuthService } from './social-auth.service';
import { AuthService } from './auth.service';
import { SocialProvider } from '../models/social-provider';

describe('SocialAuthService', () => {
  let service: SocialAuthService;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    auth = jasmine.createSpyObj('AuthService', ['startLogin']);
    TestBed.configureTestingModule({
      providers: [
        SocialAuthService,
        { provide: AuthService, useValue: auth },
      ],
    });
    service = TestBed.inject(SocialAuthService);
  });

  it('should call startLogin with provider', (done) => {
    service.authenticate(SocialProvider.Google).subscribe({
      next: () => {
        expect(auth.startLogin).toHaveBeenCalled();
        done();
      },
      error: done.fail,
    });
  });
});

