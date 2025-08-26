import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable, Subject, from, of } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../environments/environment';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from '../interceptors/auth.interceptor';
import { RegisterUserRequest } from '../models/register-user-request';
import { RegisterUserResponseDto } from '../models/register-user-response';
import { SocialProvider } from '../models/social-provider';
import { UserProfile } from '../models/UserProfile';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;
  private refresh$?: Subject<unknown>;

  constructor(
    private readonly oauth: OAuthService,
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly sessionService: SessionService,
  ) {
    this.oauth.configure({
      issuer: environment.baseApiUrl,
      clientId: 'frontend',
      responseType: 'code',
      scope: 'api offline_access',
      redirectUri: `${environment.frontendUrl}/signin-callback`,
    });

    this.oauth.requester = (method, url, body, headers) =>
      this.http.request(method, url, {
        body,
        headers,
        withCredentials: true,
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      });
  }

  init(): Promise<void> {
    return this.oauth
      .loadDiscoveryDocumentAndTryLogin()
      .then(() => this.oauth.setupAutomaticSilentRefresh());
  }

  isAuthenticated(): boolean {
    return this.oauth.hasValidAccessToken();
  }

  waitForRefresh(): Observable<unknown> {
    return this.refresh$ ? this.refresh$.asObservable() : of(null);
  }

  getAccessToken(): string | null {
    return this.oauth.getAccessToken();
  }

  getValidAccessToken(): Observable<string> {
    if (this.oauth.hasValidAccessToken()) {
      return of(this.oauth.getAccessToken() as string);
    }

    if (!this.refresh$) {
      this.refresh$ = new Subject<unknown>();

      from(this.oauth.refreshToken()).subscribe({
        next: () => {
          this.refresh$?.next(null);
          this.refresh$?.complete();
          this.refresh$ = undefined;
        },
        error: err => {
          this.refresh$?.error(err);
          this.refresh$ = undefined;
        }
      });
    }

    return this.waitForRefresh().pipe(map(() => this.oauth.getAccessToken() as string));
  }

  startLogin(returnUrl = this.router.url, provider?: SocialProvider): Promise<void> {
    this.oauth.customQueryParams = provider ? { provider } : {};
    return Promise.resolve(this.oauth.initCodeFlow(returnUrl));
  }

  logout(navigate = true): void {
    const sessionId = this.decode()?.sessionId;
    const revoke$ = sessionId
      ? this.sessionService.revokeSession(sessionId)
      : of(void 0);

    revoke$.subscribe({
      next: () => {
        this.oauth.logOut();
        if (navigate) {
          this.router.navigateByUrl(this.redirectToSignIn());
        }
      },
      error: () => {
        this.oauth.logOut();
        if (navigate) {
          this.router.navigateByUrl(this.redirectToSignIn());
        }
      }
    });
  }

  register(data: RegisterUserRequest): Observable<RegisterUserResponseDto> {
    return this.http.post<RegisterUserResponseDto>(
      `${this.api}/users/register`,
      data,
      {
        context: new HttpContext().set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true),
      },
    );
  }

  decode(): UserProfile | null {
    const token = this.oauth.getAccessToken();
    if (!token) {
      return null;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const roles = payload.role
        ? Array.isArray(payload.role)
          ? payload.role
          : [payload.role]
        : [];
      const permissions = payload.permission
        ? Array.isArray(payload.permission)
          ? payload.permission
          : [payload.permission]
        : [];

      const profile: UserProfile = {
        id: payload.sub,
        email: payload.email,
        firstName: payload.given_name,
        lastName: payload.family_name,
        fullName: payload.name,
        timeZoneId: payload.timezone,
        ipAddress: payload.ip_address,
        imageUrl: payload.image,
        deviceId: payload.device_id,
        sessionId: payload.sid ?? payload.session_id,
        country: payload.country,
        city: payload.city,
        roles,
        permissions,
        exp: payload.exp,
      };
      return profile;
    } catch {
      return null;
    }
  }

  redirectToSignIn(returnUrl: string = this.router.url): UrlTree {
    return this.router.createUrlTree(['/signin'], { queryParams: { returnUrl } });
  }
}

