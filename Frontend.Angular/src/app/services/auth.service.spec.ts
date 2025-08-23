import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { take } from 'rxjs/operators';

import { AuthService, AuthStateKind } from './auth.service';
import { environment } from '../environments/environment';
import { SocialProvider } from '../models/social-provider';

function makeToken(payload: any): string {
  const header = { alg: 'none', typ: 'JWT' };
  const encode = (obj: any) => btoa(JSON.stringify(obj)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${encode(header)}.${encode(payload)}.`;
}

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should set auth state and profile on login', (done) => {
    const token = makeToken({ sub: '1', email: 'a@b.com', exp: Math.floor(Date.now() / 1000) + 3600 });

    service.login('a@b.com', 'pw').subscribe(profile => {
      expect(profile.email).toBe('a@b.com');
      expect(profile.permissions).toEqual(['perm1']);
      service.authState$.pipe(take(1)).subscribe(state => {
        expect(state).toBe(AuthStateKind.Authenticated);
        expect(service.isAuthenticated()).toBeTrue();
        done();
      });
    });

    const req = http.expectOne(`${environment.apiUrl}/auth/token`);
    expect(req.request.method).toBe('POST');
    req.flush({ token });

    const perms = http.expectOne(`${environment.apiUrl}/users/permissions`);
    perms.flush(['perm1']);
  });

  it('should set auth state and profile on external login', (done) => {
    const token = makeToken({ sub: '2', email: 'c@d.com', exp: Math.floor(Date.now() / 1000) + 3600 });

    document.cookie = 'CSRF-TOKEN=csrf123';

    service.externalLogin(SocialProvider.Google, 'social-token').subscribe(profile => {
      expect(profile.email).toBe('c@d.com');
      expect(profile.permissions).toEqual(['perm2']);
      service.authState$.pipe(take(1)).subscribe(state => {
        expect(state).toBe(AuthStateKind.Authenticated);
        expect(service.isAuthenticated()).toBeTrue();
        done();
      });
    });

    const req = http.expectOne(`${environment.apiUrl}/auth/external-login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-CSRF-TOKEN')).toBe('csrf123');
    req.flush({ token });

    const perms = http.expectOne(`${environment.apiUrl}/users/permissions`);
    perms.flush(['perm2']);
  });
});
