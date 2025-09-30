import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, REQUIRES_AUTH } from '../interceptors/auth.interceptor';
import { DeviceSessions } from '../models/device-sessions';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSessions(): Observable<DeviceSessions[]> {
    return this.http.get<DeviceSessions[]>(`${this.api}/auth/sessions`, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    });
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
