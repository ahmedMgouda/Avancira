import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { OAuthService } from 'angular-oauth2-oidc';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let oauth: jasmine.SpyObj<OAuthService>;

  beforeEach(() => {
    oauth = jasmine.createSpyObj('OAuthService', [
      'configure',
      'loadDiscoveryDocumentAndTryLogin',
      'setupAutomaticSilentRefresh',
      'hasValidAccessToken',
      'getAccessToken',
      'refreshToken',
      'initCodeFlow',
      'logOut',
    ]);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      providers: [{ provide: OAuthService, useValue: oauth }],
    });

    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('init should load discovery document', async () => {
    oauth.loadDiscoveryDocumentAndTryLogin.and.returnValue(Promise.resolve(true));
    await service.init();
    expect(oauth.setupAutomaticSilentRefresh).toHaveBeenCalled();
  });
});

