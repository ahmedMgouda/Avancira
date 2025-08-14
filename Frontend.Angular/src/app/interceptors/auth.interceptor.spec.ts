import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { environment } from '../environments/environment';

class MockRouter {
  url = '/';
  navigate() { return Promise.resolve(true); }
}

class MockNotificationService {
  stopConnection() {}
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useClass: MockRouter },
        { provide: NotificationService, useClass: MockNotificationService },
        AuthService,
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should bypass refresh logic for auth endpoints', () => {
    const refreshSpy = spyOn(authService, 'refreshToken');
    let error: HttpErrorResponse | undefined;

    http.post('/auth/token', {}).subscribe({ error: e => error = e });

    const req = httpMock.expectOne('/auth/token');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(refreshSpy).not.toHaveBeenCalled();
    expect(error?.status).toBe(401);
  });

  it('should not loop refresh when refresh endpoint fails', () => {
    const refreshSpy = spyOn(authService, 'refreshToken').and.callThrough();
    authService['accessToken'] = 'a';
    localStorage.setItem('refresh_token', 'r');
    localStorage.setItem('refresh_token_expiry', new Date(Date.now() + 10000).toISOString());

    let error: HttpErrorResponse | undefined;
    http.get('/data').subscribe({ error: e => error = e });

    const initial = httpMock.expectOne('/data');
    initial.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    refresh.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    httpMock.expectNone(`${environment.apiUrl}/auth/refresh`);
    expect(refreshSpy).toHaveBeenCalledTimes(1);
    expect(error?.status).toBe(401);
  });
});

