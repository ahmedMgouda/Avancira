import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, REQUIRES_AUTH } from '../interceptors/auth.interceptor';
import { Session } from '../models/session';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSessions(): Observable<Session[]> {
    return this.http.get<Session[]>(`${this.api}/auth/sessions`, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    });
  }

  revokeSession(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/auth/sessions/${id}`, {
      context: new HttpContext().set(REQUIRES_AUTH, true).set(INCLUDE_CREDENTIALS, true)
    });
  }
}
