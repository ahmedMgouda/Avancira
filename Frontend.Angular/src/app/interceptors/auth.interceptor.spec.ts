import {
  HttpClient,
  HttpContext,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { SKIP_AUTH, authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { StorageService } from '../services/storage.service';
import { environment } from '../environments/environment';

class MockRouter {
  url = '/';
  navigate() { return Promise.resolve(true); }
}

class MockNotificationService {
  stopConnection() {}
}

function base64Url(obj: any): string {
  return btoa(JSON.stringify(obj))
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}

function createToken(payload: any): string {
  const header = base64Url({ alg: 'HS256', typ: 'JWT' });
  const body = base64Url(payload);
  return `${header}.${body}.sig`;
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;
  let storageSpy: jasmine.SpyObj<StorageService>;

  beforeEach(() => {
    storageSpy = jasmine.createSpyObj('StorageService', ['getItem', 'setItem', 'removeItem']);
    storageSpy.getItem.and.callFake((key: string) => localStorage.getItem(key));
    storageSpy.setItem.and.callFake((key: string, value: string) => localStorage.setItem(key, value));
    storageSpy.removeItem.and.callFake((key: string) => localStorage.removeItem(key));

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useClass: MockRouter },
        { provide: NotificationService, useClass: MockNotificationService },
        { provide: StorageService, useValue: storageSpy },
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

    const context = new HttpContext().set(SKIP_AUTH, true);
    http.post('/auth/token', {}, { context }).subscribe({ error: e => error = e });

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

  it('should retry concurrent requests with new token after refresh', () => {
    const refreshSpy = spyOn(authService, 'refreshToken').and.callThrough();
    authService['accessToken'] = 'a';
    localStorage.setItem('refresh_token', 'r');
    localStorage.setItem('refresh_token_expiry', new Date(Date.now() + 10000).toISOString());

    let resp1: any;
    let resp2: any;

    http.get('/data1').subscribe(r => resp1 = r);
    http.get('/data2').subscribe(r => resp2 = r);

    const req1 = httpMock.expectOne('/data1');
    const req2 = httpMock.expectOne('/data2');
    req1.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    req2.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    refresh.flush({ token: 'new', refreshToken: 'rr', refreshTokenExpiryTime: new Date(Date.now() + 10000).toISOString() });

    const retry1 = httpMock.expectOne('/data1');
    const retry2 = httpMock.expectOne('/data2');
    expect(retry1.request.headers.get('Authorization')).toBe('Bearer new');
    expect(retry2.request.headers.get('Authorization')).toBe('Bearer new');
    retry1.flush({ data: 1 });
    retry2.flush({ data: 2 });

    expect(resp1).toEqual({ data: 1 });
    expect(resp2).toEqual({ data: 2 });
    httpMock.expectNone(`${environment.apiUrl}/auth/refresh`);
    expect(refreshSpy).toHaveBeenCalledTimes(1);
  });

  it('should fail concurrent requests when refresh fails', () => {
    const refreshSpy = spyOn(authService, 'refreshToken').and.callThrough();
    authService['accessToken'] = 'a';
    localStorage.setItem('refresh_token', 'r');
    localStorage.setItem('refresh_token_expiry', new Date(Date.now() + 10000).toISOString());

    let error1: HttpErrorResponse | undefined;
    let error2: HttpErrorResponse | undefined;

    http.get('/data1').subscribe({ error: e => error1 = e });
    http.get('/data2').subscribe({ error: e => error2 = e });

    const req1 = httpMock.expectOne('/data1');
    const req2 = httpMock.expectOne('/data2');
    req1.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    req2.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    refresh.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(error1?.status).toBe(401);
    expect(error2?.status).toBe(401);
    httpMock.expectNone(`${environment.apiUrl}/auth/refresh`);
    expect(refreshSpy).toHaveBeenCalledTimes(1);
  });

  it('should use device id from token claims for subsequent requests', () => {
    localStorage.setItem('deviceId', 'old');

    const token = createToken({ device_id: 'new-device', exp: Date.now() / 1000 + 1000 });
    (authService as any).accessToken = token;
    // decode token to store device id
    (authService as any).decodeToken(token);

    http.get('/data').subscribe();
    const req = httpMock.expectOne('/data');
    expect(req.request.headers.get('Device-Id')).toBe('new-device');
    expect(localStorage.getItem('deviceId')).toBe('new-device');
    req.flush({});
  });
});

