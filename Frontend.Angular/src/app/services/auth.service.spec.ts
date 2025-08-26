import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

import { environment } from '../environments/environment';
import { AuthService } from './auth.service';
import { SessionService } from './session.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let oauthSpy: jasmine.SpyObj<OAuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    oauthSpy = jasmine.createSpyObj('OAuthService', [
      'configure',
      'loadDiscoveryDocumentAndTryLogin',
      'setupAutomaticSilentRefresh',
      'hasValidAccessToken',
      'getAccessToken',
      'refreshToken',
      'initCodeFlow',
      'logOut'
    ]);

    routerSpy = jasmine.createSpyObj('Router', ['navigateByUrl', 'createUrlTree']);
    routerSpy.createUrlTree.and.returnValue({} as any);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        SessionService,
        { provide: OAuthService, useValue: oauthSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('revokes session and clears cookie on logout', () => {
    const sessionId = 'session123';
    const payload = btoa(JSON.stringify({ sid: sessionId }));
    const token = `header.${payload}.sig`;
    oauthSpy.getAccessToken.and.returnValue(token);

    service.logout();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/sessions/${sessionId}`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(null);

    expect(oauthSpy.logOut).toHaveBeenCalled();
    expect(routerSpy.navigateByUrl).toHaveBeenCalled();
  });
});
