import { TestBed } from '@angular/core/testing';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthService);
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
});
