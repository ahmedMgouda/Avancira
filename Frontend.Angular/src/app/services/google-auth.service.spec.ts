import { TestBed } from '@angular/core/testing';

import { GoogleAuthService } from './google-auth.service';

describe('GoogleAuthService', () => {
  let service: GoogleAuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GoogleAuthService);
  });

  it('should reject init when clientId is falsy', async () => {
    await expectAsync(service.init('')).toBeRejected();
  });

  it('should reject signIn when not initialized', async () => {
    await expectAsync(service.signIn()).toBeRejected();
  });
});
