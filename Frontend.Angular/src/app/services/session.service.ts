import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, REQUIRES_AUTH } from '../interceptors/auth.interceptor';
import { DeviceSessions } from '../models/device-sessions';
import { UserSession } from '../models/session';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSessions(): Observable<DeviceSessions[]> {
    return this.http.get<DeviceSessions[]>(`${this.api}/auth/sessions`, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    }).pipe(map(response => this.mapDeviceSessions(response)));
  }

  getFlatSessions(): Observable<UserSession[]> {
    return this.getSessions().pipe(map(sessions => this.flattenSessions(sessions)));
  }

  flattenSessions(deviceSessions: DeviceSessions[]): UserSession[] {
    return deviceSessions.flatMap(device =>
      device.sessions.map(session => ({
        ...session,
        deviceId: device.deviceId || session.deviceId,
        deviceName: session.deviceName ?? device.deviceName,
        operatingSystem: session.operatingSystem ?? device.operatingSystem,
        userAgent: session.userAgent ?? device.userAgent,
        country: session.country ?? device.country,
        city: session.city ?? device.city,
      }))
    );
  }

  private mapDeviceSessions(response: DeviceSessions[]): DeviceSessions[] {
    return response.map(device => ({
      ...device,
      sessions: device.sessions.map(session => ({
        ...session,
        deviceId: session.deviceId || device.deviceId,
        deviceName: session.deviceName ?? device.deviceName,
        operatingSystem: session.operatingSystem ?? device.operatingSystem,
        userAgent: session.userAgent ?? device.userAgent,
        country: session.country ?? device.country,
        city: session.city ?? device.city,
      }))
    }));
  }

  revokeSession(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/auth/sessions/${id}`, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    });
  }

  revokeSessions(ids: string[]): Observable<void> {
    return this.http.post<void>(`${this.api}/auth/sessions/batch`, ids, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    });
  }
}
