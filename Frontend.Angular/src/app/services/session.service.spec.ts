import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { SessionService } from './session.service';
import { environment } from '../environments/environment';
import { DeviceSessions } from '../models/device-sessions';
import { UserSession } from '../models/session';

describe('SessionService', () => {
  let service: SessionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });
    service = TestBed.inject(SessionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch grouped sessions', () => {
    const mockResponse: DeviceSessions[] = [
      {
        deviceId: 'device-1',
        deviceName: 'Chrome on Mac',
        operatingSystem: 'macOS',
        userAgent: 'Chrome',
        country: 'US',
        city: 'NYC',
        lastActivityUtc: '2024-01-01T00:00:00Z',
        sessions: [
          {
            id: 'session-1',
            userId: 'user-1',
            authorizationId: 'auth-1',
            deviceId: 'device-1',
            deviceName: 'Chrome on Mac',
            userAgent: 'Chrome',
            operatingSystem: 'macOS',
            ipAddress: '127.0.0.1',
            country: 'US',
            city: 'NYC',
            createdAtUtc: '2024-01-01T00:00:00Z',
            absoluteExpiryUtc: '2024-02-01T00:00:00Z',
            lastActivityUtc: '2024-01-02T00:00:00Z',
            revokedAtUtc: undefined
          }
        ]
      }
    ];

    let result: DeviceSessions[] | undefined;
    service.getSessions().subscribe(data => (result = data));

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/sessions`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(mockResponse);

    expect(result).toEqual(mockResponse);
  });

  it('should flatten grouped sessions', () => {
    const mockDeviceSessions: DeviceSessions[] = [
      {
        deviceId: 'device-1',
        deviceName: 'Chrome on Mac',
        operatingSystem: 'macOS',
        userAgent: 'Chrome',
        country: 'US',
        city: 'NYC',
        lastActivityUtc: '2024-01-01T00:00:00Z',
        sessions: [
          {
            id: 'session-1',
            userId: 'user-1',
            authorizationId: 'auth-1',
            deviceId: 'device-1',
            deviceName: undefined,
            userAgent: undefined,
            operatingSystem: undefined,
            ipAddress: '127.0.0.1',
            country: undefined,
            city: undefined,
            createdAtUtc: '2024-01-01T00:00:00Z',
            absoluteExpiryUtc: '2024-02-01T00:00:00Z',
            lastActivityUtc: '2024-01-02T00:00:00Z',
            revokedAtUtc: undefined
          }
        ]
      }
    ];

    const flattened = service.flattenSessions(mockDeviceSessions);

    const expected: UserSession[] = [
      {
        id: 'session-1',
        userId: 'user-1',
        authorizationId: 'auth-1',
        deviceId: 'device-1',
        deviceName: 'Chrome on Mac',
        userAgent: 'Chrome',
        operatingSystem: 'macOS',
        ipAddress: '127.0.0.1',
        country: 'US',
        city: 'NYC',
        createdAtUtc: '2024-01-01T00:00:00Z',
        absoluteExpiryUtc: '2024-02-01T00:00:00Z',
        lastActivityUtc: '2024-01-02T00:00:00Z',
        revokedAtUtc: undefined
      }
    ];

    expect(flattened).toEqual(expected);
  });
});
