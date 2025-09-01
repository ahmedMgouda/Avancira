import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
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
      'logOut',
      'getIdentityClaims',
      'processIdToken',
      'tryLogin'
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

    expect(oauthSpy.logOut).toHaveBeenCalledWith({
      logoutUrl: `${environment.baseApiUrl}connect/logout`,
      postLogoutRedirectUri: environment.postLogoutRedirectUri,
    });
  });

  it('performs a single refresh for concurrent calls', fakeAsync(() => {
    oauthSpy.hasValidAccessToken.and.returnValue(false);
    oauthSpy.refreshToken.and.callFake(() => {
      oauthSpy.hasValidAccessToken.and.returnValue(true);
      oauthSpy.getAccessToken.and.returnValue('new-token');
      return Promise.resolve();
    });

    const results: string[] = [];
    service.getValidAccessToken().subscribe(t => results.push(t));
    service.getValidAccessToken().subscribe(t => results.push(t));

    flushMicrotasks();

    expect(results).toEqual(['new-token', 'new-token']);
    expect(oauthSpy.refreshToken).toHaveBeenCalledTimes(1);
  }));

  it('propagates refresh errors to all callers and logs out once', fakeAsync(() => {
    const err = new Error('bad');
    oauthSpy.hasValidAccessToken.and.returnValue(false);
    oauthSpy.refreshToken.and.returnValue(Promise.reject(err));

    const errors: unknown[] = [];
    service.getValidAccessToken().subscribe({ error: e => errors.push(e) });
    service.getValidAccessToken().subscribe({ error: e => errors.push(e) });

    flushMicrotasks();

    expect(errors).toEqual([err, err]);
    expect(oauthSpy.refreshToken).toHaveBeenCalledTimes(1);
    expect(oauthSpy.logOut).toHaveBeenCalledTimes(1);
  }));

  describe('decode', () => {
    it('normalizes single role and permission claims to arrays', () => {
      oauthSpy.getIdentityClaims.and.returnValue({
        sub: '1',
        role: 'admin',
        permission: 'read'
      });

      const profile = service.decode();

      expect(profile?.roles).toEqual(['admin']);
      expect(profile?.permissions).toEqual(['read']);
    });

    it('returns role and permission claims as arrays when already arrays', () => {
      oauthSpy.getIdentityClaims.and.returnValue({
        sub: '1',
        role: ['admin', 'user'],
        permission: ['read', 'write']
      });

      const profile = service.decode();

      expect(profile?.roles).toEqual(['admin', 'user']);
      expect(profile?.permissions).toEqual(['read', 'write']);
    });
  });
});
