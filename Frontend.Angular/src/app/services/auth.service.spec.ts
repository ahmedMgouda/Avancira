import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: NotificationService, useValue: { stopConnection: () => {} } }
      ]
    });

    spyOn(AuthService.prototype as any, 'restoreProfile').and.stub();

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should error waiters when refresh fails', done => {
    service.waitForRefresh().subscribe({
      next: () => fail('should not emit'),
      error: err => {
        expect(err).toBeTruthy();
        done();
      }
    });

    service.refreshFailed(new Error('fail'));
  });

  it('should clear session even if revoke fails', () => {
    (service as any).accessToken = 'token';
    const clearSpy = spyOn<any>(service, 'clearSession').and.callThrough();

    service.logout();

    const req = httpMock.expectOne(`${(service as any).apiBase}/auth/revoke`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({}, { status: 500, statusText: 'Error' });

    expect(clearSpy).toHaveBeenCalled();
  });
});
